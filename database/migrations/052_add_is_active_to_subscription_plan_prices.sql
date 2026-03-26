-- Add IsActive flag to SubscriptionPlanPrices — only one price per plan should be active at a time.

ALTER TABLE SubscriptionPlanPrices
    ADD COLUMN IsActive BOOLEAN NOT NULL DEFAULT FALSE AFTER IsDiscountActive;

-- Set the latest price per plan as active
UPDATE SubscriptionPlanPrices spp
INNER JOIN (
    SELECT SubscriptionPlanId, MAX(PriceId) AS LatestPriceId
    FROM SubscriptionPlanPrices
    GROUP BY SubscriptionPlanId
) latest ON spp.PriceId = latest.LatestPriceId
SET spp.IsActive = TRUE;

CREATE INDEX idx_price_active ON SubscriptionPlanPrices (SubscriptionPlanId, IsActive);

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('052_add_is_active_to_subscription_plan_prices', '1.0.0');
