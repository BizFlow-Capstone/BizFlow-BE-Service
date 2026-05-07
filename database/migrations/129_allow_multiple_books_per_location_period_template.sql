-- =============================================
-- Migration 129: Allow multiple books per (location, period, template version)
-- =============================================
SET NAMES utf8mb4;

-- Drop the unique constraint that blocks creating multiple books
SET @uq_exists = (
    SELECT COUNT(*)
    FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'AccountingBooks'
      AND INDEX_NAME = 'uq_book_location_period_template_version'
);

SET @sql_drop_uq = IF(
    @uq_exists > 0,
    'ALTER TABLE AccountingBooks DROP INDEX uq_book_location_period_template_version',
    'SELECT 1'
);
PREPARE stmt_drop_uq FROM @sql_drop_uq;
EXECUTE stmt_drop_uq;
DEALLOCATE PREPARE stmt_drop_uq;

-- Keep lookup performance on the same key shape with a non-unique index
SET @idx_exists = (
    SELECT COUNT(*)
    FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'AccountingBooks'
      AND INDEX_NAME = 'idx_book_location_period_template_version'
);

SET @sql_add_idx = IF(
    @idx_exists = 0,
    'ALTER TABLE AccountingBooks ADD INDEX idx_book_location_period_template_version (BusinessLocationId, PeriodId, TemplateVersionId)',
    'SELECT 1'
);
PREPARE stmt_add_idx FROM @sql_add_idx;
EXECUTE stmt_add_idx;
DEALLOCATE PREPARE stmt_add_idx;

-- ═══════════════════════════════════════════════════════════
-- Migration history
-- ═══════════════════════════════════════════════════════════
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('129_allow_multiple_books_per_location_period_template', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
