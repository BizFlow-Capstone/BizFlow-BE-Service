-- =============================================
-- Migration 044: Drop legacy auth columns from Accounts
-- =============================================

-- Drop indexes first
SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Accounts' AND INDEX_NAME = 'idx_account_email');
SET @sql = IF(@idx_exists > 0, 'ALTER TABLE Accounts DROP INDEX idx_account_email', 'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Accounts' AND INDEX_NAME = 'idx_account_phone');
SET @sql = IF(@idx_exists > 0, 'ALTER TABLE Accounts DROP INDEX idx_account_phone', 'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Drop columns
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Accounts' AND COLUMN_NAME = 'Email');
SET @sql = IF(@col_exists > 0, 'ALTER TABLE Accounts DROP COLUMN Email', 'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Accounts' AND COLUMN_NAME = 'Phone');
SET @sql = IF(@col_exists > 0, 'ALTER TABLE Accounts DROP COLUMN Phone', 'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Accounts' AND COLUMN_NAME = 'EmailVerified');
SET @sql = IF(@col_exists > 0, 'ALTER TABLE Accounts DROP COLUMN EmailVerified', 'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Track migration
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('044_drop_legacy_auth_columns', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
