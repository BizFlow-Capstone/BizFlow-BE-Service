-- =============================================
-- Migration  : 050_create_notification_center_tables
-- Description: Unified notification migration (campaigns, notifications, user inbox, outbox, archive)
-- Date       : 2026-03-25
-- =============================================

CREATE TABLE IF NOT EXISTS NotificationTemplates (
    NotificationTemplateId CHAR(36) NOT NULL PRIMARY KEY,
    EventCode VARCHAR(100) NOT NULL,
    NotificationType VARCHAR(50) NOT NULL,
    TitleTemplate VARCHAR(250) NOT NULL,
    ContentTemplate TEXT NOT NULL,
    DefaultActionType VARCHAR(50) NULL,
    DefaultTargetScreen VARCHAR(100) NULL,
    DefaultActionPayloadJson JSON NULL,
    IsActive BIT(1) NOT NULL DEFAULT b'1',
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT uq_notification_templates_event_code UNIQUE (EventCode),
    INDEX idx_notification_templates_is_active (IsActive),
    INDEX idx_notification_templates_type (NotificationType)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS NotificationCampaigns (
    NotificationDispatchId BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    NotificationTemplateId CHAR(36) NULL,
    NotificationType VARCHAR(50) NOT NULL,
    Priority VARCHAR(20) NOT NULL DEFAULT 'NORMAL',
    Title VARCHAR(250) NOT NULL,
    Content TEXT NOT NULL,
    DataJson JSON NULL,
    ActionType VARCHAR(50) NULL,
    TargetScreen VARCHAR(100) NULL,
    ActionPayloadJson JSON NULL,
    RecipientScope VARCHAR(30) NOT NULL DEFAULT 'SPECIFIC_USERS',
    RecipientUserIdsJson JSON NULL,
    ScheduledAt DATETIME NULL,
    SentAt DATETIME NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'PENDING',
    ErrorMessage TEXT NULL,
    CreatedByUserId CHAR(36) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_notification_dispatches_template
        FOREIGN KEY (NotificationTemplateId) REFERENCES NotificationTemplates(NotificationTemplateId)
        ON DELETE SET NULL,
    CONSTRAINT fk_notification_dispatches_created_by
        FOREIGN KEY (CreatedByUserId) REFERENCES Profiles(ProfileId)
        ON DELETE SET NULL,
    INDEX idx_notification_dispatches_template (NotificationTemplateId),
    INDEX idx_notification_dispatches_created_by (CreatedByUserId),
    INDEX idx_notification_dispatches_status (Status),
    INDEX idx_notification_dispatches_scheduled_at (ScheduledAt),
    INDEX idx_notification_dispatches_created_at (CreatedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

SET @old_table_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'NotificationDispatches'
);

SET @new_table_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'NotificationCampaigns'
);

SET @sql := IF(
    @old_table_exists = 1 AND @new_table_exists = 0,
    'RENAME TABLE NotificationDispatches TO NotificationCampaigns',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

CREATE TABLE IF NOT EXISTS Notifications (
    NotificationId CHAR(36) NOT NULL PRIMARY KEY,
    NotificationType VARCHAR(50) NOT NULL,
    Priority VARCHAR(20) NOT NULL DEFAULT 'NORMAL',
    Title VARCHAR(250) NOT NULL,
    Content TEXT NOT NULL,
    ActionType VARCHAR(50) NULL,
    TargetScreen VARCHAR(100) NULL,
    ActionPayloadJson JSON NULL,
    DataJson JSON NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_notifications_created_at (CreatedAt),
    INDEX idx_notifications_type (NotificationType)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS UserNotifications (
    UserNotificationId BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    UserId CHAR(36) NOT NULL,
    NotificationId CHAR(36) NULL,
    NotificationType VARCHAR(50) NOT NULL,
    Priority VARCHAR(20) NOT NULL DEFAULT 'NORMAL',
    Title VARCHAR(250) NOT NULL,
    Content TEXT NOT NULL,
    ActionType VARCHAR(50) NULL,
    TargetScreen VARCHAR(100) NULL,
    ActionPayloadJson JSON NULL,
    DeliveryStatus VARCHAR(20) NOT NULL DEFAULT 'IN_QUEUE',
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    SentAt DATETIME NULL,
    ReadAt DATETIME NULL,
    ErrorMessage TEXT NULL,
    CONSTRAINT fk_user_notifications_user
        FOREIGN KEY (UserId) REFERENCES Profiles(ProfileId)
        ON DELETE CASCADE,
    CONSTRAINT fk_user_notifications_notification
        FOREIGN KEY (NotificationId) REFERENCES Notifications(NotificationId)
        ON DELETE SET NULL,
    INDEX idx_user_notifications_user_id (UserId),
    INDEX idx_user_notifications_user_read (UserId, ReadAt),
    INDEX idx_user_notifications_created_at (CreatedAt),
    INDEX idx_user_notifications_notification_id (NotificationId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS UserNotificationsArchive (
    UserNotificationId BIGINT NOT NULL PRIMARY KEY,
    UserId CHAR(36) NOT NULL,
    NotificationId CHAR(36) NULL,
    NotificationType VARCHAR(50) NOT NULL,
    Priority VARCHAR(20) NOT NULL DEFAULT 'NORMAL',
    Title VARCHAR(250) NOT NULL,
    Content TEXT NOT NULL,
    ActionType VARCHAR(50) NULL,
    TargetScreen VARCHAR(100) NULL,
    ActionPayloadJson JSON NULL,
    DeliveryStatus VARCHAR(20) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    SentAt DATETIME NULL,
    ReadAt DATETIME NULL,
    ErrorMessage TEXT NULL,
    INDEX idx_user_notifications_archive_user_created (UserId, CreatedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS NotificationOutboxMessages (
    NotificationOutboxMessageId BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    EventType VARCHAR(100) NOT NULL,
    PayloadJson JSON NOT NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'PENDING',
    RetryCount INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    LastAttemptAt DATETIME NULL,
    ProcessedAt DATETIME NULL,
    LastError TEXT NULL,
    INDEX idx_notification_outbox_status (Status),
    INDEX idx_notification_outbox_created_at (CreatedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

SET @has_notification_id := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'UserNotifications'
      AND COLUMN_NAME = 'NotificationId'
);

SET @sql := IF(
    @has_notification_id = 0,
    'ALTER TABLE UserNotifications ADD COLUMN NotificationId CHAR(36) NULL AFTER UserId',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @archive_has_notification_id := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'UserNotificationsArchive'
      AND COLUMN_NAME = 'NotificationId'
);

SET @sql := IF(
    @archive_has_notification_id = 0,
    'ALTER TABLE UserNotificationsArchive ADD COLUMN NotificationId CHAR(36) NULL AFTER UserId',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @dispatch_priority_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'NotificationCampaigns'
      AND COLUMN_NAME = 'Priority'
);

SET @sql := IF(
    @dispatch_priority_exists = 0,
    'ALTER TABLE NotificationCampaigns ADD COLUMN Priority VARCHAR(20) NOT NULL DEFAULT ''NORMAL'' AFTER NotificationType',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @user_priority_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'UserNotifications'
      AND COLUMN_NAME = 'Priority'
);

SET @sql := IF(
    @user_priority_exists = 0,
    'ALTER TABLE UserNotifications ADD COLUMN Priority VARCHAR(20) NOT NULL DEFAULT ''NORMAL'' AFTER NotificationType',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @legacy_rows_count := 0;
SET @has_legacy_dispatch_col := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'UserNotifications'
      AND COLUMN_NAME = 'NotificationDispatchId'
);

SET @sql := IF(
    @has_legacy_dispatch_col = 1,
    'SELECT COUNT(*) INTO @legacy_rows_count FROM UserNotifications WHERE NotificationId IS NULL AND NotificationDispatchId IS NOT NULL',
    'SELECT 0 INTO @legacy_rows_count'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql := IF(
    @legacy_rows_count > 0,
    'CREATE TEMPORARY TABLE tmp_campaign_notification_map (NotificationDispatchId BIGINT PRIMARY KEY, NotificationId CHAR(36) NOT NULL)',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql := IF(
    @legacy_rows_count > 0,
    'INSERT INTO tmp_campaign_notification_map (NotificationDispatchId, NotificationId) SELECT NotificationDispatchId, UUID() FROM NotificationCampaigns',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql := IF(
    @legacy_rows_count > 0,
    'INSERT INTO Notifications (NotificationId, NotificationType, Priority, Title, Content, ActionType, TargetScreen, ActionPayloadJson, DataJson, CreatedAt) SELECT map.NotificationId, campaign.NotificationType, campaign.Priority, campaign.Title, campaign.Content, campaign.ActionType, campaign.TargetScreen, campaign.ActionPayloadJson, campaign.DataJson, campaign.CreatedAt FROM NotificationCampaigns campaign JOIN tmp_campaign_notification_map map ON map.NotificationDispatchId = campaign.NotificationDispatchId',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql := IF(
    @legacy_rows_count > 0,
    'UPDATE UserNotifications user_notification JOIN tmp_campaign_notification_map map ON map.NotificationDispatchId = user_notification.NotificationDispatchId SET user_notification.NotificationId = map.NotificationId WHERE user_notification.NotificationId IS NULL',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @archive_has_legacy_dispatch_col := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'UserNotificationsArchive'
      AND COLUMN_NAME = 'NotificationDispatchId'
);

SET @sql := IF(
    @legacy_rows_count > 0 AND @archive_has_legacy_dispatch_col = 1,
    'UPDATE UserNotificationsArchive archive JOIN tmp_campaign_notification_map map ON map.NotificationDispatchId = archive.NotificationDispatchId SET archive.NotificationId = map.NotificationId WHERE archive.NotificationId IS NULL',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql := IF(
    @legacy_rows_count > 0,
    'DROP TEMPORARY TABLE IF EXISTS tmp_campaign_notification_map',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @fk_dispatch_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'UserNotifications'
      AND CONSTRAINT_NAME = 'fk_user_notifications_dispatch'
      AND CONSTRAINT_TYPE = 'FOREIGN KEY'
);

SET @sql := IF(
    @fk_dispatch_exists = 1,
    'ALTER TABLE UserNotifications DROP FOREIGN KEY fk_user_notifications_dispatch',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @idx_dispatch_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'UserNotifications'
      AND INDEX_NAME = 'idx_user_notifications_dispatch_id'
);

SET @sql := IF(
    @idx_dispatch_exists > 0,
    'ALTER TABLE UserNotifications DROP INDEX idx_user_notifications_dispatch_id',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @idx_notification_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'UserNotifications'
      AND INDEX_NAME = 'idx_user_notifications_notification_id'
);

SET @sql := IF(
    @idx_notification_exists = 0,
    'ALTER TABLE UserNotifications ADD INDEX idx_user_notifications_notification_id (NotificationId)',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @fk_notification_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'UserNotifications'
      AND CONSTRAINT_NAME = 'fk_user_notifications_notification'
      AND CONSTRAINT_TYPE = 'FOREIGN KEY'
);

SET @sql := IF(
    @fk_notification_exists = 0,
    'ALTER TABLE UserNotifications ADD CONSTRAINT fk_user_notifications_notification FOREIGN KEY (NotificationId) REFERENCES Notifications(NotificationId) ON DELETE SET NULL',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @has_user_dispatch_col := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'UserNotifications'
      AND COLUMN_NAME = 'NotificationDispatchId'
);

SET @sql := IF(
    @has_user_dispatch_col = 1,
    'ALTER TABLE UserNotifications DROP COLUMN NotificationDispatchId',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @has_archive_dispatch_col := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'UserNotificationsArchive'
      AND COLUMN_NAME = 'NotificationDispatchId'
);

SET @sql := IF(
    @has_archive_dispatch_col = 1,
    'ALTER TABLE UserNotificationsArchive DROP COLUMN NotificationDispatchId',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

INSERT INTO NotificationTemplates (
    NotificationTemplateId,
    EventCode,
    NotificationType,
    TitleTemplate,
    ContentTemplate,
    DefaultActionType,
    DefaultTargetScreen,
    IsActive,
    CreatedAt
)
VALUES
(UUID(), 'EMPLOYEE_INVITE', 'INVITE_EMPLOYEE', 'Bạn có lời mời làm nhân viên', '{OwnerName} đã mời bạn làm nhân viên. Mở ứng dụng để xem chi tiết lời mời.', 'NAVIGATE', 'EmployeeInvitationsPage', b'1', UTC_TIMESTAMP()),
(UUID(), 'EMPLOYEE_REMOVED', 'EMPLOYEE_REMOVED', 'Thông báo nhân sự', 'Bạn không còn làm việc tại {BusinessName}.', NULL, NULL, b'1', UTC_TIMESTAMP())
ON DUPLICATE KEY UPDATE
NotificationType = VALUES(NotificationType),
TitleTemplate = VALUES(TitleTemplate),
ContentTemplate = VALUES(ContentTemplate),
DefaultActionType = VALUES(DefaultActionType),
DefaultTargetScreen = VALUES(DefaultTargetScreen),
IsActive = VALUES(IsActive);

-- =============================================
-- Insert migration history (legacy compatible)
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES
('050_create_notification_center_tables', '1.0.0'),
('051_notification_outbox_archive_and_priority', '1.0.0'),
('052_realign_campaign_notification_relationships', '1.0.0'),
('053_cleanup_legacy_notification_dispatch_columns', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
