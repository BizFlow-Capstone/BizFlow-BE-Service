-- Migration: 054_add_allocated_limit_to_feature_usages
-- Adds AllocatedLimit on FeatureUsages to support quantity stacking (limit accumulates too).

ALTER TABLE FeatureUsages
    ADD COLUMN AllocatedLimit INT NULL AFTER UsedCount;

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
ALTER TABLE FeatureUsages
    MODIFY AllocatedLimit INT NOT NULL;

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('078_add_allocated_limit_to_feature_usages', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';

