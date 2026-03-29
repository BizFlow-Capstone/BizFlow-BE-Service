-- Migration: 049_subscription_plan_price_and_remove_tier
-- Moves pricing from SubscriptionPlans to new SubscriptionPlanPrices table.
-- Adds Description to SubscriptionPlans. Removes Tier, BasePrice, DiscountedPrice.

-- 1. Create SubscriptionPlanPrices table
CREATE TABLE IF NOT EXISTS SubscriptionPlanPrices (
    PriceId              INT AUTO_INCREMENT PRIMARY KEY,
    SubscriptionPlanId   INT NOT NULL,
    BasePrice            DECIMAL(15,2) NOT NULL,
    DiscountedPrice      DECIMAL(15,2) NULL,
    DiscountStart        DATETIME NULL,
    DiscountEnd          DATETIME NULL,
    IsDiscountActive     BOOLEAN NOT NULL DEFAULT FALSE,
    Currency             VARCHAR(3) NOT NULL DEFAULT 'VND',
    CreatedAt            DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt            DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY fk_price_plan (SubscriptionPlanId) REFERENCES SubscriptionPlans(SubscriptionPlanId) ON DELETE CASCADE,
    INDEX idx_price_plan (SubscriptionPlanId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Price history for subscription plans';

-- 2. Migrate existing prices into SubscriptionPlanPrices
INSERT INTO SubscriptionPlanPrices (SubscriptionPlanId, BasePrice, DiscountedPrice, IsDiscountActive, Currency, CreatedAt, UpdatedAt)
SELECT SubscriptionPlanId, BasePrice, DiscountedPrice, (DiscountedPrice IS NOT NULL), 'VND', CreatedAt, UpdatedAt
FROM SubscriptionPlans;

-- 3. Add Description column
ALTER TABLE SubscriptionPlans ADD COLUMN Description TEXT NULL AFTER Name;

-- 4. Drop old columns
ALTER TABLE SubscriptionPlans DROP COLUMN BasePrice;
ALTER TABLE SubscriptionPlans DROP COLUMN DiscountedPrice;
ALTER TABLE SubscriptionPlans DROP COLUMN Tier;

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('074_subscription_plan_price_and_remove_tier', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
