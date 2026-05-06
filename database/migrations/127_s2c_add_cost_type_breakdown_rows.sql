-- =============================================
-- Migration 127: S2c — add cost_type_subtotal rows (a-e breakdown)
--
-- Changes:
--   1. Extend chk_rowdef_row_type CHECK constraint to allow 'cost_type_subtotal'
--   2. Rebuild S2c (TemplateVersionId = 4) per_section rows:
--      • Keep revenue section as-is (header + data_placeholder + subtotal)
--      • Expand cost section with per-CostType aggregate rows (a–e) before
--        the existing section_subtotal
--   3. Re-link S2c FormulaIds (unchanged formulas, new SortOrders)
-- =============================================

SET NAMES utf8mb4;

-- ═══════════════════════════════════════════════════════════
-- PART 1: Extend CHECK constraint to allow 'cost_type_subtotal'
-- MySQL requires DROP + re-ADD to modify a CHECK constraint.
-- ═══════════════════════════════════════════════════════════

ALTER TABLE TemplateRowDefinitions
    DROP CONSTRAINT chk_rowdef_row_type,
    ADD CONSTRAINT chk_rowdef_row_type CHECK (
        RowType IN (
            'industry_header', 'data_placeholder', 'subtotal', 'tax_line',
            'grand_total', 'section_header', 'section_subtotal',
            'balance_row', 'monthly_total', 'quarterly_total', 'profit_row',
            'cost_type_subtotal'
        )
    );

-- ═══════════════════════════════════════════════════════════
-- PART 2: Rebuild S2c per_section rows
-- ═══════════════════════════════════════════════════════════

DELETE FROM TemplateRowDefinitions
WHERE TemplateVersionId = 4 AND Position = 'per_section';

INSERT INTO TemplateRowDefinitions
(TemplateVersionId, RowType, RowLabel, Position, SortOrder, GroupByField, SectionType, VisibleFieldCodes, FormulaId, TaxType, SectionFilterValue)
VALUES
-- ── Revenue section (unchanged) ──────────────────────────
(4, 'section_header', 'I. DOANH THU', 'per_section', 1, 'Section', 'revenue_cost', '["dien_giai"]', NULL, NULL, 'revenue'),
(4, 'data_placeholder', NULL,          'per_section', 2, 'Section', 'revenue_cost', NULL,            NULL, NULL, NULL),
(4, 'section_subtotal', 'Tong doanh thu', 'per_section', 3, 'Section', 'revenue_cost', '["dien_giai","so_tien"]', NULL, NULL, 'revenue'),

-- ── Cost section with per-CostType breakdown ─────────────
(4, 'section_header', 'II. CHI PHI HOP LY', 'per_section', 4, 'Section', 'revenue_cost', '["dien_giai"]', NULL, NULL, 'cost'),
(4, 'data_placeholder', NULL,                'per_section', 5, 'Section', 'revenue_cost', NULL,            NULL, NULL, NULL),

-- a) Nguyen lieu, vat lieu, nhien lieu, nang luong, hang hoa
(4, 'cost_type_subtotal',
 'a) Chi phi nguyen lieu, vat lieu, nhien lieu, nang luong, hang hoa su dung vao san xuat, kinh doanh',
 'per_section', 6, NULL, 'revenue_cost', '["dien_giai","so_tien"]', NULL, NULL, 'import'),

-- b) Tien luong, tien cong, BHBB
(4, 'cost_type_subtotal',
 'b) Chi phi tien luong, tien cong, cac khoan phu cap, bao hiem bat buoc va cac khoan chi tra cho nguoi lao dong',
 'per_section', 7, NULL, 'revenue_cost', '["dien_giai","so_tien"]', NULL, NULL, 'salary'),

-- c) Khau hao TSCD
(4, 'cost_type_subtotal',
 'c) Chi phi khau hao tai san co dinh phuc vu cho hoat dong san xuat, kinh doanh (neu co)',
 'per_section', 8, NULL, 'revenue_cost', '["dien_giai","so_tien"]', NULL, NULL, 'depreciation'),

-- d) Dich vu mua ngoai (dien, nuoc, dien thoai, internet, van chuyen, thue TS, sua chua, bao duong)
(4, 'cost_type_subtotal',
 'd) Chi phi dich vu mua ngoai nhu dien, nuoc, dien thoai, internet, van chuyen, thue tai san, sua chua, bao duong',
 'per_section', 9, NULL, 'revenue_cost', '["dien_giai","so_tien"]', NULL, NULL, 'utilities,transport,rent,maintenance'),

-- d) Tra lai tien vay
(4, 'cost_type_subtotal',
 'd) Chi phi tra lai tien vay von san xuat, kinh doanh theo lai suat thuc te',
 'per_section', 10, NULL, 'revenue_cost', '["dien_giai","so_tien"]', NULL, NULL, 'interest'),

-- e) Cac khoan chi khac
(4, 'cost_type_subtotal',
 'e) Cac khoan chi khac phuc vu truc tiep hoat dong san xuat, kinh doanh',
 'per_section', 11, NULL, 'revenue_cost', '["dien_giai","so_tien"]', NULL, NULL, 'other,marketing,manual'),

-- Tong chi phi hop ly (2)
(4, 'section_subtotal', 'Tong chi phi hop ly', 'per_section', 12, 'Section', 'revenue_cost', '["dien_giai","so_tien"]', NULL, NULL, 'cost');

-- ═══════════════════════════════════════════════════════════
-- PART 3: Re-link FormulaIds for S2c per_section rows
-- ═══════════════════════════════════════════════════════════

UPDATE TemplateRowDefinitions
SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2C_TOTAL_REVENUE')
WHERE TemplateVersionId = 4 AND RowType = 'section_subtotal' AND SortOrder = 3;

UPDATE TemplateRowDefinitions
SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2C_TOTAL_COST')
WHERE TemplateVersionId = 4 AND RowType = 'section_subtotal' AND SortOrder = 12;

-- ═══════════════════════════════════════════════════════════
-- Migration history
-- ═══════════════════════════════════════════════════════════
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('127_s2c_add_cost_type_breakdown_rows', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
