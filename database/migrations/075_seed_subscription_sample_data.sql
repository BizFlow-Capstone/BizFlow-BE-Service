-- Migration: 051_seed_subscription_sample_data
-- Seeds Features, SubscriptionPlans, PlanFeatures, and SubscriptionPlanPrices
-- for development/testing. Idempotent via INSERT IGNORE.

-- =============================================
-- 1. Features
-- =============================================
INSERT IGNORE INTO Features (FeatureId, FeatureCode, Name, Description) VALUES
(1, 'LOCATIONS',      'Số cửa hàng',           'Số lượng cửa hàng tối đa được tạo'),
(2, 'EMPLOYEES',      'Số nhân viên',          'Số lượng nhân viên tối đa trên toàn hệ thống'),
(3, 'PRODUCTS',       'Số sản phẩm',           'Số lượng sản phẩm tối đa được quản lý'),
(4, 'ORDERS',         'Đơn hàng / tháng',      'Số lượng đơn hàng được tạo mỗi chu kỳ'),
(5, 'IMPORTS',        'Phiếu nhập / tháng',    'Số lượng phiếu nhập hàng mỗi chu kỳ'),
(6, 'REPORTS',        'Báo cáo nâng cao',      'Truy cập báo cáo doanh thu, lợi nhuận chi tiết'),
(7, 'DEBT_MGMT',      'Quản lý công nợ',       'Theo dõi công nợ khách hàng và nhà cung cấp'),
(8, 'EXPORT',         'Xuất Excel / tháng',    'Số lần xuất dữ liệu ra Excel mỗi chu kỳ');

-- =============================================
-- 2. SubscriptionPlans
-- =============================================
INSERT IGNORE INTO SubscriptionPlans (SubscriptionPlanId, Name, Description, DurationDays, IsActive) VALUES
(1, 'Miễn phí',    'Gói dùng thử cho cá nhân, tiểu thương mới bắt đầu.',                   0, TRUE),
(2, 'Cơ bản',      'Phù hợp cho cửa hàng nhỏ có 1-2 điểm bán.',                           30, TRUE),
(3, 'Nâng cao',    'Dành cho cửa hàng mở rộng, cần quản lý nhiều chi nhánh và nhân viên.', 30, TRUE),
(4, 'Doanh nghiệp','Không giới hạn — phù hợp chuỗi cửa hàng, doanh nghiệp.',              30, TRUE);

-- =============================================
-- 3. PlanFeatures  (UsageLimit: -1 = unlimited, 0 = disabled)
-- =============================================
-- Plan 1: Miễn phí
INSERT IGNORE INTO PlanFeatures (SubscriptionPlanId, FeatureId, UsageLimit) VALUES
(1, 1,   1),    -- 1 cửa hàng
(1, 2,   2),    -- 2 nhân viên
(1, 3,  50),    -- 50 sản phẩm
(1, 4, 100),    -- 100 đơn / tháng
(1, 5,  10),    -- 10 phiếu nhập / tháng
(1, 6,   0),    -- Không có báo cáo nâng cao
(1, 7,   0),    -- Không có quản lý công nợ
(1, 8,   3);    -- 3 lần xuất Excel / tháng

-- Plan 2: Cơ bản — 99,000 VND/tháng
INSERT IGNORE INTO PlanFeatures (SubscriptionPlanId, FeatureId, UsageLimit) VALUES
(2, 1,   2),    -- 2 cửa hàng
(2, 2,  10),    -- 10 nhân viên
(2, 3, 500),    -- 500 sản phẩm
(2, 4, 1000),   -- 1,000 đơn / tháng
(2, 5, 100),    -- 100 phiếu nhập / tháng
(2, 6,  -1),    -- Báo cáo nâng cao (unlimited)
(2, 7,   0),    -- Không có quản lý công nợ
(2, 8,  10);    -- 10 lần xuất Excel / tháng

-- Plan 3: Nâng cao — 249,000 VND/tháng (đang khuyến mãi 199,000)
INSERT IGNORE INTO PlanFeatures (SubscriptionPlanId, FeatureId, UsageLimit) VALUES
(3, 1,   5),    -- 5 cửa hàng
(3, 2,  50),    -- 50 nhân viên
(3, 3, 2000),   -- 2,000 sản phẩm
(3, 4, 5000),   -- 5,000 đơn / tháng
(3, 5, 500),    -- 500 phiếu nhập / tháng
(3, 6,  -1),    -- Báo cáo nâng cao (unlimited)
(3, 7,  -1),    -- Quản lý công nợ (unlimited)
(3, 8,  50);    -- 50 lần xuất Excel / tháng

-- Plan 4: Doanh nghiệp — 499,000 VND/tháng
INSERT IGNORE INTO PlanFeatures (SubscriptionPlanId, FeatureId, UsageLimit) VALUES
(4, 1,  -1),    -- Unlimited cửa hàng
(4, 2,  -1),    -- Unlimited nhân viên
(4, 3,  -1),    -- Unlimited sản phẩm
(4, 4,  -1),    -- Unlimited đơn hàng
(4, 5,  -1),    -- Unlimited phiếu nhập
(4, 6,  -1),    -- Báo cáo nâng cao (unlimited)
(4, 7,  -1),    -- Quản lý công nợ (unlimited)
(4, 8,  -1);    -- Unlimited xuất Excel

-- =============================================
-- 4. SubscriptionPlanPrices
-- =============================================
-- Plan 1: Miễn phí — 0 VND
INSERT IGNORE INTO SubscriptionPlanPrices (PriceId, SubscriptionPlanId, BasePrice, DiscountedPrice, DiscountStart, DiscountEnd, IsDiscountActive, Currency) VALUES
(1, 1, 0.00, NULL, NULL, NULL, FALSE, 'VND');

-- Plan 2: Cơ bản — 99,000 VND
INSERT IGNORE INTO SubscriptionPlanPrices (PriceId, SubscriptionPlanId, BasePrice, DiscountedPrice, DiscountStart, DiscountEnd, IsDiscountActive, Currency) VALUES
(2, 2, 99000.00, NULL, NULL, NULL, FALSE, 'VND');

-- Plan 3: Nâng cao — 249,000 VND, khuyến mãi 199,000 đến hết 30/06/2026
INSERT IGNORE INTO SubscriptionPlanPrices (PriceId, SubscriptionPlanId, BasePrice, DiscountedPrice, DiscountStart, DiscountEnd, IsDiscountActive, Currency) VALUES
(3, 3, 249000.00, 199000.00, '2026-01-01 00:00:00', '2026-06-30 23:59:59', TRUE, 'VND');

-- Plan 4: Doanh nghiệp — 499,000 VND
INSERT IGNORE INTO SubscriptionPlanPrices (PriceId, SubscriptionPlanId, BasePrice, DiscountedPrice, DiscountStart, DiscountEnd, IsDiscountActive, Currency) VALUES
(4, 4, 499000.00, NULL, NULL, NULL, FALSE, 'VND');

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('075_seed_subscription_sample_data', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
