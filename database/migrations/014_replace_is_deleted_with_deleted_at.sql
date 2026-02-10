-- Migration: 014_replace_is_deleted_with_deleted_at
-- Description: Replace IsDeleted (BOOLEAN) with DeletedAt (DATETIME) for soft delete
-- Date: 2026-02-09

-- =============================================
-- USERS TABLE
-- =============================================
-- 1. Ensure DeletedAt exists
SET @col_deleted_at_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'bizflow_db' AND TABLE_NAME = 'Users' AND COLUMN_NAME = 'DeletedAt');
SET @sql = IF(@col_deleted_at_exists = 0,
    'ALTER TABLE Users ADD COLUMN DeletedAt DATETIME DEFAULT NULL COMMENT ''Soft delete timestamp'' AFTER IsActive',
    'SELECT "Column DeletedAt already exists in Users" AS Info');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- 2. Migrate data: IsDeleted=1 -> DeletedAt=NOW()
SET @col_is_deleted_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'bizflow_db' AND TABLE_NAME = 'Users' AND COLUMN_NAME = 'IsDeleted');
SET @sql = IF(@col_is_deleted_exists > 0,
    'UPDATE Users SET DeletedAt = NOW() WHERE IsDeleted = 1 AND DeletedAt IS NULL',
    'SELECT "Skipping data migration for Users (IsDeleted missing)" AS Info');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- 3. Drop IsDeleted
SET @sql = IF(@col_is_deleted_exists > 0,
    'ALTER TABLE Users DROP COLUMN IsDeleted',
    'SELECT "Skipping drop IsDeleted for Users" AS Info');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;


-- =============================================
-- BUSINESS LOCATIONS TABLE
-- =============================================
-- 1. Ensure DeletedAt exists
SET @col_deleted_at_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'bizflow_db' AND TABLE_NAME = 'BusinessLocations' AND COLUMN_NAME = 'DeletedAt');
SET @sql = IF(@col_deleted_at_exists = 0,
    'ALTER TABLE BusinessLocations ADD COLUMN DeletedAt DATETIME DEFAULT NULL COMMENT ''Soft delete timestamp'' AFTER IsActive',
    'SELECT "Column DeletedAt already exists in BusinessLocations" AS Info');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- 2. Migrate data
SET @col_is_deleted_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'bizflow_db' AND TABLE_NAME = 'BusinessLocations' AND COLUMN_NAME = 'IsDeleted');
SET @sql = IF(@col_is_deleted_exists > 0,
    'UPDATE BusinessLocations SET DeletedAt = NOW() WHERE IsDeleted = 1 AND DeletedAt IS NULL',
    'SELECT "Skipping data migration for BusinessLocations (IsDeleted missing)" AS Info');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- 3. Drop IsDeleted
SET @sql = IF(@col_is_deleted_exists > 0,
    'ALTER TABLE BusinessLocations DROP COLUMN IsDeleted',
    'SELECT "Skipping drop IsDeleted for BusinessLocations" AS Info');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;


-- =============================================
-- PRODUCTS TABLE
-- =============================================
-- 1. Ensure DeletedAt exists
SET @col_deleted_at_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'bizflow_db' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'DeletedAt');
SET @sql = IF(@col_deleted_at_exists = 0,
    'ALTER TABLE Products ADD COLUMN DeletedAt DATETIME DEFAULT NULL COMMENT ''Soft delete timestamp'' AFTER Status',
    'SELECT "Column DeletedAt already exists in Products" AS Info');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- 2. Migrate data
SET @col_is_deleted_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'bizflow_db' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'IsDeleted');
SET @sql = IF(@col_is_deleted_exists > 0,
    'UPDATE Products SET DeletedAt = NOW() WHERE IsDeleted = 1 AND DeletedAt IS NULL',
    'SELECT "Skipping data migration for Products (IsDeleted missing)" AS Info');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- 3. Drop IsDeleted
SET @sql = IF(@col_is_deleted_exists > 0,
    'ALTER TABLE Products DROP COLUMN IsDeleted',
    'SELECT "Skipping drop IsDeleted for Products" AS Info');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;


-- Insert this migration
INSERT INTO __MigrationHistory (MigrationId, ProductVersion) 
VALUES ('014_replace_is_deleted_with_deleted_at', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
