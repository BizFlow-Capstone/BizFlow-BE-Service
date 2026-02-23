-- Migration: 022_add_isactive_to_importschemas
-- Description: Add IsActive column to ImportSchemas table
-- Date: 2026-02-23

ALTER TABLE ImportSchemas
    ADD COLUMN IsActive BOOLEAN NOT NULL DEFAULT TRUE
        COMMENT 'Whether this schema template is available for use'
    AFTER Name;

-- Set existing schema as active
UPDATE ImportSchemas SET IsActive = TRUE;

-- Insert this migration
INSERT INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('022_add_isactive_to_importschemas', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
