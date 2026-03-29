ALTER TABLE SubscriptionPlans
    ADD COLUMN DeletedAt DATETIME NULL AFTER IsActive;

CREATE INDEX idx_plan_deleted_at ON SubscriptionPlans (DeletedAt);

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('078_add_deleted_at_to_subscription_plans', '1.0.0');
