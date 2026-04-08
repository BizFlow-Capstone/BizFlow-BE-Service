-- Migration: 075_add_stripe_product_id
-- Adds StripeProductId column to SubscriptionPlans for auto-sync with Stripe.
-- Idempotent: safe to re-run.

DROP PROCEDURE IF EXISTS migrate_075;
DELIMITER $$
CREATE PROCEDURE migrate_075()
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SubscriptionPlans' AND COLUMN_NAME = 'StripeProductId'
    ) THEN
        ALTER TABLE SubscriptionPlans ADD COLUMN StripeProductId VARCHAR(255) NULL AFTER StripePriceId;
    END IF;
END$$
DELIMITER ;

CALL migrate_075();
DROP PROCEDURE IF EXISTS migrate_075;

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('075_add_stripe_product_id', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
