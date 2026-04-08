-- Migration: 033_add_memo_to_stock_movements
-- Description: Add Memo column to StockMovements for manual notes/reason.
-- Date: 2026-03-13

-- =============================================
-- 1. Add Memo column (idempotent)
-- =============================================
SET @col_exists = (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'StockMovements'
      AND COLUMN_NAME = 'Memo'
);

SET @sql = IF(
    @col_exists = 0,
    'ALTER TABLE StockMovements ADD COLUMN Memo VARCHAR(1000) NULL COMMENT ''Manual note/reason for this stock movement'' AFTER ReferenceId',
    'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- =============================================
-- Track migration
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('033_add_memo_to_stock_movements', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
