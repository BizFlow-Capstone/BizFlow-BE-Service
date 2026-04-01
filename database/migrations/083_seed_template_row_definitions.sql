-- Migration: 083_seed_template_row_definitions
-- Seed TemplateRowDefinitions for template versions 1..6.
-- Idempotent: deletes existing seed data before re-inserting.

DELETE FROM TemplateRowDefinitions;
ALTER TABLE TemplateRowDefinitions AUTO_INCREMENT = 1;

INSERT INTO TemplateRowDefinitions
(TemplateVersionId, RowType, RowLabel, Position, SortOrder, GroupByField, SectionType, VisibleFieldCodes, FormulaId, TaxType)
VALUES
-- S1a (TemplateVersionId = 1)
(1, 'data_placeholder', NULL, 'per_group', 1, NULL, NULL, NULL, NULL, NULL),
(1, 'monthly_total', 'Cong thang {monthName}', 'end_of_book', 1, NULL, NULL, '["dien_giai","revenue"]', NULL, NULL),
(1, 'quarterly_total', 'Cong quy {quarterName}', 'end_of_book', 2, NULL, NULL, '["dien_giai","revenue"]', NULL, NULL),

-- S2a (TemplateVersionId = 2)
(2, 'industry_header', '{groupIndex}. {businessTypeName}', 'per_group', 1, 'BusinessTypeId', 'industry_group', '["dien_giai"]', NULL, NULL),
(2, 'data_placeholder', NULL, 'per_group', 2, 'BusinessTypeId', 'industry_group', NULL, NULL, NULL),
(2, 'subtotal', 'Tong cong ({groupIndex})', 'per_group', 3, 'BusinessTypeId', 'industry_group', '["dien_giai","so_tien"]', NULL, NULL),
(2, 'tax_line', 'Thue GTGT', 'per_group', 4, 'BusinessTypeId', 'industry_group', '["dien_giai","so_tien"]', NULL, 'VAT'),
(2, 'tax_line', 'Thue TNCN', 'per_group', 5, 'BusinessTypeId', 'industry_group', '["dien_giai","so_tien"]', NULL, 'PIT'),
(2, 'grand_total', 'Tong so thue GTGT phai nop', 'end_of_book', 1, NULL, NULL, '["dien_giai","so_tien"]', NULL, 'VAT'),
(2, 'grand_total', 'Tong so thue TNCN phai nop', 'end_of_book', 2, NULL, NULL, '["dien_giai","so_tien"]', NULL, 'PIT'),

-- S2b (TemplateVersionId = 3)
(3, 'industry_header', '{groupIndex}. {businessTypeName}', 'per_group', 1, 'BusinessTypeId', 'industry_group', '["dien_giai"]', NULL, NULL),
(3, 'data_placeholder', NULL, 'per_group', 2, 'BusinessTypeId', 'industry_group', NULL, NULL, NULL),
(3, 'subtotal', 'Tong cong ({groupIndex})', 'per_group', 3, 'BusinessTypeId', 'industry_group', '["dien_giai","so_tien"]', NULL, NULL),
(3, 'tax_line', 'Thue GTGT', 'per_group', 4, 'BusinessTypeId', 'industry_group', '["dien_giai","so_tien"]', NULL, 'VAT'),
(3, 'grand_total', 'Tong so thue GTGT phai nop', 'end_of_book', 1, NULL, NULL, '["dien_giai","so_tien"]', NULL, 'VAT'),

-- S2c (TemplateVersionId = 4)
(4, 'section_header', 'I. DOANH THU', 'per_section', 1, 'Section', 'revenue_cost', '["dien_giai"]', NULL, NULL),
(4, 'data_placeholder', NULL, 'per_section', 2, 'Section', 'revenue_cost', NULL, NULL, NULL),
(4, 'section_subtotal', 'Tong doanh thu', 'per_section', 3, 'Section', 'revenue_cost', '["dien_giai","so_tien"]', NULL, NULL),
(4, 'section_header', 'II. CHI PHI HOP LY', 'per_section', 4, 'Section', 'revenue_cost', '["dien_giai"]', NULL, NULL),
(4, 'data_placeholder', NULL, 'per_section', 5, 'Section', 'revenue_cost', NULL, NULL, NULL),
(4, 'section_subtotal', 'Tong chi phi hop ly', 'per_section', 6, 'Section', 'revenue_cost', '["dien_giai","so_tien"]', NULL, NULL),
(4, 'profit_row', 'III. CHENH LECH (DT - CP)', 'end_of_book', 1, NULL, NULL, '["dien_giai","so_tien"]', NULL, NULL),
(4, 'tax_line', 'Thue TNCN phai nop', 'end_of_book', 2, NULL, NULL, '["dien_giai","so_tien"]', NULL, 'PIT'),

-- S2d (TemplateVersionId = 5)
(5, 'balance_row', 'Ton dau ky', 'start_of_book', 1, 'ProductId', 'per_product', '["dien_giai","so_tien"]', NULL, NULL),
(5, 'data_placeholder', NULL, 'per_group', 2, 'ProductId', 'per_product', NULL, NULL, NULL),
(5, 'balance_row', 'Ton cuoi ky', 'end_of_book', 1, 'ProductId', 'per_product', '["dien_giai","so_tien"]', NULL, NULL),

-- S2e (TemplateVersionId = 6)
(6, 'section_header', 'I. TIEN MAT', 'per_section', 1, 'Section', 'cash_bank', '["dien_giai"]', NULL, NULL),
(6, 'balance_row', 'Ton dau ky', 'per_section', 2, 'Section', 'cash_bank', '["dien_giai","so_tien"]', NULL, NULL),
(6, 'data_placeholder', NULL, 'per_section', 3, 'Section', 'cash_bank', NULL, NULL, NULL),
(6, 'balance_row', 'Ton cuoi ky', 'per_section', 4, 'Section', 'cash_bank', '["dien_giai","so_tien"]', NULL, NULL),
(6, 'section_header', 'II. TIEN GUI NGAN HANG', 'per_section', 5, 'Section', 'cash_bank', '["dien_giai"]', NULL, NULL),
(6, 'balance_row', 'Ton dau ky', 'per_section', 6, 'Section', 'cash_bank', '["dien_giai","so_tien"]', NULL, NULL),
(6, 'data_placeholder', NULL, 'per_section', 7, 'Section', 'cash_bank', NULL, NULL, NULL),
(6, 'balance_row', 'Du cuoi ky', 'per_section', 8, 'Section', 'cash_bank', '["dien_giai","so_tien"]', NULL, NULL);

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('083_seed_template_row_definitions', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
