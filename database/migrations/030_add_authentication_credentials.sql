-- Migration: 030_add_authentication_credentials
-- Description: Unified credential model + JWT refresh tokens
--   - Alter Accounts: make PasswordHash nullable (Google-only accounts)
--   - Create Credentials table (unified: phone/email/google)
--   - Create RefreshTokens table
--   - Migrate existing Account Email/Phone → Credentials
--   - Remove Email, Phone, EmailVerified from Accounts (keep PasswordHash)
-- Date: 2026-03-12

-- =============================================
-- 1. ALTER Accounts: PasswordHash → nullable
--    Google-only accounts sẽ không có password
-- =============================================
ALTER TABLE Accounts
    MODIFY COLUMN PasswordHash VARCHAR(255) DEFAULT NULL COMMENT 'BCrypt hash, NULL for Google-only accounts';

-- =============================================
-- 2. CREATE Credentials TABLE (unified)
--    Mỗi row = 1 identity (phone / email / google)
--    1 account tối đa 3 credentials (1 per type)
-- =============================================
CREATE TABLE IF NOT EXISTS Credentials (
    CredentialId CHAR(36) NOT NULL PRIMARY KEY,
    AccountId CHAR(36) NOT NULL COMMENT 'FK to Accounts',
    Type ENUM('phone', 'email', 'google') NOT NULL COMMENT 'Credential type',
    Identifier VARCHAR(255) NOT NULL COMMENT 'Phone: +84xxx, Email: user@mail, Google: sub-id',
    EmailVerified BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'Only used for type=email',
    GoogleEmail VARCHAR(255) DEFAULT NULL COMMENT 'Only used for type=google (informational)',
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_credential_account FOREIGN KEY (AccountId)
        REFERENCES Accounts(AccountId) ON DELETE CASCADE ON UPDATE CASCADE,

    UNIQUE INDEX uq_credential_type_identifier (Type, Identifier),
    UNIQUE INDEX uq_credential_account_type (AccountId, Type),
    INDEX idx_credential_account (AccountId),
    INDEX idx_credential_identifier (Identifier)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- 3. CREATE RefreshTokens TABLE
-- =============================================
CREATE TABLE IF NOT EXISTS RefreshTokens (
    RefreshTokenId CHAR(36) NOT NULL PRIMARY KEY,
    AccountId CHAR(36) NOT NULL COMMENT 'FK to Accounts',
    TokenHash VARCHAR(512) NOT NULL COMMENT 'SHA-256 hash of refresh token',
    TokenSalt VARCHAR(128) NOT NULL COMMENT 'Random salt (Base64)',
    DeviceInfo VARCHAR(500) DEFAULT NULL COMMENT 'User-Agent or device identifier',
    ExpiresAt DATETIME NOT NULL COMMENT 'Token expiry timestamp',
    RevokedAt DATETIME DEFAULT NULL COMMENT 'NULL = active, NOT NULL = revoked',
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_refresh_token_account FOREIGN KEY (AccountId)
        REFERENCES Accounts(AccountId) ON DELETE CASCADE ON UPDATE CASCADE,

    INDEX idx_rt_account_id (AccountId),
    INDEX idx_rt_token_hash (TokenHash),
    INDEX idx_rt_expires_at (ExpiresAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- Track migration
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('030_add_authentication_credentials', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
