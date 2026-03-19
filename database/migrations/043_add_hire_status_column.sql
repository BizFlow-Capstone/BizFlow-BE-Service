-- =============================================
-- Migration 043: Add Status column to Hires safely
-- =============================================

-- Check and add Status column if not exists
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Hires' AND COLUMN_NAME = 'Status');
SET @sql = IF(@col_exists = 0,
    'ALTER TABLE Hires ADD COLUMN Status VARCHAR(20) NOT NULL DEFAULT ''accepted'' COMMENT ''pending, accepted, rejected'' AFTER IsActive',
    'SELECT "Column Status already exists in Hires" AS Info');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Check and add Index if not exists
SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Hires' AND INDEX_NAME = 'idx_hire_status');
SET @sql = IF(@idx_exists = 0,
    'CREATE INDEX idx_hire_status ON Hires(Status)',
    'SELECT "Index idx_hire_status already exists" AS Info');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Backfill status from legacy IsActive data
UPDATE Hires
SET Status = CASE
    WHEN IsActive = TRUE THEN 'accepted'
    WHEN IsActive = FALSE THEN 'rejected'
    ELSE 'pending'
END
WHERE Status IS NULL OR Status = '' OR Status = 'accepted';

-- Track migration
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('043_add_hire_status_column', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
