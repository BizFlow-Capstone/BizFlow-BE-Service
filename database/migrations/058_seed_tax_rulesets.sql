-- =============================================
-- Migration 058: Seed Tax Rulesets (TT152/2025 v1.0.0)
-- =============================================

-- 1 TaxRuleset
INSERT INTO TaxRulesets (Code, Name, Description, Version, EffectiveFrom, IsActive) VALUES
('TT152_2025', 'Thông tư 152/2025/TT-BTC', 'Quy định về thuế cho hộ kinh doanh theo TT152/2025', '1.0.0', '2026-01-01', TRUE);

-- 4 TaxGroupRules (RulesetId = 1)
INSERT INTO TaxGroupRules
(RulesetId, GroupNumber, GroupName, GroupDescription, ConditionsJson, OutcomesJson, SortOrder) VALUES

-- Nhóm 1: DT < 500 triệu → miễn VAT + PIT
(1, 1, 'Nhóm 1', 'Hộ kinh doanh có doanh thu dưới 500 triệu/năm — miễn thuế GTGT và TNCN',
 '{"minRevenue":0,"maxRevenue":500000000}',
 '{"vatExempt":true,"pitExempt":true,"allowedTaxMethods":["exempt"],"defaultTaxMethod":"exempt","requiredBooks":{"default":["S1a"]},"vatReportFrequency":"quarterly","pitReportFrequency":"exempt","annualSettlement":false,"eInvoiceRequired":false}',
 1),

-- Nhóm 2: 500 triệu ≤ DT < 3 tỷ → Cách 1 hoặc Cách 2
(1, 2, 'Nhóm 2', 'Doanh thu 500 triệu - 3 tỷ/năm — chọn Cách 1 (% DT) hoặc Cách 2 (DT - CP)',
 '{"minRevenue":500000000,"maxRevenue":3000000000}',
 '{"vatExempt":false,"pitExempt":false,"allowedTaxMethods":["method_1","method_2"],"defaultTaxMethod":"method_1","pitRateMethod2":0.15,"revenueDeduction":500000000,"requiredBooks":{"method_1":["S2a"],"method_2":["S2b","S2c","S2d","S2e"]},"vatReportFrequency":"quarterly","pitReportFrequency":"quarterly","annualSettlement":true,"eInvoiceRequired":false,"eInvoiceRevenueThreshold":1000000000}',
 2),

-- Nhóm 3: 3 tỷ ≤ DT < 50 tỷ → chỉ Cách 2
(1, 3, 'Nhóm 3', 'Doanh thu 3 tỷ - 50 tỷ/năm — bắt buộc Cách 2',
 '{"minRevenue":3000000000,"maxRevenue":50000000000}',
 '{"vatExempt":false,"pitExempt":false,"allowedTaxMethods":["method_2"],"defaultTaxMethod":"method_2","pitRateMethod2":0.17,"requiredBooks":{"method_2":["S2b","S2c","S2d","S2e"]},"vatReportFrequency":"quarterly","annualSettlement":false,"eInvoiceRequired":true}',
 3),

-- Nhóm 4: DT ≥ 50 tỷ → chỉ Cách 2, báo cáo monthly
(1, 4, 'Nhóm 4', 'Doanh thu từ 50 tỷ/năm trở lên — bắt buộc Cách 2, báo cáo hàng tháng',
 '{"minRevenue":50000000000,"maxRevenue":null}',
 '{"vatExempt":false,"pitExempt":false,"allowedTaxMethods":["method_2"],"defaultTaxMethod":"method_2","pitRateMethod2":0.20,"requiredBooks":{"method_2":["S2b","S2c","S2d","S2e"]},"vatReportFrequency":"monthly","annualSettlement":false,"eInvoiceRequired":true}',
 4);

-- IndustryTaxRates (RulesetId = 1)
-- Sử dụng BusinessTypeId từ bảng BusinessTypes hiện có
-- Giả sử seed data có các BusinessTypeId tương ứng
-- Nếu chưa có thì INSERT vào BusinessTypes trước

-- Tạo BusinessTypes mẫu nếu chưa có (sử dụng UUID cố định cho consistency)
INSERT IGNORE INTO BusinessTypes (BusinessTypeId, Code, Name, Description, Status, CreatedAt, LastModifiedAt) VALUES
('11111111-1111-1111-1111-111111111001', 'bt-retail', 'Phân phối, cung cấp hàng hóa', 'Bán lẻ, bán buôn hàng hóa', 'active', NOW(), NOW()),
('11111111-1111-1111-1111-111111111002', 'bt-service', 'Dịch vụ', 'Cung cấp dịch vụ', 'active', NOW(), NOW()),
('11111111-1111-1111-1111-111111111003', 'bt-fnb', 'Sản xuất, dịch vụ gắn hàng hóa', 'F&B, sản xuất', 'active', NOW(), NOW()),
('11111111-1111-1111-1111-111111111004', 'bt-transport', 'Vận tải', 'Dịch vụ vận tải', 'active', NOW(), NOW());

-- VAT rates
INSERT INTO IndustryTaxRates (RulesetId, BusinessTypeId, TaxType, TaxRate, Description) VALUES
(1, '11111111-1111-1111-1111-111111111001', 'VAT', 0.0100, 'Phân phối, cung cấp hàng hóa: GTGT 1%'),
(1, '11111111-1111-1111-1111-111111111002', 'VAT', 0.0500, 'Dịch vụ: GTGT 5%'),
(1, '11111111-1111-1111-1111-111111111003', 'VAT', 0.0300, 'Sản xuất, dịch vụ gắn hàng hóa: GTGT 3%'),
(1, '11111111-1111-1111-1111-111111111004', 'VAT', 0.0300, 'Vận tải: GTGT 3%');

-- PIT Method 1 rates
INSERT INTO IndustryTaxRates (RulesetId, BusinessTypeId, TaxType, TaxRate, Description) VALUES
(1, '11111111-1111-1111-1111-111111111001', 'PIT_METHOD_1', 0.0050, 'Phân phối hàng hóa: TNCN 0.5%'),
(1, '11111111-1111-1111-1111-111111111002', 'PIT_METHOD_1', 0.0200, 'Dịch vụ: TNCN 2%'),
(1, '11111111-1111-1111-1111-111111111003', 'PIT_METHOD_1', 0.0150, 'Sản xuất, DV gắn hàng hóa: TNCN 1.5%'),
(1, '11111111-1111-1111-1111-111111111004', 'PIT_METHOD_1', 0.0150, 'Vận tải: TNCN 1.5%');

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('058_seed_tax_rulesets', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
