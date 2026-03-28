-- =============================================
-- Migration 042: Add DeviceTokens table for multi-device FCM support
-- =============================================

CREATE TABLE IF NOT EXISTS DeviceTokens (
    DeviceTokenId CHAR(36) PRIMARY KEY COMMENT 'UUID',
    ProfileId CHAR(36) NOT NULL COMMENT 'FK to Profiles',
    Token TEXT NOT NULL COMMENT 'FCM Token',
    DeviceName VARCHAR(255) NULL COMMENT 'Device identifier',
    Platform VARCHAR(50) NOT NULL COMMENT 'iOS, Android, Web',
    RegisteredAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    LastUsedAt DATETIME NULL,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,

    CONSTRAINT fk_device_token_profile
        FOREIGN KEY (ProfileId) REFERENCES Profiles(ProfileId)
        ON DELETE CASCADE,

    UNIQUE KEY idx_device_token_unique (ProfileId, Token(255)),
    INDEX idx_profile_active (ProfileId, IsActive),
    INDEX idx_platform (Platform)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('042_add_device_tokens_table', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
