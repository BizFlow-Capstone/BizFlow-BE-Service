-- Migration: 089_backfill_free_subscription_for_existing_profiles
-- Chuẩn hóa gói "Miễn phí" chỉ còn 2 feature đúng FeatureCodes.cs: AI (10), LOCATIONS (1).
-- Xóa feature FREE (migration 088) và gói mẫu 076 (Cơ bản / Nâng cao / Doanh nghiệp) khi không còn FK.
-- Backfill subscription active cho profile chưa có subscription active.
-- Idempotent: có thể chạy lại; gói free luôn được reset PlanFeatures về đúng 2 dòng.

SET NAMES utf8mb4 COLLATE utf8mb4_0900_ai_ci;

-- ---------------------------------------------------------------------------
-- 0) Xóa feature FREE (nếu có): gỡ PlanFeatures trước, rồi xóa bản ghi Features.
-- ---------------------------------------------------------------------------
DELETE pf FROM PlanFeatures pf
INNER JOIN Features f ON f.FeatureId = pf.FeatureId
WHERE LOWER(f.FeatureCode) = 'free';

DELETE FROM Features
WHERE LOWER(FeatureCode) = 'free';

-- ---------------------------------------------------------------------------
-- 0b) Gỡ gói mẫu từ 076_seed_subscription_sample_data (không seed lại trong migration này).
--     Chỉ xóa khi không còn Subscriptions / Transactions tham chiếu.
-- ---------------------------------------------------------------------------
DELETE pf FROM PlanFeatures pf
INNER JOIN SubscriptionPlans sp ON sp.SubscriptionPlanId = pf.SubscriptionPlanId
WHERE sp.Name IN ('Cơ bản', 'Nâng cao', 'Doanh nghiệp')
  AND sp.DeletedAt IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM Subscriptions s WHERE s.SubscriptionPlanId = sp.SubscriptionPlanId
  )
  AND NOT EXISTS (
      SELECT 1 FROM Transactions t WHERE t.SubscriptionPlanId = sp.SubscriptionPlanId
  );

DELETE spp FROM SubscriptionPlanPrices spp
INNER JOIN SubscriptionPlans sp ON sp.SubscriptionPlanId = spp.SubscriptionPlanId
WHERE sp.Name IN ('Cơ bản', 'Nâng cao', 'Doanh nghiệp')
  AND sp.DeletedAt IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM Subscriptions s WHERE s.SubscriptionPlanId = sp.SubscriptionPlanId
  )
  AND NOT EXISTS (
      SELECT 1 FROM Transactions t WHERE t.SubscriptionPlanId = sp.SubscriptionPlanId
  );

DELETE sp FROM SubscriptionPlans sp
WHERE sp.Name IN ('Cơ bản', 'Nâng cao', 'Doanh nghiệp')
  AND sp.DeletedAt IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM Subscriptions s WHERE s.SubscriptionPlanId = sp.SubscriptionPlanId
  )
  AND NOT EXISTS (
      SELECT 1 FROM Transactions t WHERE t.SubscriptionPlanId = sp.SubscriptionPlanId
  );

-- ---------------------------------------------------------------------------
-- 1) Catalog Features — khớp 076_seed_subscription_sample_data (8 dòng) + AI;
--    không có FREE (không dùng trong FeatureCodes.cs).
-- ---------------------------------------------------------------------------
INSERT INTO Features (FeatureCode, Name, Description, CreatedAt, UpdatedAt)
SELECT 'LOCATIONS', 'Số cửa hàng', 'Số lượng cửa hàng tối đa được tạo', NOW(), NOW()
WHERE NOT EXISTS (SELECT 1 FROM Features WHERE FeatureCode = 'LOCATIONS');

INSERT INTO Features (FeatureCode, Name, Description, CreatedAt, UpdatedAt)
SELECT 'EMPLOYEES', 'Số nhân viên', 'Số lượng nhân viên tối đa trên toàn hệ thống', NOW(), NOW()
WHERE NOT EXISTS (SELECT 1 FROM Features WHERE FeatureCode = 'EMPLOYEES');

INSERT INTO Features (FeatureCode, Name, Description, CreatedAt, UpdatedAt)
SELECT 'PRODUCTS', 'Số sản phẩm', 'Số lượng sản phẩm tối đa được quản lý', NOW(), NOW()
WHERE NOT EXISTS (SELECT 1 FROM Features WHERE FeatureCode = 'PRODUCTS');

INSERT INTO Features (FeatureCode, Name, Description, CreatedAt, UpdatedAt)
SELECT 'ORDERS', 'Đơn hàng / tháng', 'Số lượng đơn hàng được tạo mỗi chu kỳ', NOW(), NOW()
WHERE NOT EXISTS (SELECT 1 FROM Features WHERE FeatureCode = 'ORDERS');

