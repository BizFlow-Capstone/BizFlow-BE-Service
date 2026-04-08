-- Up Migration
CREATE TABLE IF NOT EXISTS otp_codes (
    id         CHAR(36)     NOT NULL PRIMARY KEY,
    email      VARCHAR(255) NOT NULL,
    code       VARCHAR(10)  NOT NULL,
    created_at DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expired_at DATETIME     NOT NULL,
    is_used    TINYINT(1)   NOT NULL DEFAULT 0,
    INDEX idx_otp_codes_email      (email),
    INDEX idx_otp_codes_expired_at (expired_at)
);

-- Down Migration
-- DROP TABLE IF EXISTS otp_codes;
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('102_create_otp_codes_table', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);