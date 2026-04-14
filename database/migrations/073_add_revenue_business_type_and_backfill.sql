-- =============================================
-- Migration 073: Add Revenue.BusinessTypeId and backfill legacy rows
-- Purpose:
-- 1) Add BusinessTypeId to Revenues (idempotent).
-- 2) Backfill BusinessTypeId for existing revenues.
-- 3) Normalize formula TaxType token PIT_M1 -> PIT_METHOD_1.
-- =============================================

SET SQL_SAFE_UPDATES = 0;

-- 1) Add Revenue.BusinessTypeId column if missing
SET @has_revenue_business_type_col := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'Revenues'
      AND COLUMN_NAME = 'BusinessTypeId'
);

SET @sql_add_revenue_business_type_col := IF(
    @has_revenue_business_type_col = 0,
    'ALTER TABLE Revenues ADD COLUMN BusinessTypeId CHAR(36) NULL COMMENT ''Nguon phan loai nganh nghe cho book live view'' AFTER BusinessLocationId',
    'SELECT 1'
);

PREPARE stmt_add_revenue_business_type_col FROM @sql_add_revenue_business_type_col;
EXECUTE stmt_add_revenue_business_type_col;
DEALLOCATE PREPARE stmt_add_revenue_business_type_col;

-- 2) Add index if missing
SET @has_idx_revenue_business_type := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'Revenues'
      AND INDEX_NAME = 'idx_revenue_business_type'
);

SET @sql_add_idx_revenue_business_type := IF(
    @has_idx_revenue_business_type = 0,
    'ALTER TABLE Revenues ADD INDEX idx_revenue_business_type (BusinessTypeId)',
    'SELECT 1'
);

PREPARE stmt_add_idx_revenue_business_type FROM @sql_add_idx_revenue_business_type;
EXECUTE stmt_add_idx_revenue_business_type;
DEALLOCATE PREPARE stmt_add_idx_revenue_business_type;

-- 3) Add FK if missing
SET @has_fk_revenue_business_type := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS
    WHERE CONSTRAINT_SCHEMA = DATABASE()
      AND CONSTRAINT_NAME = 'fk_revenue_business_type'
      AND TABLE_NAME = 'Revenues'
);

SET @sql_add_fk_revenue_business_type := IF(
    @has_fk_revenue_business_type = 0,
    'ALTER TABLE Revenues ADD CONSTRAINT fk_revenue_business_type FOREIGN KEY (BusinessTypeId) REFERENCES BusinessTypes(BusinessTypeId) ON DELETE SET NULL ON UPDATE CASCADE',
    'SELECT 1'
);

PREPARE stmt_add_fk_revenue_business_type FROM @sql_add_fk_revenue_business_type;
EXECUTE stmt_add_fk_revenue_business_type;
DEALLOCATE PREPARE stmt_add_fk_revenue_business_type;

-- Aiven: disable primary key requirement for temp tables (session-scoped)
SET SESSION sql_require_primary_key = 0;

-- 4) Backfill from Order -> OrderDetails -> SaleItems -> Products when OrderId is available
DROP TEMPORARY TABLE IF EXISTS tmp_revenue_bt_from_order;
CREATE TEMPORARY TABLE tmp_revenue_bt_from_order AS
SELECT
    t.RevenueId,
    SUBSTRING_INDEX(
        GROUP_CONCAT(t.BusinessTypeId ORDER BY t.LineAmount DESC, t.BusinessTypeId SEPARATOR ','),
        ',',
        1
    ) AS BusinessTypeId
FROM (
    SELECT
        r.RevenueId,
        p.BusinessTypeId,
        SUM(od.Amount) AS LineAmount
    FROM Revenues r
    JOIN OrderDetails od
        ON od.OrderId = r.OrderId
    JOIN SaleItems si
        ON si.SaleItemId = od.SaleItemId
    JOIN Products p
        ON p.ProductId = si.ProductId
    WHERE r.BusinessTypeId IS NULL
      AND r.OrderId IS NOT NULL
      AND r.DeletedAt IS NULL
      AND p.DeletedAt IS NULL
    GROUP BY r.RevenueId, p.BusinessTypeId
) t
GROUP BY t.RevenueId;

CREATE INDEX idx_tmp_revenue_bt_from_order_revenue ON tmp_revenue_bt_from_order (RevenueId);

UPDATE Revenues r
JOIN tmp_revenue_bt_from_order ro
    ON ro.RevenueId = r.RevenueId
SET r.BusinessTypeId = ro.BusinessTypeId
WHERE r.BusinessTypeId IS NULL;

-- 5) Backfill remaining rows by dominant BusinessType per location (from active products)
DROP TEMPORARY TABLE IF EXISTS tmp_location_default_bt;
CREATE TEMPORARY TABLE tmp_location_default_bt AS
SELECT
    x.BusinessLocationId,
    SUBSTRING_INDEX(
        GROUP_CONCAT(x.BusinessTypeId ORDER BY x.Cnt DESC, x.BusinessTypeId SEPARATOR ','),
        ',',
        1
    ) AS BusinessTypeId
FROM (
    SELECT
        p.BusinessLocationId,
        p.BusinessTypeId,
        COUNT(*) AS Cnt
    FROM Products p
    WHERE p.DeletedAt IS NULL
    GROUP BY p.BusinessLocationId, p.BusinessTypeId
) x
GROUP BY x.BusinessLocationId;

CREATE INDEX idx_tmp_location_default_bt_location ON tmp_location_default_bt (BusinessLocationId);

UPDATE Revenues r
JOIN tmp_location_default_bt lbt
    ON lbt.BusinessLocationId = r.BusinessLocationId
SET r.BusinessTypeId = lbt.BusinessTypeId
WHERE r.BusinessTypeId IS NULL;

-- 6) Global fallback for rows that still cannot be inferred
SET @fallback_business_type_id := (
    SELECT bt.BusinessTypeId
    FROM BusinessTypes bt
    WHERE LOWER(bt.Status) = 'active'
    ORDER BY
        CASE
            WHEN bt.Code = 'RETAIL' THEN 0
            WHEN bt.Code = 'bt-retail' THEN 1
            ELSE 2
        END,
        bt.BusinessTypeId
    LIMIT 1
);

UPDATE Revenues r
SET r.BusinessTypeId = @fallback_business_type_id
WHERE r.BusinessTypeId IS NULL
  AND @fallback_business_type_id IS NOT NULL;

-- 7) Normalize formula TaxType token for strict matching in engine
UPDATE FormulaDefinitions
SET ExpressionJson = REPLACE(ExpressionJson, '"TaxType":"PIT_M1"', '"TaxType":"PIT_METHOD_1"')
WHERE ExpressionJson LIKE '%"TaxType"%PIT_M1%';

UPDATE FormulaDefinitions
SET ExpressionJson = REPLACE(ExpressionJson, '"TaxType": "PIT_M1"', '"TaxType": "PIT_METHOD_1"')
WHERE ExpressionJson LIKE '%"TaxType"%PIT_M1%';

-- Cleanup temp tables
DROP TEMPORARY TABLE IF EXISTS tmp_location_default_bt;
DROP TEMPORARY TABLE IF EXISTS tmp_revenue_bt_from_order;

-- Restore primary key requirement
SET SESSION sql_require_primary_key = 1;

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('073_add_revenue_business_type_and_backfill', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
