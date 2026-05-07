-- =============================================
-- Migration 130: Allow multiple AccountingPeriods per (location, type, year, quarter)
-- =============================================
SET NAMES utf8mb4;

-- MySQL không cho DROP index đang được dùng để enforce foreign key.
-- Ở schema hiện tại, `fk_period_location` đang dựa vào `idx_period_unique`.
-- => Drop FK trước, rồi mới drop/create index non-unique.

-- 1) Ensure an index exists only on BusinessLocationId (needed for FK recreation)
SET @idx_support_exists = (
    SELECT COUNT(*)
    FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'AccountingPeriods'
      AND INDEX_NAME = 'idx_period_location_support'
);

SET @sql_add_support_idx = IF(
    @idx_support_exists = 0,
    'ALTER TABLE AccountingPeriods ADD INDEX idx_period_location_support (BusinessLocationId)',
    'SELECT 1'
);
PREPARE stmt_add_support_idx FROM @sql_add_support_idx;
EXECUTE stmt_add_support_idx;
DEALLOCATE PREPARE stmt_add_support_idx;

-- 2) Drop FK fk_period_location if exists
SET @fk_exists = (
    SELECT COUNT(*)
    FROM information_schema.TABLE_CONSTRAINTS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'AccountingPeriods'
      AND CONSTRAINT_NAME = 'fk_period_location'
      AND CONSTRAINT_TYPE = 'FOREIGN KEY'
);

SET @sql_drop_fk = IF(
    @fk_exists > 0,
    'ALTER TABLE AccountingPeriods DROP FOREIGN KEY fk_period_location',
    'SELECT 1'
);
PREPARE stmt_drop_fk FROM @sql_drop_fk;
EXECUTE stmt_drop_fk;
DEALLOCATE PREPARE stmt_drop_fk;

-- 3) Drop unique idx_period_unique if it exists
SET @idx_period_unique_exists = (
    SELECT COUNT(*)
    FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'AccountingPeriods'
      AND INDEX_NAME = 'idx_period_unique'
);

SET @sql_drop_idx = IF(
    @idx_period_unique_exists > 0,
    'ALTER TABLE AccountingPeriods DROP INDEX idx_period_unique',
    'SELECT 1'
);
PREPARE stmt_drop_idx FROM @sql_drop_idx;
EXECUTE stmt_drop_idx;
DEALLOCATE PREPARE stmt_drop_idx;

-- 4) Add back a NON-unique index for lookups on the same key shape
SET @idx_period_unique_exists2 = (
    SELECT COUNT(*)
    FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'AccountingPeriods'
      AND INDEX_NAME = 'idx_period_unique'
);

SET @sql_add_idx = IF(
    @idx_period_unique_exists2 = 0,
    'ALTER TABLE AccountingPeriods ADD INDEX idx_period_unique (BusinessLocationId, PeriodType, Year, Quarter)',
    'SELECT 1'
);
PREPARE stmt_add_idx FROM @sql_add_idx;
EXECUTE stmt_add_idx;
DEALLOCATE PREPARE stmt_add_idx;

-- 5) Recreate FK fk_period_location
SET @fk_exists2 = (
    SELECT COUNT(*)
    FROM information_schema.TABLE_CONSTRAINTS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'AccountingPeriods'
      AND CONSTRAINT_NAME = 'fk_period_location'
      AND CONSTRAINT_TYPE = 'FOREIGN KEY'
);

SET @sql_add_fk = IF(
    @fk_exists2 = 0,
    'ALTER TABLE AccountingPeriods
        ADD CONSTRAINT fk_period_location
        FOREIGN KEY (BusinessLocationId)
        REFERENCES BusinessLocations(BusinessLocationId)
        ON DELETE RESTRICT ON UPDATE CASCADE',
    'SELECT 1'
);
PREPARE stmt_add_fk FROM @sql_add_fk;
EXECUTE stmt_add_fk;
DEALLOCATE PREPARE stmt_add_fk;

-- ═══════════════════════════════════════════════════════════
-- Migration history
-- ═══════════════════════════════════════════════════════════
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('130_allow_multiple_periods_per_key', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
