-- =============================================
-- Migration  : 117_document_number_uniqueness_and_replace_flow
-- Description: Enforce DocumentNumber uniqueness per Owner across Costs/Revenues
--              (even for cancelled rows) and enable "replace-when-posted" flow
--              for Imports / Costs / Revenues, mirroring the Order replacement
--              pattern.
--
--              Adds:
--               * AccountingDocumentLocks (OwnerId, DocumentNumberNormalized)
--                 synthetic row-lock table to serialize concurrent writes using
--                 MySQL InnoDB row locks (SELECT ... FOR UPDATE).
--               * DocumentNumberNormalized on Costs, Revenues (generated) for
--                 fast lookup.
--               * Status / CancelledAt / CancelledBy / RefCostId on Costs.
--               * Status / CancelledAt / CancelledBy / RefRevenueId on Revenues.
--               * RefImportId / CancelledBy on Imports (CancelledAt already
--                 exists).
--
-- Date       : 2026-04-23
-- =============================================

-- =============================================================================
-- 1. AccountingDocumentLocks
--    Synthetic (OwnerId, DocumentNumberNormalized) key. Rows are created the
--    very first time an owner uses a document number and NEVER deleted. We
--    SELECT ... FOR UPDATE on these rows inside the Cost/Revenue/Import write
--    transactions to serialize concurrent uniqueness checks.
-- =============================================================================
SET @has_doc_lock_table = (
    SELECT COUNT(*)
    FROM information_schema.TABLES
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'AccountingDocumentLocks'
);

