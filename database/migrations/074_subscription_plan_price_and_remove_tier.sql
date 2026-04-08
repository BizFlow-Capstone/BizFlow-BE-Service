-- Migration: 074_subscription_plan_price_and_remove_tier
-- Moves pricing from SubscriptionPlans to new SubscriptionPlanPrices table.
-- Adds Description to SubscriptionPlans. Removes Tier, BasePrice, DiscountedPrice.
-- Idempotent: safe to re-run.

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

-- 2-4. Migrate data + schema changes (wrapped in procedure for idempotency)
DROP PROCEDURE IF EXISTS migrate_074;
DELIMITER $$
CREATE PROCEDURE migrate_074()
BEGIN
    -- 2. Migrate existing prices — only if source columns still exist
    IF EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SubscriptionPlans' AND COLUMN_NAME = 'BasePrice'
    ) THEN
        INSERT INTO SubscriptionPlanPrices (SubscriptionPlanId, BasePrice, DiscountedPrice, IsDiscountActive, Currency, CreatedAt, UpdatedAt)
        SELECT sp.SubscriptionPlanId, sp.BasePrice, sp.DiscountedPrice, (sp.DiscountedPrice IS NOT NULL), 'VND', sp.CreatedAt, sp.UpdatedAt
        FROM SubscriptionPlans sp
        WHERE sp.SubscriptionPlanId NOT IN (SELECT DISTINCT SubscriptionPlanId FROM SubscriptionPlanPrices);
    END IF;

    -- 3. Add Description column if not exists
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SubscriptionPlans' AND COLUMN_NAME = 'Description'
    ) THEN
        ALTER TABLE SubscriptionPlans ADD COLUMN Description TEXT NULL AFTER Name;
    END IF;

    -- 4. Drop old columns if they still exist
    IF EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SubscriptionPlans' AND COLUMN_NAME = 'BasePrice'
    ) THEN
        ALTER TABLE SubscriptionPlans DROP COLUMN BasePrice;
    END IF;

    IF EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SubscriptionPlans' AND COLUMN_NAME = 'DiscountedPrice'
    ) THEN
        ALTER TABLE SubscriptionPlans DROP COLUMN DiscountedPrice;
    END IF;

    IF EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SubscriptionPlans' AND COLUMN_NAME = 'Tier'
    ) THEN
        ALTER TABLE SubscriptionPlans DROP COLUMN Tier;
    END IF;
END$$
DELIMITER ;

CALL migrate_074();
DROP PROCEDURE IF EXISTS migrate_074;

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('074_subscription_plan_price_and_remove_tier', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
