-- =============================================
-- Migration 046: Add InvitedAt and make StartAt nullable for hire workflow
-- =============================================

-- 1) Add InvitedAt column if missing
SET @col_exists = (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Hires' AND COLUMN_NAME = 'InvitedAt'
);
SET @sql = IF(
    @col_exists = 0,
    'ALTER TABLE Hires ADD COLUMN InvitedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT ''Invitation timestamp'' AFTER Status',
    'SELECT "Column InvitedAt already exists" AS Info'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- 2) Ensure index for InvitedAt exists
SET @idx_exists = (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Hires' AND INDEX_NAME = 'idx_hire_invited_at'
);
SET @sql = IF(
    @idx_exists = 0,
    'CREATE INDEX idx_hire_invited_at ON Hires(InvitedAt)',
    'SELECT "Index idx_hire_invited_at already exists" AS Info'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- 3) Backfill InvitedAt from old StartAt when needed
UPDATE Hires
SET InvitedAt = COALESCE(InvitedAt, StartAt, CURRENT_TIMESTAMP)
WHERE InvitedAt IS NULL;

-- 4) Make StartAt nullable (employment starts only when accepted)
SET @is_not_null = (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'Hires'
      AND COLUMN_NAME = 'StartAt'
      AND IS_NULLABLE = 'NO'
);
SET @sql = IF(
    @is_not_null > 0,
    'ALTER TABLE Hires MODIFY COLUMN StartAt DATETIME NULL COMMENT ''Start date of employment (NULL when pending/rejected)''',
    'SELECT "StartAt is already nullable" AS Info'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- 5) Normalize pending/rejected timeline
UPDATE Hires
SET StartAt = NULL,
    EndAt = NULL
WHERE Status IN ('pending', 'rejected');

-- Track migration
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('046_hire_invited_at_and_nullable_startat', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
