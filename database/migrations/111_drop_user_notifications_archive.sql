-- =============================================
-- Migration  : 111_drop_user_notifications_archive
-- Description: Remove unused UserNotificationsArchive table
-- Date       : 2026-04-10
-- =============================================

DROP TABLE IF EXISTS UserNotificationsArchive;

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES
('111_drop_user_notifications_archive', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
