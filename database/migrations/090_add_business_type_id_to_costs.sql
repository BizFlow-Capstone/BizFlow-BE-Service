-- ============================================================
-- Migration 090: Add BusinessTypeId to Costs table
-- Purpose: Enable per-industry cost tracking for TT152 S2c
--   (Cách 2): PIT = Σ MAX(0, revenue_i - cost_i) × PIT_rate_i
--   Costs must be tagged with BusinessTypeId so each industry's
--   profit can be computed accurately instead of proration.
-- ============================================================

-- 1. Add nullable BusinessTypeId column (idempotent)
SET @col_exists = (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Costs' AND COLUMN_NAME = 'BusinessTypeId'
);

SET @sql = IF(@col_exists = 0,
    'ALTER TABLE Costs ADD COLUMN BusinessTypeId CHAR(36) DEFAULT NULL COMMENT ''FK to BusinessTypes'' AFTER BusinessLocationId',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- 2. Index for fast per-industry cost queries (idempotent)
SET @idx_exists = (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Costs' AND INDEX_NAME = 'idx_cost_business_type'
);
SET @sql = IF(@idx_exists = 0,
    'CREATE INDEX idx_cost_business_type ON Costs (BusinessTypeId)',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- 3. Composite index (idempotent)
SET @idx2_exists = (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Costs' AND INDEX_NAME = 'idx_cost_location_bt_date'
);
SET @sql = IF(@idx2_exists = 0,
    'CREATE INDEX idx_cost_location_bt_date ON Costs (BusinessLocationId, BusinessTypeId, CostDate)',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- 4. Foreign key constraint (idempotent)
SET @fk_exists = (
    SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Costs' AND CONSTRAINT_NAME = 'fk_cost_business_type'
);
SET @sql = IF(@fk_exists = 0,
    'ALTER TABLE Costs ADD CONSTRAINT fk_cost_business_type FOREIGN KEY (BusinessTypeId) REFERENCES BusinessTypes(BusinessTypeId) ON DELETE SET NULL',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- 5. Migration history
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('090_add_business_type_id_to_costs', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
