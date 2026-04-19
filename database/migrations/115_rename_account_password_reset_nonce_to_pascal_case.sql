-- =============================================
-- Migration  : 115_rename_account_password_reset_nonce_to_pascal_case
-- Description: Rename Accounts.password_reset_nonce -> PasswordResetNonce
-- Date       : 2026-04-17
-- =============================================

SET @has_pascal_col = (
    SELECT COUNT(*)
    FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = 'Accounts'
      AND column_name = 'PasswordResetNonce'
);

SET @has_snake_col = (
    SELECT COUNT(*)
    FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = 'Accounts'
      AND column_name = 'password_reset_nonce'
);

SET @rename_sql = IF(
    @has_pascal_col > 0,
    'SELECT ''Skip: Accounts.PasswordResetNonce already exists'' AS Info',
    IF(
        @has_snake_col > 0,
        'ALTER TABLE Accounts CHANGE COLUMN `password_reset_nonce` `PasswordResetNonce` CHAR(36) NULL COMMENT ''Single-use forgot-password JWT; cleared after reset''',
        'SELECT ''Skip: Accounts.password_reset_nonce not found'' AS Info'
    )
);

PREPARE stmt_rename_password_reset_nonce FROM @rename_sql;
EXECUTE stmt_rename_password_reset_nonce;
DEALLOCATE PREPARE stmt_rename_password_reset_nonce;

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('115_rename_account_password_reset_nonce_to_pascal_case', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
