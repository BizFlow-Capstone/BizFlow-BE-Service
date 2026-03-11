-- Migration: 028_restore_missing_columns
-- Description: Restore columns that were incorrectly removed in migration 026
--   - BusinessLocations: Add back District, City, IsActive, DeletedAt
--   - Imports: Add CancelReason
--   - UserLocationAssignments: Add back IsOwner, IsActive
-- Date: 2026-03-11

-- =============================================
-- 1. BUSINESSLOCATIONS: Restore District, City, IsActive, DeletedAt
-- =============================================
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'BusinessLocations' AND COLUMN_NAME = 'District');
SET @sql = IF(@col_exists = 0,
    'ALTER TABLE BusinessLocations ADD COLUMN District VARCHAR(100) DEFAULT NULL AFTER Address',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'BusinessLocations' AND COLUMN_NAME = 'City');
SET @sql = IF(@col_exists = 0,
    'ALTER TABLE BusinessLocations ADD COLUMN City VARCHAR(100) DEFAULT NULL AFTER District',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'BusinessLocations' AND COLUMN_NAME = 'IsActive');
SET @sql = IF(@col_exists = 0,
    'ALTER TABLE BusinessLocations ADD COLUMN IsActive BOOLEAN NOT NULL DEFAULT TRUE AFTER Status',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'BusinessLocations' AND COLUMN_NAME = 'DeletedAt');
SET @sql = IF(@col_exists = 0,
    'ALTER TABLE BusinessLocations ADD COLUMN DeletedAt DATETIME DEFAULT NULL COMMENT ''Soft delete timestamp'' AFTER UpdatedAt',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Add indexes for restored columns
SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'BusinessLocations' AND INDEX_NAME = 'idx_business_location_city');
SET @sql = IF(@idx_exists = 0,
    'ALTER TABLE BusinessLocations ADD INDEX idx_business_location_city (City)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'BusinessLocations' AND INDEX_NAME = 'idx_business_location_is_active');
SET @sql = IF(@idx_exists = 0,
    'ALTER TABLE BusinessLocations ADD INDEX idx_business_location_is_active (IsActive)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'BusinessLocations' AND INDEX_NAME = 'idx_business_location_deleted_at');
SET @sql = IF(@idx_exists = 0,
    'ALTER TABLE BusinessLocations ADD INDEX idx_business_location_deleted_at (DeletedAt)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- =============================================
-- 2. IMPORTS: Add CancelReason
-- =============================================
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Imports' AND COLUMN_NAME = 'CancelReason');
SET @sql = IF(@col_exists = 0,
    'ALTER TABLE Imports ADD COLUMN CancelReason TEXT DEFAULT NULL COMMENT ''Reason for cancellation'' AFTER CancelledAt',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- =============================================
-- 3. USERLOCATIONASSIGNMENTS: Restore IsOwner, IsActive
-- =============================================
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'UserLocationAssignments' AND COLUMN_NAME = 'IsOwner');
SET @sql = IF(@col_exists = 0,
    'ALTER TABLE UserLocationAssignments ADD COLUMN IsOwner BOOLEAN NOT NULL DEFAULT FALSE COMMENT ''Is the owner of this location'' AFTER BusinessLocationId',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'UserLocationAssignments' AND COLUMN_NAME = 'IsActive');
SET @sql = IF(@col_exists = 0,
    'ALTER TABLE UserLocationAssignments ADD COLUMN IsActive BOOLEAN NOT NULL DEFAULT TRUE AFTER IsOwner',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- =============================================
-- Insert this migration
-- =============================================
INSERT INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('028_restore_missing_columns', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
