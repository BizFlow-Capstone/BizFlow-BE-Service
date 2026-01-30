-- Migration: 004_rename_manager_to_consultant
-- Description: Rename role 'manager' to 'consultant'
-- Date: 2026-01-30

-- Update role name from 'manager' to 'consultant'
UPDATE Roles 
SET Name = 'consultant', 
    Description = 'Consultant with elevated privileges',
    UpdatedAt = NOW()
WHERE Name = 'manager';

-- Insert this migration
INSERT INTO __MigrationHistory (MigrationId, ProductVersion) 
VALUES ('004_rename_manager_to_consultant', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