INSERT INTO Features (FeatureCode, Name, Description, CreatedAt, UpdatedAt)
SELECT 'IMPORTS', 'Phiếu nhập / tháng', 'Số lượng phiếu nhập hàng mỗi chu kỳ', NOW(), NOW()
WHERE NOT EXISTS (SELECT 1 FROM Features WHERE FeatureCode = 'IMPORTS');

INSERT INTO Features (FeatureCode, Name, Description, CreatedAt, UpdatedAt)
SELECT 'REPORTS', 'Báo cáo nâng cao', 'Truy cập báo cáo doanh thu, lợi nhuận chi tiết', NOW(), NOW()
WHERE NOT EXISTS (SELECT 1 FROM Features WHERE FeatureCode = 'REPORTS');

INSERT INTO Features (FeatureCode, Name, Description, CreatedAt, UpdatedAt)
SELECT 'DEBT_MGMT', 'Quản lý công nợ', 'Theo dõi công nợ khách hàng và nhà cung cấp', NOW(), NOW()
WHERE NOT EXISTS (SELECT 1 FROM Features WHERE FeatureCode = 'DEBT_MGMT');

INSERT INTO Features (FeatureCode, Name, Description, CreatedAt, UpdatedAt)
SELECT 'EXPORT', 'Xuất Excel / tháng', 'Số lần xuất dữ liệu ra Excel mỗi chu kỳ', NOW(), NOW()
WHERE NOT EXISTS (SELECT 1 FROM Features WHERE FeatureCode = 'EXPORT');

INSERT INTO Features (FeatureCode, Name, Description, CreatedAt, UpdatedAt)
SELECT 'AI', 'Sử dụng AI', 'Giới hạn lượt dùng AI mỗi chu kỳ', NOW(), NOW()
WHERE NOT EXISTS (SELECT 1 FROM Features WHERE FeatureCode = 'AI');

SET @feat_ai := (SELECT FeatureId FROM Features WHERE FeatureCode = 'AI' LIMIT 1);
SET @feat_loc := (SELECT FeatureId FROM Features WHERE FeatureCode = 'LOCATIONS' LIMIT 1);

-- ---------------------------------------------------------------------------
-- 2) Gói Miễn phí: tạo nếu chưa có (theo FreePlanOptions.PlanName)
-- ---------------------------------------------------------------------------
INSERT INTO SubscriptionPlans (
    Name,
    Description,
    DurationDays,
    StripeProductId,
    StripePriceId,
    IsActive,
    DeletedAt,
    CreatedAt,
    UpdatedAt
)
SELECT
    'Miễn phí',
    'Gói miễn phí mặc định — quota reset vào mùng 1 hàng tháng (theo timezone cấu hình).',
    0,
    NULL,
    NULL,
    TRUE,
    NULL,
    NOW(),
    NOW()
WHERE NOT EXISTS (
    SELECT 1
    FROM SubscriptionPlans sp
    WHERE sp.Name = 'Miễn phí'
      AND sp.DeletedAt IS NULL
);

SET @free_plan_id := (
    SELECT sp.SubscriptionPlanId
    FROM SubscriptionPlans sp
    WHERE sp.Name = 'Miễn phí'
      AND sp.DeletedAt IS NULL
    ORDER BY sp.SubscriptionPlanId
    LIMIT 1
);

-- Bật lại gói (kể cả trước đó IsActive = 0 do dữ liệu lỗi)
UPDATE SubscriptionPlans
SET IsActive = TRUE,
    UpdatedAt = NOW()
WHERE SubscriptionPlanId = @free_plan_id;

-- ---------------------------------------------------------------------------
-- 3) PlanFeatures: CHỈ AI + LOCATIONS (xóa FREE, EMPLOYEES, … khỏi gói này)
-- ---------------------------------------------------------------------------
DELETE FROM PlanFeatures
WHERE SubscriptionPlanId = @free_plan_id;

INSERT INTO PlanFeatures (SubscriptionPlanId, FeatureId, UsageLimit, CreatedAt)
VALUES
    (@free_plan_id, @feat_ai, 10, NOW()),
    (@free_plan_id, @feat_loc, 1, NOW());

-- ---------------------------------------------------------------------------
-- 4) Giá 0 VND active cho gói miễn phí
-- ---------------------------------------------------------------------------
UPDATE SubscriptionPlanPrices
SET IsActive = FALSE,
    UpdatedAt = NOW()
WHERE SubscriptionPlanId = @free_plan_id;

