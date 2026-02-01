-- Migration: 011_add_soft_delete_to_business_locations
-- Description: Add IsDeleted column to BusinessLocations table for soft delete functionality
-- Date: 2026-02-01

-- =============================================
-- ADD IsDeleted COLUMN TO BusinessLocations
-- =============================================

-- Check if column exists before adding (idempotent)
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'bizflow_db' 
    AND TABLE_NAME = 'BusinessLocations' 
    AND COLUMN_NAME = 'IsDeleted');

SET @sql_add_col = IF(@col_exists = 0,
    'ALTER TABLE BusinessLocations ADD COLUMN IsDeleted BOOLEAN NOT NULL DEFAULT FALSE COMMENT ''Soft delete flag'' AFTER IsActive',
    'SELECT "Column IsDeleted already exists in BusinessLocations" AS Info');

PREPARE stmt FROM @sql_add_col;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add index for IsDeleted if not exists
SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS 
    WHERE TABLE_SCHEMA = 'bizflow_db' 
    AND TABLE_NAME = 'BusinessLocations' 
    AND INDEX_NAME = 'idx_business_location_is_deleted');

SET @sql_add_idx = IF(@idx_exists = 0,
    'ALTER TABLE BusinessLocations ADD INDEX idx_business_location_is_deleted (IsDeleted)',
    'SELECT "Index idx_business_location_is_deleted already exists" AS Info');

PREPARE stmt FROM @sql_add_idx;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Insert this migration
INSERT INTO __MigrationHistory (MigrationId, ProductVersion) 
VALUES ('011_add_soft_delete_to_business_locations', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
