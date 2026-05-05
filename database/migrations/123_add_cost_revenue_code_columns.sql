-- =============================================
-- Migration  : 123_add_cost_revenue_code_columns
-- Description: Add CostCode and RevenueCode columns + unique indexes
--              to align with domain entities and code-generation flow.
-- Date       : 2026-05-05
-- =============================================

-- ── Costs ─────────────────────────────────────────────────────────────

SET @has_cost_code = (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Costs' AND COLUMN_NAME = 'CostCode'
);
SET @sql = IF(@has_cost_code = 0,
    'ALTER TABLE `Costs` ADD COLUMN `CostCode` VARCHAR(50) NULL COMMENT ''Auto-generated cost code''',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx_cost_code = (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Costs' AND INDEX_NAME = 'idx_cost_code'
);
SET @sql = IF(@idx_cost_code = 0,
    'CREATE UNIQUE INDEX `idx_cost_code` ON `Costs` (`CostCode`)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- ── Revenues ──────────────────────────────────────────────────────────

SET @has_revenue_code = (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Revenues' AND COLUMN_NAME = 'RevenueCode'
);
SET @sql = IF(@has_revenue_code = 0,
    'ALTER TABLE `Revenues` ADD COLUMN `RevenueCode` VARCHAR(50) NULL COMMENT ''Auto-generated revenue code''',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx_revenue_code = (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Revenues' AND INDEX_NAME = 'idx_revenue_code'
);
SET @sql = IF(@idx_revenue_code = 0,
    'CREATE UNIQUE INDEX `idx_revenue_code` ON `Revenues` (`RevenueCode`)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('123_add_cost_revenue_code_columns', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
