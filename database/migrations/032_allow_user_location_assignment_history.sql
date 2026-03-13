-- Migration: 032_allow_user_location_assignment_history
-- Description: Allow multiple assignment history records for the same (UserId, BusinessLocationId).
--              Remove unique constraint idx_user_location_unique and replace with a non-unique index.
-- Date: 2026-03-13

-- =============================================
-- 1. Drop unique index that blocks re-assign history rows
-- =============================================
SET @idx_exists = (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'UserLocationAssignments'
      AND INDEX_NAME = 'idx_user_location_unique'
      AND NON_UNIQUE = 0
);

SET @sql = IF(
    @idx_exists > 0,
    'ALTER TABLE UserLocationAssignments DROP INDEX idx_user_location_unique',
    'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- =============================================
-- 2. Ensure non-unique lookup index for query performance
-- =============================================
SET @lookup_idx_exists = (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'UserLocationAssignments'
      AND INDEX_NAME = 'idx_user_location_lookup'
);

SET @sql = IF(
    @lookup_idx_exists = 0,
    'ALTER TABLE UserLocationAssignments ADD INDEX idx_user_location_lookup (UserId, BusinessLocationId)',
    'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- =============================================
-- 3. Add assignment history columns
-- =============================================
SET @assigned_at_exists = (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'UserLocationAssignments'
      AND COLUMN_NAME = 'AssignedAt'
);

SET @sql = IF(
    @assigned_at_exists = 0,
    'ALTER TABLE UserLocationAssignments ADD COLUMN AssignedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT ''When employee/user was assigned to location'' AFTER IsActive',
    'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @unassigned_at_exists = (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'UserLocationAssignments'
      AND COLUMN_NAME = 'UnassignedAt'
);

SET @sql = IF(
    @unassigned_at_exists = 0,
    'ALTER TABLE UserLocationAssignments ADD COLUMN UnassignedAt DATETIME NULL COMMENT ''When employee/user was removed from location'' AFTER AssignedAt',
    'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Backfill historical removed records (best-effort)
UPDATE UserLocationAssignments
SET UnassignedAt = COALESCE(UnassignedAt, CURRENT_TIMESTAMP)
WHERE IsActive = FALSE;

-- =============================================
-- Track migration
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('032_allow_user_location_assignment_history', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
