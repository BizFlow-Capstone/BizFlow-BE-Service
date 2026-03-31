-- Migration: 080_add_document_number_and_date_to_costs_revenues
-- Chung tu khong la cot rieng.
-- Chung tu gom:
-- - DocumentNumber: so hieu chung tu (optional)
-- - DocumentDate  : ngay chung tu (optional)

SET @has_cost_document_number := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'Costs'
      AND COLUMN_NAME = 'DocumentNumber'
);

SET @sql := IF(
    @has_cost_document_number = 0,
    'ALTER TABLE Costs ADD COLUMN DocumentNumber VARCHAR(100) NULL COMMENT ''So hieu chung tu'' AFTER DocumentPublicId',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @has_cost_document_date := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'Costs'
      AND COLUMN_NAME = 'DocumentDate'
);

SET @sql := IF(
    @has_cost_document_date = 0,
    'ALTER TABLE Costs ADD COLUMN DocumentDate DATE NULL COMMENT ''Ngay chung tu'' AFTER DocumentNumber',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @has_revenue_document_number := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'Revenues'
      AND COLUMN_NAME = 'DocumentNumber'
);

SET @sql := IF(
    @has_revenue_document_number = 0,
    'ALTER TABLE Revenues ADD COLUMN DocumentNumber VARCHAR(100) NULL COMMENT ''So hieu chung tu'' AFTER MoneyChannel',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @has_revenue_document_date := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'Revenues'
      AND COLUMN_NAME = 'DocumentDate'
);

SET @sql := IF(
    @has_revenue_document_date = 0,
    'ALTER TABLE Revenues ADD COLUMN DocumentDate DATE NULL COMMENT ''Ngay chung tu'' AFTER DocumentNumber',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('081_add_document_number_and_date_to_costs_revenues', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);

