-- =============================================
-- Migration  : 124_add_avatar_public_id_to_profiles
-- Description: Add AvatarPublicId to Profiles for Cloudinary cleanup safety.
-- Date       : 2026-05-05
-- =============================================

SET @has_avatar_public_id = (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Profiles' AND COLUMN_NAME = 'AvatarPublicId'
);
SET @sql = IF(@has_avatar_public_id = 0,
    'ALTER TABLE `Profiles` ADD COLUMN `AvatarPublicId` VARCHAR(255) NULL COMMENT ''Cloudinary public ID for avatar deletion/cleanup''',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('124_add_avatar_public_id_to_profiles', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
