-- Migration: 002_add_roles_table
-- Description: Add Roles table
-- Date: 2026-01-23

-- Example Roles table
CREATE TABLE IF NOT EXISTS Roles (
    Id CHAR(36) NOT NULL PRIMARY KEY,
    Name VARCHAR(100) NOT NULL UNIQUE,
    Description TEXT,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_name (Name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Insert this migration
INSERT INTO __MigrationHistory (MigrationId, ProductVersion) 
VALUES ('002_add_roles_table', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;