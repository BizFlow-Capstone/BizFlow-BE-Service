-- Migration: 106_add_account_MustChangePassword
-- Purpose: Thêm cột MustChangePassword (nhắc đổi mật khẩu trên client).

ALTER TABLE Accounts
    ADD COLUMN `MustChangePassword` TINYINT(1) NOT NULL DEFAULT 0
        COMMENT 'Prompt client to change password until cleared'
        AFTER PasswordResetNonce;

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('106_add_account_MustChangePassword', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
