-- Migration: 048_create_subscription_payment_tables
-- Adds tables for billing, subscriptions, features, and transaction tracking.
-- Updates Profiles with StripeCustomerId.
-- Optimizes UserLocationAssignments for fast entitlement queries.

-- Note: Added DROP statements to make this migration idempotent during development
SET FOREIGN_KEY_CHECKS = 0;
DROP TABLE IF EXISTS SubscriptionAuditLogs;
DROP TABLE IF EXISTS FeatureUsages;
DROP TABLE IF EXISTS Transactions;
DROP TABLE IF EXISTS Subscriptions;
DROP TABLE IF EXISTS PlanFeatures;
DROP TABLE IF EXISTS SubscriptionPlans;
DROP TABLE IF EXISTS Features;
SET FOREIGN_KEY_CHECKS = 1;

-- Safely drop StripeCustomerId if it exists
DELIMITER $$
CREATE PROCEDURE DropStripeCustomerIdIfExists()
BEGIN
    DECLARE col_exists INT;
    SELECT COUNT(*) INTO col_exists 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = DATABASE() 
      AND TABLE_NAME = 'Profiles' 
      AND COLUMN_NAME = 'StripeCustomerId';
      
    IF col_exists > 0 THEN
        ALTER TABLE Profiles DROP COLUMN StripeCustomerId;
    END IF;
END$$
DELIMITER ;
CALL DropStripeCustomerIdIfExists();
DROP PROCEDURE DropStripeCustomerIdIfExists;

-- 1. Add StripeCustomerId to Profiles
ALTER TABLE Profiles 
ADD COLUMN StripeCustomerId VARCHAR(255) NULL COMMENT 'Stripe Customer ID for payment orchestration' AFTER TaxCode;

-- 2. Create Features table
CREATE TABLE Features (
    FeatureId INT AUTO_INCREMENT PRIMARY KEY,
    FeatureCode VARCHAR(50) NOT NULL UNIQUE,
    Name VARCHAR(200) NOT NULL,
    Description TEXT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='System features available to subscription plans';

-- 3. Create SubscriptionPlans table
CREATE TABLE SubscriptionPlans (
    SubscriptionPlanId INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Tier INT NOT NULL,
    BasePrice DECIMAL(15,2) NOT NULL,
    DiscountedPrice DECIMAL(15,2) NULL,
    DurationDays INT NOT NULL DEFAULT 30,
    StripePriceId VARCHAR(255) NULL UNIQUE,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Available subscription plans';

-- 4. Create PlanFeatures mapping/limits
CREATE TABLE PlanFeatures (
    SubscriptionPlanId INT NOT NULL,
    FeatureId INT NOT NULL,
    UsageLimit INT NOT NULL DEFAULT -1,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (SubscriptionPlanId, FeatureId),
    FOREIGN KEY fk_plan_feature_plan (SubscriptionPlanId) REFERENCES SubscriptionPlans(SubscriptionPlanId) ON DELETE CASCADE,
    FOREIGN KEY fk_plan_feature_feature (FeatureId) REFERENCES Features(FeatureId) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Mapping features to subscription plans and defining usage limits';

-- 5. Create Subscriptions tracking table
CREATE TABLE Subscriptions (
    SubscriptionId CHAR(36) PRIMARY KEY,
    OwnerProfileId CHAR(36) NOT NULL,
    SubscriptionPlanId INT NOT NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'pending',
    IsAutoRenew BOOLEAN NOT NULL DEFAULT FALSE,
    StartDate DATETIME NOT NULL,
    EndDate DATETIME NOT NULL,
    LastReminderSentAt DATETIME NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY fk_subscription_owner (OwnerProfileId) REFERENCES Profiles(ProfileId),
    FOREIGN KEY fk_subscription_plan (SubscriptionPlanId) REFERENCES SubscriptionPlans(SubscriptionPlanId),
    INDEX idx_sub_owner_status (OwnerProfileId, Status),
    INDEX idx_sub_end_date_status (EndDate, Status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='User subscriptions tracking';

-- 6. Create Transactions table
CREATE TABLE Transactions (
    TransactionId CHAR(36) PRIMARY KEY,
    ProfileId CHAR(36) NOT NULL,
    SubscriptionPlanId INT NOT NULL,
    SubscriptionId CHAR(36) NULL,
    StripeCheckoutSessionId VARCHAR(255) NULL,
    StripePaymentIntentId VARCHAR(255) NULL,
    IdempotencyKey VARCHAR(100) NOT NULL UNIQUE,
    TransactionType VARCHAR(20) NOT NULL,
    PlanPrice DECIMAL(15,2) NOT NULL,
    ProrationCredit DECIMAL(15,2) NOT NULL DEFAULT 0,
    FinalAmount DECIMAL(15,2) NOT NULL,
    Currency VARCHAR(3) NOT NULL DEFAULT 'VND',
    Status VARCHAR(20) NOT NULL DEFAULT 'pending',
    PaidAt DATETIME NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY fk_transaction_profile (ProfileId) REFERENCES Profiles(ProfileId),
    FOREIGN KEY fk_transaction_plan (SubscriptionPlanId) REFERENCES SubscriptionPlans(SubscriptionPlanId),
    FOREIGN KEY fk_transaction_subscription (SubscriptionId) REFERENCES Subscriptions(SubscriptionId) ON DELETE SET NULL,
    INDEX idx_txn_stripe_session (StripeCheckoutSessionId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Payment transactions for subscriptions';

-- 7. Create FeatureUsages snapshot/sync table
CREATE TABLE FeatureUsages (
    FeatureUsageId INT AUTO_INCREMENT PRIMARY KEY,
    SubscriptionId CHAR(36) NOT NULL,
    FeatureId INT NOT NULL,
    UsedCount INT NOT NULL DEFAULT 0,
    PeriodStart DATETIME NOT NULL,
    PeriodEnd DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY fk_feature_usage_subscription (SubscriptionId) REFERENCES Subscriptions(SubscriptionId) ON DELETE CASCADE,
    FOREIGN KEY fk_feature_usage_feature (FeatureId) REFERENCES Features(FeatureId) ON DELETE CASCADE,
    UNIQUE INDEX idx_feature_usage_sub_feat (SubscriptionId, FeatureId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Tracking usage of limited features per subscription period';

-- 8. Create SubscriptionAuditLogs for tracking system checks and upgrades
CREATE TABLE SubscriptionAuditLogs (
    AuditLogId INT AUTO_INCREMENT PRIMARY KEY,
    SubscriptionId CHAR(36) NOT NULL,
    Action VARCHAR(50) NOT NULL,
    Details JSON NULL,
    PerformedBy CHAR(36) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY fk_audit_log_subscription (SubscriptionId) REFERENCES Subscriptions(SubscriptionId) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Audit trail for subscription lifecycle events';

-- 9. Add optimized index to UserLocationAssignments for 'require feature' check
CREATE INDEX idx_ula_location_owner_active ON UserLocationAssignments(BusinessLocationId, IsOwner, IsActive);
