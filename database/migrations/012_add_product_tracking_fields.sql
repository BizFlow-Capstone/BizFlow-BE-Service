-- Migration: 012_add_product_tracking_fields
-- Description: Add track_inventory, status, and IsDeleted fields to Products table
-- Date: 2026-02-02

-- =============================================
-- ADD track_inventory COLUMN TO Products
-- =============================================

-- Check if column exists before adding (idempotent)
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'bizflow_db' 
    AND TABLE_NAME = 'Products' 
    AND COLUMN_NAME = 'track_inventory');

SET @sql_add_col = IF(@col_exists = 0,
    'ALTER TABLE Products ADD COLUMN track_inventory BOOLEAN NOT NULL DEFAULT TRUE COMMENT ''Whether to track inventory quantity'' AFTER manufacturer',
    'SELECT "Column track_inventory already exists in Products" AS Info');

PREPARE stmt FROM @sql_add_col;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- =============================================
-- ADD status COLUMN TO Products
-- =============================================

-- Check if column exists before adding (idempotent)
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'bizflow_db' 
    AND TABLE_NAME = 'Products' 
    AND COLUMN_NAME = 'status');

SET @sql_add_col = IF(@col_exists = 0,
    'ALTER TABLE Products ADD COLUMN status ENUM(''active'', ''inactive'', ''discontinued'') NOT NULL DEFAULT ''active'' COMMENT ''Product sale status'' AFTER track_inventory',
    'SELECT "Column status already exists in Products" AS Info');

PREPARE stmt FROM @sql_add_col;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- =============================================
-- ADD IsDeleted COLUMN TO Products (Soft Delete)
-- =============================================

-- Check if column exists before adding (idempotent)
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'bizflow_db' 
    AND TABLE_NAME = 'Products' 
    AND COLUMN_NAME = 'IsDeleted');

SET @sql_add_col = IF(@col_exists = 0,
    'ALTER TABLE Products ADD COLUMN IsDeleted BOOLEAN NOT NULL DEFAULT FALSE COMMENT ''Soft delete flag'' AFTER status',
    'SELECT "Column IsDeleted already exists in Products" AS Info');

PREPARE stmt FROM @sql_add_col;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add index for status if not exists
SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS 
    WHERE TABLE_SCHEMA = 'bizflow_db' 
    AND TABLE_NAME = 'Products' 
    AND INDEX_NAME = 'idx_product_status');

SET @sql_add_idx = IF(@idx_exists = 0,
    'ALTER TABLE Products ADD INDEX idx_product_status (status)',
    'SELECT "Index idx_product_status already exists" AS Info');

PREPARE stmt FROM @sql_add_idx;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add index for IsDeleted if not exists
SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS 
    WHERE TABLE_SCHEMA = 'bizflow_db' 
    AND TABLE_NAME = 'Products' 
    AND INDEX_NAME = 'idx_product_is_deleted');

SET @sql_add_idx = IF(@idx_exists = 0,
    'ALTER TABLE Products ADD INDEX idx_product_is_deleted (IsDeleted)',
    'SELECT "Index idx_product_is_deleted already exists" AS Info');

PREPARE stmt FROM @sql_add_idx;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Insert this migration
INSERT INTO __MigrationHistory (MigrationId, ProductVersion) 
VALUES ('012_add_product_tracking_fields', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;


