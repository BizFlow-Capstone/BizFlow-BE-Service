-- Migration: 078_add_deleted_at_to_subscription_plans
-- Adds soft-delete column to SubscriptionPlans.
-- Idempotent: safe to re-run.

DROP PROCEDURE IF EXISTS migrate_078;
DELIMITER $$
CREATE PROCEDURE migrate_078()
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SubscriptionPlans' AND COLUMN_NAME = 'DeletedAt'
    ) THEN
        ALTER TABLE SubscriptionPlans ADD COLUMN DeletedAt DATETIME NULL AFTER IsActive;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SubscriptionPlans' AND INDEX_NAME = 'idx_plan_deleted_at'
    ) THEN
        CREATE INDEX idx_plan_deleted_at ON SubscriptionPlans (DeletedAt);
    END IF;
END$$
DELIMITER ;

CALL migrate_078();
DROP PROCEDURE IF EXISTS migrate_078;

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('078_add_deleted_at_to_subscription_plans', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
