-- Migration: 025_add_created_at_to_importschemas
-- Description: Add CreatedAt column to ImportSchemas table
-- Date: 2026-02-25

ALTER TABLE ImportSchemas
    ADD COLUMN CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
        COMMENT 'When this schema was first created'
    AFTER EverActivated;

INSERT INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('025_add_created_at_to_importschemas', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
