-- Chu kỳ gói miễn phí theo tháng dương lịch (app dùng DurationDays = 0; hết hạn theo Subscription.EndDate).
SET NAMES utf8mb4 COLLATE utf8mb4_unicode_ci;

UPDATE SubscriptionPlans sp
JOIN SubscriptionPlanPrices spp ON spp.SubscriptionPlanId = sp.SubscriptionPlanId
SET sp.DurationDays = 0,
    sp.Description = 'Gói miễn phí mặc định — quota reset vào mùng 1 hàng tháng (theo timezone cấu hình).',
    sp.UpdatedAt = NOW()
WHERE sp.IsActive = 1
  AND sp.DeletedAt IS NULL
  AND spp.IsActive = 1
  AND spp.BasePrice = 0
  AND sp.Name = 'Miễn phí';

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('090_free_plan_calendar_month_duration', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