SET @sql = IF(
    @has_doc_lock_table = 0,
    'CREATE TABLE AccountingDocumentLocks (
        LockId                      CHAR(36)     NOT NULL,
        OwnerId                     CHAR(36)     NOT NULL,
        DocumentNumberNormalized    VARCHAR(100) NOT NULL,
        CreatedAt                   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
        PRIMARY KEY (LockId),
        UNIQUE KEY ux_doc_lock_owner_number (OwnerId, DocumentNumberNormalized),
        INDEX idx_doc_lock_owner (OwnerId)
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
      COMMENT=''Synthetic row-lock table for DocumentNumber uniqueness per Owner''',
    'SELECT ''Skip: AccountingDocumentLocks already exists'' AS Info'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- =============================================================================
-- 2. Costs : Status, CancelledAt, CancelledBy, RefCostId, DocumentNumberNormalized
-- =============================================================================
SET @has_cost_status = (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Costs' AND COLUMN_NAME = 'Status'
);
SET @sql = IF(@has_cost_status = 0,
    'ALTER TABLE Costs ADD COLUMN Status VARCHAR(32) NOT NULL DEFAULT ''posted'' COMMENT ''draft | posted | cancelled | replaced'' AFTER Amount',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_cost_cancelled_at = (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Costs' AND COLUMN_NAME = 'CancelledAt'
);
SET @sql = IF(@has_cost_cancelled_at = 0,
    'ALTER TABLE Costs ADD COLUMN CancelledAt DATETIME NULL COMMENT ''When the cost was cancelled (replace-flow or manual)''',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_cost_cancelled_by = (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Costs' AND COLUMN_NAME = 'CancelledBy'
);
SET @sql = IF(@has_cost_cancelled_by = 0,
    'ALTER TABLE Costs ADD COLUMN CancelledBy CHAR(36) NULL COMMENT ''UserId that cancelled this cost''',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_cost_ref = (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Costs' AND COLUMN_NAME = 'RefCostId'
);
SET @sql = IF(@has_cost_ref = 0,
    'ALTER TABLE Costs ADD COLUMN RefCostId BIGINT NULL COMMENT ''Original Cost this record replaces (replace-when-posted flow)''',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_cost_doc_norm = (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Costs' AND COLUMN_NAME = 'DocumentNumberNormalized'
);
SET @sql = IF(@has_cost_doc_norm = 0,
    'ALTER TABLE Costs ADD COLUMN DocumentNumberNormalized VARCHAR(100) GENERATED ALWAYS AS (UPPER(TRIM(DocumentNumber))) STORED NULL COMMENT ''Normalized DocumentNumber for uniqueness index''',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Index on (BusinessLocationId, DocumentNumberNormalized) to speed per-location lookup (owner resolution goes through BusinessLocations).
SET @idx_cost_doc = (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Costs' AND INDEX_NAME = 'idx_cost_location_doc_norm'
);
SET @sql = IF(@idx_cost_doc = 0,
    'CREATE INDEX idx_cost_location_doc_norm ON Costs (BusinessLocationId, DocumentNumberNormalized)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx_cost_ref = (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Costs' AND INDEX_NAME = 'idx_cost_ref_cost'
);
SET @sql = IF(@idx_cost_ref = 0,
    'CREATE INDEX idx_cost_ref_cost ON Costs (RefCostId)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Back-fill Status for existing rows: DeletedAt => cancelled, else posted.
UPDATE Costs
SET Status = CASE WHEN DeletedAt IS NOT NULL THEN 'cancelled' ELSE 'posted' END
WHERE Status = 'posted' AND DeletedAt IS NOT NULL;

-- =============================================================================
-- 3. Revenues : Status, CancelledAt, CancelledBy, RefRevenueId, DocumentNumberNormalized
-- =============================================================================
SET @has_rev_status = (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Revenues' AND COLUMN_NAME = 'Status'
);
SET @sql = IF(@has_rev_status = 0,
    'ALTER TABLE Revenues ADD COLUMN Status VARCHAR(32) NOT NULL DEFAULT ''posted'' COMMENT ''draft | posted | cancelled | replaced'' AFTER Amount',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_rev_cancelled_at = (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Revenues' AND COLUMN_NAME = 'CancelledAt'
);
SET @sql = IF(@has_rev_cancelled_at = 0,
    'ALTER TABLE Revenues ADD COLUMN CancelledAt DATETIME NULL COMMENT ''When the revenue was cancelled (replace-flow or manual)''',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_rev_cancelled_by = (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Revenues' AND COLUMN_NAME = 'CancelledBy'
);
SET @sql = IF(@has_rev_cancelled_by = 0,
    'ALTER TABLE Revenues ADD COLUMN CancelledBy CHAR(36) NULL COMMENT ''UserId that cancelled this revenue''',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_rev_ref = (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Revenues' AND COLUMN_NAME = 'RefRevenueId'
);
SET @sql = IF(@has_rev_ref = 0,
    'ALTER TABLE Revenues ADD COLUMN RefRevenueId BIGINT NULL COMMENT ''Original Revenue this record replaces (replace-when-posted flow)''',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_rev_doc_norm = (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Revenues' AND COLUMN_NAME = 'DocumentNumberNormalized'
);
SET @sql = IF(@has_rev_doc_norm = 0,
    'ALTER TABLE Revenues ADD COLUMN DocumentNumberNormalized VARCHAR(100) GENERATED ALWAYS AS (UPPER(TRIM(DocumentNumber))) STORED NULL COMMENT ''Normalized DocumentNumber for uniqueness index''',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx_rev_doc = (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Revenues' AND INDEX_NAME = 'idx_rev_location_doc_norm'
);
SET @sql = IF(@idx_rev_doc = 0,
    'CREATE INDEX idx_rev_location_doc_norm ON Revenues (BusinessLocationId, DocumentNumberNormalized)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx_rev_ref = (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Revenues' AND INDEX_NAME = 'idx_rev_ref_revenue'
);
SET @sql = IF(@idx_rev_ref = 0,
    'CREATE INDEX idx_rev_ref_revenue ON Revenues (RefRevenueId)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

UPDATE Revenues
SET Status = CASE WHEN DeletedAt IS NOT NULL THEN 'cancelled' ELSE 'posted' END
WHERE Status = 'posted' AND DeletedAt IS NOT NULL;

-- =============================================================================
-- 4. Imports : RefImportId, CancelledBy
--    (CancelledAt & Status already exist)
-- =============================================================================
SET @has_imp_ref = (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Imports' AND COLUMN_NAME = 'RefImportId'
);
SET @sql = IF(@has_imp_ref = 0,
    'ALTER TABLE Imports ADD COLUMN RefImportId BIGINT NULL COMMENT ''Original Import this record replaces (replace-when-confirmed flow)''',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_imp_cancelled_by = (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Imports' AND COLUMN_NAME = 'CancelledBy'
);
SET @sql = IF(@has_imp_cancelled_by = 0,
    'ALTER TABLE Imports ADD COLUMN CancelledBy CHAR(36) NULL COMMENT ''UserId that cancelled this import''',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx_imp_ref = (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Imports' AND INDEX_NAME = 'idx_import_ref_import'
);
SET @sql = IF(@idx_imp_ref = 0,
    'CREATE INDEX idx_import_ref_import ON Imports (RefImportId)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- =============================================================================
-- 5. Back-fill AccountingDocumentLocks for every (Owner, DocumentNumber) pair
--    already in use. Owner is resolved through UserLocationAssignments
--    (IsOwner = 1) because BusinessLocations has no direct OwnerId column.
--    Uses INSERT IGNORE to avoid duplicate PK/UNIQUE collisions on re-run.
-- =============================================================================
INSERT IGNORE INTO AccountingDocumentLocks (LockId, OwnerId, DocumentNumberNormalized, CreatedAt)
SELECT UUID(), ula.UserId, UPPER(TRIM(c.DocumentNumber)), NOW()
FROM Costs c
JOIN UserLocationAssignments ula ON ula.BusinessLocationId = c.BusinessLocationId
WHERE c.DocumentNumber IS NOT NULL
  AND TRIM(c.DocumentNumber) <> ''
  AND ula.IsOwner = 1
GROUP BY ula.UserId, UPPER(TRIM(c.DocumentNumber));

INSERT IGNORE INTO AccountingDocumentLocks (LockId, OwnerId, DocumentNumberNormalized, CreatedAt)
SELECT UUID(), ula.UserId, UPPER(TRIM(r.DocumentNumber)), NOW()
FROM Revenues r
JOIN UserLocationAssignments ula ON ula.BusinessLocationId = r.BusinessLocationId
WHERE r.DocumentNumber IS NOT NULL
  AND TRIM(r.DocumentNumber) <> ''
  AND ula.IsOwner = 1
GROUP BY ula.UserId, UPPER(TRIM(r.DocumentNumber));

-- =============================================================================
-- 6. Migration history
-- =============================================================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('117_document_number_uniqueness_and_replace_flow', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