INSERT INTO SubscriptionPlanPrices (
    SubscriptionPlanId,
    BasePrice,
    DiscountedPrice,
    DiscountStart,
    DiscountEnd,
    IsDiscountActive,
    IsActive,
    Currency,
    CreatedAt,
    UpdatedAt
)
SELECT
    @free_plan_id,
    0,
    NULL,
    NULL,
    NULL,
    FALSE,
    TRUE,
    'VND',
    NOW(),
    NOW()
FROM DUAL
WHERE NOT EXISTS (
    SELECT 1
    FROM SubscriptionPlanPrices spp
    WHERE spp.SubscriptionPlanId = @free_plan_id
      AND spp.BasePrice = 0
);

UPDATE SubscriptionPlanPrices spp
INNER JOIN (
    SELECT MIN(PriceId) AS PriceId
    FROM SubscriptionPlanPrices
    WHERE SubscriptionPlanId = @free_plan_id
      AND BasePrice = 0
) z ON z.PriceId = spp.PriceId
SET spp.IsActive = TRUE,
    spp.UpdatedAt = NOW();

-- ---------------------------------------------------------------------------
-- 5) Backfill Subscriptions + FeatureUsages (profile chưa có subscription active)
-- ---------------------------------------------------------------------------
DROP TEMPORARY TABLE IF EXISTS tmp_free_subscription_backfill;
CREATE TEMPORARY TABLE tmp_free_subscription_backfill (
    SubscriptionId CHAR(36) COLLATE utf8mb4_0900_ai_ci NOT NULL PRIMARY KEY,
    OwnerProfileId CHAR(36) COLLATE utf8mb4_0900_ai_ci NOT NULL,
    StartDate DATETIME NOT NULL,
    EndDate DATETIME NOT NULL
);

INSERT INTO tmp_free_subscription_backfill (SubscriptionId, OwnerProfileId, StartDate, EndDate)
SELECT
    UUID() AS SubscriptionId,
    p.ProfileId AS OwnerProfileId,
    NOW() AS StartDate,
    CASE
        WHEN sp.DurationDays > 0 THEN DATE_ADD(NOW(), INTERVAL sp.DurationDays DAY)
        -- Chu kỳ lịch (DurationDays = 0): kết thúc kỳ = 00:00 ngày đầu tháng sau (gần với app)
        ELSE TIMESTAMP(DATE_ADD(LAST_DAY(NOW()), INTERVAL 1 DAY))
    END AS EndDate
FROM Profiles p
JOIN SubscriptionPlans sp ON sp.SubscriptionPlanId = @free_plan_id
WHERE @free_plan_id IS NOT NULL
  AND @feat_ai IS NOT NULL
  AND @feat_loc IS NOT NULL
  AND NOT EXISTS (
      SELECT 1
      FROM Subscriptions s
      WHERE s.OwnerProfileId COLLATE utf8mb4_0900_ai_ci = p.ProfileId COLLATE utf8mb4_0900_ai_ci
        AND LOWER(s.Status COLLATE utf8mb4_0900_ai_ci) = 'active'
  );

INSERT INTO Subscriptions (
    SubscriptionId,
    OwnerProfileId,
    SubscriptionPlanId,
    Status,
    IsAutoRenew,
    StartDate,
    EndDate,
    CreatedAt,
    UpdatedAt
)
SELECT
    t.SubscriptionId,
    t.OwnerProfileId,
    @free_plan_id,
    'active',
    0,
    t.StartDate,
    t.EndDate,
    NOW(),
    NOW()
FROM tmp_free_subscription_backfill t;

INSERT INTO FeatureUsages (
    SubscriptionId,
    FeatureId,
    UsedCount,
    AllocatedLimit,
    PeriodStart,
    PeriodEnd,
    UpdatedAt
)
SELECT
    t.SubscriptionId,
    pf.FeatureId,
    0,
    pf.UsageLimit,
    t.StartDate,
    t.EndDate,
    NOW()
FROM tmp_free_subscription_backfill t
JOIN PlanFeatures pf ON pf.SubscriptionPlanId = @free_plan_id
WHERE NOT EXISTS (
    SELECT 1
    FROM FeatureUsages fu
    WHERE fu.SubscriptionId COLLATE utf8mb4_0900_ai_ci = t.SubscriptionId COLLATE utf8mb4_0900_ai_ci
      AND fu.FeatureId = pf.FeatureId
);

INSERT INTO SubscriptionAuditLogs (
    SubscriptionId,
    Action,
    Details,
    CreatedAt
)
SELECT
    t.SubscriptionId,
    'activated',
    JSON_OBJECT('source', 'migration_089_free_backfill', 'features', 'AI,LOCATIONS'),
    NOW()
FROM tmp_free_subscription_backfill t;

DROP TEMPORARY TABLE IF EXISTS tmp_free_subscription_backfill;

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('089_backfill_free_subscription_for_existing_profiles', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
