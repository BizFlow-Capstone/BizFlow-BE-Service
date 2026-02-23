-- Migration 019: Rename Description to Note, Date to ReceivedAt in Imports table
-- Run this BEFORE re-scaffolding the entity
-- Note: handles both PascalCase and lowercase column names (Linux MySQL is case-sensitive)

-- Rename Description -> Note (try PascalCase first, fallback covered by IF EXISTS logic below)
SET @col_desc = (
    SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME   = 'Imports'
      AND UPPER(COLUMN_NAME) = 'DESCRIPTION'
    LIMIT 1
);

SET @col_date = (
    SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME   = 'Imports'
      AND UPPER(COLUMN_NAME) = 'DATE'
    LIMIT 1
);

-- Build and execute CHANGE for Description -> Note
SET @sql1 = IF(@col_desc IS NOT NULL,
    CONCAT('ALTER TABLE `Imports` CHANGE COLUMN `', @col_desc, '` `Note` TEXT NULL'),
    'SELECT ''Column Description not found, skipping'' AS info'
);
PREPARE stmt1 FROM @sql1;
EXECUTE stmt1;
DEALLOCATE PREPARE stmt1;

-- Build and execute CHANGE for Date -> ReceivedAt
SET @sql2 = IF(@col_date IS NOT NULL,
    CONCAT('ALTER TABLE `Imports` CHANGE COLUMN `', @col_date, '` `ReceivedAt` DATETIME NULL'),
    'SELECT ''Column Date not found, skipping'' AS info'
);
PREPARE stmt2 FROM @sql2;
EXECUTE stmt2;
DEALLOCATE PREPARE stmt2;

-- Insert this migration
INSERT INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('019_rename_imports_description_and_date', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
 