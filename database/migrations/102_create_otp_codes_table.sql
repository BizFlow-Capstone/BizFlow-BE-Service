-- Up Migration
CREATE TABLE IF NOT EXISTS OtpCodes (
    Id         CHAR(36)     NOT NULL PRIMARY KEY,
    Email      VARCHAR(255) NOT NULL,
    Code       VARCHAR(10)  NOT NULL,
    CreatedAt  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ExpiredAt  DATETIME     NOT NULL,
    IsUsed     TINYINT(1)   NOT NULL DEFAULT 0,
    INDEX idx_otp_codes_email      (Email),
    INDEX idx_otp_codes_expired_at (ExpiredAt)
);

-- Down Migration
-- DROP TABLE IF EXISTS OtpCodes;
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('102_create_otp_codes_table', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);