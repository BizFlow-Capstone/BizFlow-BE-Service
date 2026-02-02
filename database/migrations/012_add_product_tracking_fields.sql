-- Migration: 012_add_product_tracking_fields
-- Description: Add Sku, TrackInventory, Status, and IsDeleted fields to Products table
-- Date: 2026-02-02

-- =============================================
-- ADD Sku COLUMN TO Products
-- =============================================

-- Check if column exists before adding (idempotent)
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'bizflow_db' 
    AND TABLE_NAME = 'Products' 
    AND COLUMN_NAME = 'Sku');

SET @sql_add_col = IF(@col_exists = 0,
    'ALTER TABLE Products ADD COLUMN Sku VARCHAR(100) DEFAULT NULL COMMENT ''Stock Keeping Unit code'' AFTER ProductName',
    'SELECT "Column Sku already exists in Products" AS Info');

PREPARE stmt FROM @sql_add_col;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- =============================================
-- ADD TrackInventory COLUMN TO Products
-- =============================================

-- Check if column exists before adding (idempotent)
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'bizflow_db' 
    AND TABLE_NAME = 'Products' 
    AND COLUMN_NAME = 'TrackInventory');

SET @sql_add_col = IF(@col_exists = 0,
    'ALTER TABLE Products ADD COLUMN TrackInventory BOOLEAN NOT NULL DEFAULT TRUE COMMENT ''Whether to track inventory quantity'' AFTER Manufacturer',
    'SELECT "Column TrackInventory already exists in Products" AS Info');

PREPARE stmt FROM @sql_add_col;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- =============================================
-- ADD Status COLUMN TO Products
-- =============================================

-- Check if column exists before adding (idempotent)
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'bizflow_db' 
    AND TABLE_NAME = 'Products' 
    AND COLUMN_NAME = 'Status');

SET @sql_add_col = IF(@col_exists = 0,
    'ALTER TABLE Products ADD COLUMN Status ENUM(''active'', ''inactive'', ''discontinued'') NOT NULL DEFAULT ''active'' COMMENT ''Product sale status'' AFTER TrackInventory',
    'SELECT "Column Status already exists in Products" AS Info');

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

-- Add index for Sku if not exists
SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS 
    WHERE TABLE_SCHEMA = 'bizflow_db' 
    AND TABLE_NAME = 'Products' 
    AND INDEX_NAME = 'idx_product_sku');

SET @sql_add_idx = IF(@idx_exists = 0,
    'ALTER TABLE Products ADD INDEX idx_product_sku (Sku)',
    'SELECT "Index idx_product_sku already exists" AS Info');

PREPARE stmt FROM @sql_add_idx;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add index for status if not exists
SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS 
    WHERE TABLE_SCHEMA = 'bizflow_db' 
    AND TABLE_NAME = 'Products' 
    AND INDEX_NAME = 'idx_product_status');

SET @sql_add_idx = IF(@idx_exists = 0,
    'ALTER TABLE Products ADD INDEX idx_product_status (Status)',
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


