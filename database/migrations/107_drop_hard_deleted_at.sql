-- Migration: 107_drop_hard_deleted_at
-- Purpose: Xóa cột hard_deleted_at (migration 104 cũ) nếu vẫn còn trên DB đã deploy trước đó.

SET @colExists := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Accounts' AND COLUMN_NAME = 'hard_deleted_at');

SET @dropSql := IF(@colExists > 0,
    'ALTER TABLE Accounts DROP COLUMN hard_deleted_at',
    'SELECT 1');

PREPARE dropStmt FROM @dropSql;
EXECUTE dropStmt;
DEALLOCATE PREPARE dropStmt;

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('107_drop_hard_deleted_at', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
