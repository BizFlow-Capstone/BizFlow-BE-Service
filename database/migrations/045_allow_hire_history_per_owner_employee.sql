-- =============================================
-- Migration 045: Allow multiple hire history records per owner-employee pair
-- =============================================

-- If idx_hire_owner_employee exists and is UNIQUE, drop it first
SET @is_unique_idx = (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'Hires'
      AND INDEX_NAME = 'idx_hire_owner_employee'
      AND NON_UNIQUE = 0
);

SET @sql = IF(
    @is_unique_idx > 0,
    'ALTER TABLE Hires DROP INDEX idx_hire_owner_employee',
    'SELECT "Unique index idx_hire_owner_employee not found or already non-unique" AS Info'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Ensure idx_hire_owner_employee exists as NON-UNIQUE index
SET @non_unique_idx_exists = (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'Hires'
      AND INDEX_NAME = 'idx_hire_owner_employee'
      AND NON_UNIQUE = 1
);

SET @sql = IF(
    @non_unique_idx_exists = 0,
    'CREATE INDEX idx_hire_owner_employee ON Hires (OwnerId, EmployeeId)',
    'SELECT "Non-unique index idx_hire_owner_employee already exists" AS Info'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Track migration
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('045_allow_hire_history_per_owner_employee', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
