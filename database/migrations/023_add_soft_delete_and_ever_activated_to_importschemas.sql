-- Migration: 023_add_soft_delete_and_ever_activated_to_importschemas
-- Description: Add EverActivated and DeletedAt columns to ImportSchemas table
-- Date: 2026-02-24

ALTER TABLE ImportSchemas
    ADD COLUMN EverActivated BOOLEAN NOT NULL DEFAULT FALSE
        COMMENT 'True if this schema has ever been set as active (gates soft vs hard delete)'
    AFTER IsActive,
    ADD COLUMN DeletedAt DATETIME NULL
        COMMENT 'Soft delete timestamp; NULL means not deleted'
    AFTER EverActivated;

-- Add index for query filter performance
CREATE INDEX idx_importschemas_deletedat ON ImportSchemas(DeletedAt);

INSERT INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('023_add_soft_delete_and_ever_activated_to_importschemas', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
