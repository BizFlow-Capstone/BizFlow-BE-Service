-- Migration: 007_placeholder
-- Description: Reserved for future use (BusinessLocations now uses INT from creation)
-- Date: 2026-01-31

-- =============================================
-- NOTE: This migration is no longer needed because:
-- - BusinessLocations is created with INT AUTO_INCREMENT in migration 005
-- - UserLocationAssignments is created with INT AUTO_INCREMENT in migration 005
-- =============================================

-- Insert this migration (placeholder)
INSERT INTO __MigrationHistory (MigrationId, ProductVersion) 
VALUES ('007_placeholder', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
