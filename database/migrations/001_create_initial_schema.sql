-- Migration: 001_create_initial_schema
-- Description: Create initial database schema
-- Date: 2026-01-23

-- Create migrations tracking table
CREATE TABLE IF NOT EXISTS __MigrationHistory (
    MigrationId VARCHAR(150) NOT NULL PRIMARY KEY,
    ProductVersion VARCHAR(32) NOT NULL,
    AppliedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Insert this migration
INSERT INTO __MigrationHistory (MigrationId, ProductVersion) 
VALUES ('001_create_initial_schema', '1.0.0');