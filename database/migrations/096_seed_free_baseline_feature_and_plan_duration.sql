-- Migration: 083_seed_free_baseline_feature_and_plan_duration
-- Ensure baseline FREE feature exists and free plans are non-expiring (DurationDays = -1).

SET NAMES utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 1) Đảm bảo tồn tại feature FREE (dùng làm quyền cơ bản cho tất cả API đăng nhập)
INSERT INTO Features (FeatureCode, Name, Description, CreatedAt, UpdatedAt)
SELECT 'FREE', 'Gói miễn phí (mặc định)', 'Quyền cơ bản miễn phí cho tất cả tài khoản mới', NOW(), NOW()
WHERE NOT EXISTS (
    SELECT 1
    FROM Features
    WHERE LOWER(FeatureCode) = 'free'
);

-- 2) Gắn feature FREE vào tất cả gói đang active (baseline cho API đã đăng nhập)
INSERT INTO PlanFeatures (SubscriptionPlanId, FeatureId, UsageLimit, CreatedAt)
SELECT sp.SubscriptionPlanId, f.FeatureId, -1, NOW()
FROM SubscriptionPlans sp
JOIN Features f ON LOWER(f.FeatureCode) = 'free'
WHERE sp.IsActive = 1
  AND sp.DeletedAt IS NULL
  AND NOT EXISTS (
      SELECT 1
      FROM PlanFeatures pf
      WHERE pf.SubscriptionPlanId = sp.SubscriptionPlanId
        AND pf.FeatureId = f.FeatureId
  );

-- 3) Các gói có giá 0 VND sẽ được coi là gói free, không giới hạn thời gian (DurationDays = -1)
UPDATE SubscriptionPlans sp
JOIN SubscriptionPlanPrices spp ON spp.SubscriptionPlanId = sp.SubscriptionPlanId
SET sp.DurationDays = -1,
    sp.UpdatedAt = NOW()
WHERE sp.IsActive = 1
  AND sp.DeletedAt IS NULL
  AND spp.IsActive = 1
  AND spp.BasePrice = 0
  AND sp.DurationDays >= 0;

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('096_seed_free_baseline_feature_and_plan_duration', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
