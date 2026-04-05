-- Migration: 103_add_account_password_reset_nonce
-- One-time forgot-password reset: JWT must match Accounts.password_reset_nonce until reset succeeds.

ALTER TABLE Accounts
    ADD COLUMN password_reset_nonce CHAR(36) NULL
        COMMENT 'Single-use forgot-password JWT; cleared after reset'
        AFTER DeletedAt;
