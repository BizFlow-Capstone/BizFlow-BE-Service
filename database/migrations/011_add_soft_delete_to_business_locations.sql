-- Migration: 011_add_soft_delete_to_business_locations
-- Description: Add IsDeleted column to BusinessLocations table for soft delete functionality
-- Date: 2026-02-01

-- =============================================
-- ADD IsDeleted COLUMN TO BusinessLocations
-- =============================================

-- Use stored procedure to handle conditional logic
DROP PROCEDURE IF EXISTS bizflow_migration_011;

DELIMITER $$
CREATE PROCEDURE bizflow_migration_011()
BEGIN
    DECLARE col_exists INT DEFAULT 0;
    
    -- Check if column already exists
    SELECT COUNT(*) INTO col_exists 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = DATABASE() 
    AND TABLE_NAME = 'BusinessLocations' 
    AND COLUMN_NAME = 'IsDeleted';
    
    -- Only add column if it doesn't exist
    IF col_exists = 0 THEN
        ALTER TABLE BusinessLocations 
        ADD COLUMN IsDeleted BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'Soft delete flag' AFTER IsActive;
        
        ALTER TABLE BusinessLocations 
        ADD INDEX idx_business_location_is_deleted (IsDeleted);
    END IF;
END$$

DELIMITER ;

-- Call the procedure
CALL bizflow_migration_011();

-- Cleanup
DROP PROCEDURE IF EXISTS bizflow_migration_011;

-- Insert this migration
INSERT INTO __MigrationHistory (MigrationId, ProductVersion) 
VALUES ('011_add_soft_delete_to_business_locations', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
