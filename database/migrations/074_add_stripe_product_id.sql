-- Migration: 050_add_stripe_product_id
-- Adds StripeProductId column to SubscriptionPlans for auto-sync with Stripe.

ALTER TABLE SubscriptionPlans
    ADD COLUMN StripeProductId VARCHAR(255) NULL AFTER StripePriceId;

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('074_add_stripe_product_id', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
