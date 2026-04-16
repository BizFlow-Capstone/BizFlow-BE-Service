-- =============================================
-- Migration  : 114_normalize_otp_codes_table_name
-- Description: Normalize OTP table name to PascalCase plural: OtpCodes
-- Date       : 2026-04-16
-- =============================================

SET @has_pascal_otp_codes = (
    SELECT COUNT(*)
    FROM information_schema.tables
    WHERE table_schema = DATABASE()
      AND table_name = 'OtpCodes'
);

SET @has_otp_code = (
    SELECT COUNT(*)
    FROM information_schema.tables
    WHERE table_schema = DATABASE()
      AND table_name = 'otp_code'
);

SET @has_snake_otp_codes = (
    SELECT COUNT(*)
    FROM information_schema.tables
    WHERE table_schema = DATABASE()
      AND table_name = 'otp_codes'
);

SET @rename_sql = IF(
    @has_pascal_otp_codes > 0,
    'SELECT ''Skip: OtpCodes already exists'' AS Info',
    IF(
        @has_otp_code > 0,
        'RENAME TABLE otp_code TO OtpCodes',
        IF(
            @has_snake_otp_codes > 0,
            'RENAME TABLE otp_codes TO OtpCodes',
            'SELECT ''Skip: no OTP table found to rename'' AS Info'
        )
    )
);

PREPARE stmt_rename_otp_table FROM @rename_sql;
EXECUTE stmt_rename_otp_table;
DEALLOCATE PREPARE stmt_rename_otp_table;

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('114_normalize_otp_codes_table_name', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
