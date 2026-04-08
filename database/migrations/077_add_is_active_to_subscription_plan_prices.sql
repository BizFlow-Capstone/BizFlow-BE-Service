-- Migration: 077_add_is_active_to_subscription_plan_prices
-- Add IsActive flag to SubscriptionPlanPrices — only one price per plan should be active at a time.
-- Idempotent: safe to re-run.

DROP PROCEDURE IF EXISTS migrate_077;
DELIMITER $$
CREATE PROCEDURE migrate_077()
BEGIN
    -- Add IsActive column if not exists
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SubscriptionPlanPrices' AND COLUMN_NAME = 'IsActive'
    ) THEN
        ALTER TABLE SubscriptionPlanPrices ADD COLUMN IsActive BOOLEAN NOT NULL DEFAULT FALSE AFTER IsDiscountActive;
    END IF;

    -- Set the latest price per plan as active (idempotent UPDATE)
    UPDATE SubscriptionPlanPrices spp
    INNER JOIN (
        SELECT SubscriptionPlanId, MAX(PriceId) AS LatestPriceId
        FROM SubscriptionPlanPrices
        GROUP BY SubscriptionPlanId
    ) latest ON spp.PriceId = latest.LatestPriceId
    SET spp.IsActive = TRUE;

    -- Add index if not exists
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SubscriptionPlanPrices' AND INDEX_NAME = 'idx_price_active'
    ) THEN
        CREATE INDEX idx_price_active ON SubscriptionPlanPrices (SubscriptionPlanId, IsActive);
    END IF;
END$$
DELIMITER ;

CALL migrate_077();
DROP PROCEDURE IF EXISTS migrate_077;

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('077_add_is_active_to_subscription_plan_prices', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
