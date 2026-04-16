-- Migration: 103_add_account_password_reset_nonce
-- One-time forgot-password reset: JWT must match Accounts.PasswordResetNonce until reset succeeds.

ALTER TABLE Accounts
    ADD COLUMN PasswordResetNonce CHAR(36) NULL
        COMMENT 'Single-use forgot-password JWT; cleared after reset'
        AFTER DeletedAt;

-- ═══════════════════════════════════════════════════════════
-- Migration history
-- ═══════════════════════════════════════════════════════════
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('103_add_account_password_reset_nonce', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
