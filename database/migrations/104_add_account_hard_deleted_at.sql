-- Migration: 104_add_account_hard_deleted_at
-- Purpose: Track physical purge completion (optional marker before row removal in future variants).

ALTER TABLE Accounts
    ADD COLUMN HardDeletedAt DATETIME NULL
        COMMENT 'UTC when physical purge completed; row removed after successful purge'
        AFTER PasswordResetNonce;

CREATE INDEX idx_account_hard_deleted_at ON Accounts (HardDeletedAt);

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('104_add_account_hard_deleted_at', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);

-- ═══════════════════════════════════════════════════════════
-- Migration history
-- ═══════════════════════════════════════════════════════════
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('104_add_account_hard_deleted_at', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);