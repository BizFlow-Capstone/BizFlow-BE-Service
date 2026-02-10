-- Migration: 013_add_product_image_publicid
-- Description: Add ImagePublicId column to Products table for Cloudinary image management
-- Date: 2026-02-08

-- =============================================
-- ADD ImagePublicId COLUMN TO PRODUCTS TABLE
-- =============================================

-- Check if column exists before adding
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'bizflow_db' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'ImagePublicId');

SET @sql = IF(@col_exists = 0,
    'ALTER TABLE Products ADD COLUMN ImagePublicId VARCHAR(255) NULL COMMENT ''Cloudinary public ID for image deletion'' AFTER ImageUrl',
    'SELECT "Column ImagePublicId already exists in Products" AS Info');

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add index for faster lookup during cleanup jobs (only if column was added)
SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS 
    WHERE TABLE_SCHEMA = 'bizflow_db' AND TABLE_NAME = 'Products' AND INDEX_NAME = 'idx_product_image_publicid');

SET @sql = IF(@idx_exists = 0,
    'CREATE INDEX idx_product_image_publicid ON Products(ImagePublicId)',
    'SELECT "Index idx_product_image_publicid already exists" AS Info');

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Insert migration record
INSERT INTO __MigrationHistory (MigrationId, ProductVersion) 
VALUES ('013_add_product_image_publicid', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
