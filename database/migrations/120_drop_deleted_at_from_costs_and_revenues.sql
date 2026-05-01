-- =============================================
-- Migration  : 120_drop_deleted_at_from_costs_and_revenues
-- Description: Remove legacy DeletedAt soft-delete columns from Costs and Revenues.
--              Lifecycle is now represented by Status + CancelledAt/CancelledBy.
-- Date       : 2026-05-01
-- =============================================

-- 1) Backfill Status from legacy DeletedAt before dropping columns.
SET @has_cost_deleted_at = (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Costs' AND COLUMN_NAME = 'DeletedAt'
);
SET @sql = IF(@has_cost_deleted_at > 0,
    'UPDATE Costs SET Status = ''cancelled'' WHERE DeletedAt IS NOT NULL AND Status = ''posted''',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_revenue_deleted_at = (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Revenues' AND COLUMN_NAME = 'DeletedAt'
);
SET @sql = IF(@has_revenue_deleted_at > 0,
    'UPDATE Revenues SET Status = ''cancelled'' WHERE DeletedAt IS NOT NULL AND Status = ''posted''',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- 2) Drop DeletedAt from Costs.
SET @sql = IF(@has_cost_deleted_at > 0,
    'ALTER TABLE Costs DROP COLUMN DeletedAt',
    'SELECT ''Skip: Costs.DeletedAt does not exist'' AS Info');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- 3) Drop DeletedAt from Revenues.
SET @sql = IF(@has_revenue_deleted_at > 0,
    'ALTER TABLE Revenues DROP COLUMN DeletedAt',
    'SELECT ''Skip: Revenues.DeletedAt does not exist'' AS Info');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('120_drop_deleted_at_from_costs_and_revenues', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
