-- Migration: 079_add_allocated_limit_to_feature_usages
-- Adds AllocatedLimit on FeatureUsages to support quantity stacking (limit accumulates too).
-- Idempotent: safe to re-run.

DROP PROCEDURE IF EXISTS migrate_079;
DELIMITER $$
CREATE PROCEDURE migrate_079()
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'FeatureUsages' AND COLUMN_NAME = 'AllocatedLimit'
    ) THEN
        ALTER TABLE FeatureUsages ADD COLUMN AllocatedLimit INT NULL AFTER UsedCount;

        -- Backfill from PlanFeatures based on the subscription plan.
        UPDATE FeatureUsages fu
        JOIN Subscriptions s ON s.SubscriptionId = fu.SubscriptionId
        JOIN PlanFeatures pf
            ON pf.SubscriptionPlanId = s.SubscriptionPlanId
            AND pf.FeatureId = fu.FeatureId
        SET fu.AllocatedLimit = pf.UsageLimit;

        -- Safety: if anything remains NULL, set to 0 (no access).
        UPDATE FeatureUsages
        SET AllocatedLimit = COALESCE(AllocatedLimit, 0);

        -- Make it required.
        ALTER TABLE FeatureUsages MODIFY AllocatedLimit INT NOT NULL;
    END IF;
END$$
DELIMITER ;

CALL migrate_079();
DROP PROCEDURE IF EXISTS migrate_079;

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('079_add_allocated_limit_to_feature_usages', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';

