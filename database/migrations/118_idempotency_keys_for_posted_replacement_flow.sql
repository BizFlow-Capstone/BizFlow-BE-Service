-- =============================================
-- Migration  : 118_idempotency_keys_for_posted_replacement_flow
-- Description: Adds optional IdempotencyKey on Costs, Revenues, Imports so
--              replace-when-posted / replace-when-confirmed flows can safely
--              de-duplicate retries (mirrors Order BillMetadata marker pattern).
-- Date       : 2026-04-23
-- =============================================

SET @has_cost_idem = (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Costs' AND COLUMN_NAME = 'IdempotencyKey'
);
SET @sql = IF(@has_cost_idem = 0,
    'ALTER TABLE Costs ADD COLUMN IdempotencyKey VARCHAR(100) NULL COMMENT ''Client idempotency key for replace-when-posted row''',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx_cost_ref_idem = (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Costs' AND INDEX_NAME = 'idx_cost_ref_idempotency'
);
SET @sql = IF(@idx_cost_ref_idem = 0,
    'CREATE INDEX idx_cost_ref_idempotency ON Costs (RefCostId, IdempotencyKey)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_rev_idem = (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Revenues' AND COLUMN_NAME = 'IdempotencyKey'
);
SET @sql = IF(@has_rev_idem = 0,
    'ALTER TABLE Revenues ADD COLUMN IdempotencyKey VARCHAR(100) NULL COMMENT ''Client idempotency key for replace-when-posted row''',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx_rev_ref_idem = (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Revenues' AND INDEX_NAME = 'idx_rev_ref_idempotency'
);
SET @sql = IF(@idx_rev_ref_idem = 0,
    'CREATE INDEX idx_rev_ref_idempotency ON Revenues (RefRevenueId, IdempotencyKey)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_imp_idem = (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Imports' AND COLUMN_NAME = 'IdempotencyKey'
);
SET @sql = IF(@has_imp_idem = 0,
    'ALTER TABLE Imports ADD COLUMN IdempotencyKey VARCHAR(100) NULL COMMENT ''Client idempotency key for replace-when-confirmed row''',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx_imp_ref_idem = (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Imports' AND INDEX_NAME = 'idx_import_ref_idempotency'
);
SET @sql = IF(@idx_imp_ref_idem = 0,
    'CREATE INDEX idx_import_ref_idempotency ON Imports (RefImportId, IdempotencyKey)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('118_idempotency_keys_for_posted_replacement_flow', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
