-- =============================================
-- Migration 059: Seed Accounting Templates + Active Versions
-- =============================================

-- 6 AccountingTemplates
INSERT INTO AccountingTemplates (TemplateCode, Name, Description, ApplicableGroups, ApplicableMethods) VALUES
('S1a', 'Sổ chi tiết bán hàng',
 'Sổ chi tiết bán hàng cho Nhóm 1 (DT < 500 triệu/năm) — miễn thuế',
 '[1]', NULL),

('S2a', 'Sổ doanh thu bán hàng hóa, dịch vụ (Cách 1)',
 'Sổ doanh thu theo ngành — tính thuế GTGT + TNCN trực tiếp trên DT. Áp dụng Nhóm 2 Cách 1.',
 '[2]', '["method_1"]'),

('S2b', 'Sổ doanh thu bán hàng hóa, dịch vụ (Cách 2)',
 'Sổ doanh thu theo ngành — chỉ tính thuế GTGT (TNCN tính ở S2c). Áp dụng Nhóm 2 Cách 2, Nhóm 3-4.',
 '[2,3,4]', '["method_2"]'),

('S2c', 'Sổ chi tiết doanh thu, chi phí',
 'Ghi doanh thu và chi phí hợp lý, tính chênh lệch = thu nhập chịu thuế TNCN. Áp dụng Nhóm 2 Cách 2, Nhóm 3-4.',
 '[2,3,4]', '["method_2"]'),

('S2d', 'Sổ chi tiết vật liệu, dụng cụ, sản phẩm, hàng hóa',
 'Sổ kho XNT — nhập/xuất/tồn, bình quân gia quyền. Mỗi sản phẩm 1 trang. Áp dụng Nhóm 2 Cách 2, Nhóm 3-4.',
 '[2,3,4]', '["method_2"]'),

('S2e', 'Sổ chi tiết tiền',
 'Theo dõi tiền mặt + tiền gửi không kỳ hạn: thu/chi, gửi/rút, tồn quỹ. Áp dụng Nhóm 2 Cách 2, Nhóm 3-4.',
 '[2,3,4]', '["method_2"]');

-- 6 Active Versions (v1.0) — 1 per template
-- Giả sử TemplateId tự tăng từ 1-6
INSERT INTO AccountingTemplateVersions (TemplateId, VersionLabel, IsActive, EffectiveFrom, ChangeNotes) VALUES
(1, 'v1.0', TRUE, '2026-01-01', 'Initial version — TT152/2025'),
(2, 'v1.0', TRUE, '2026-01-01', 'Initial version — TT152/2025'),
(3, 'v1.0', TRUE, '2026-01-01', 'Initial version — TT152/2025'),
(4, 'v1.0', TRUE, '2026-01-01', 'Initial version — TT152/2025'),
(5, 'v1.0', TRUE, '2026-01-01', 'Initial version — TT152/2025'),
(6, 'v1.0', TRUE, '2026-01-01', 'Initial version — TT152/2025');

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('059_seed_accounting_templates', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
