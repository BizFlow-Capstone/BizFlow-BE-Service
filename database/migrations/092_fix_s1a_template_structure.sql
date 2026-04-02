-- =============================================
-- Migration 092: Fix S1a template structure
--
-- Problems fixed:
--   1. FieldMappings had wrong field codes (date/description/revenue)
--      and an unnecessary STT column not shown in the template form.
--   2. VisibleFieldCodes in RowDefinitions referenced old field code "revenue".
--   3. RowTypes "monthly_total"/"quarterly_total" were non-standard and
--      not handled by BookRenderingService; replaced with "grand_total".
--   4. Missing FieldMapping for "tong_cong" (linked to S1A_QUARTERLY_TOTAL).
--   5. Missing FormulaId link on RowDefinition grand_total row.
--
-- Template S1a (Mẫu số S1a-HKD) column layout:
--   A: Ngày tháng  |  B: Diễn giải  |  1: Số tiền
-- Footer: Tổng cộng
-- =============================================

-- ═══════════════════════════════════════════
-- PART 1: Fix TemplateFieldMappings (TemplateVersionId = 1)
-- ═══════════════════════════════════════════

-- 1a. Remove old incorrect mappings (idempotent)
DELETE FROM TemplateFieldMappings
WHERE TemplateVersionId = 1
  AND FieldCode IN ('stt', 'date', 'description', 'revenue', 'tong_cong');

-- 1b. Insert correct field mappings
INSERT INTO TemplateFieldMappings
(TemplateVersionId, FieldCode, FieldLabel, FieldType, SourceType, SourceEntityId, SourceFieldId, AggregationType, ExportColumn, SortOrder, IsRequired)
VALUES
-- A: Ngày tháng
(1, 'ngay_thang', 'Ngày tháng', 'date',    'query', 1, 2, 'none', 'A', 1, TRUE),
-- B: Diễn giải
(1, 'dien_giai',  'Diễn giải',  'text',    'query', 1, 3, 'none', 'B', 2, TRUE),
-- 1: Số tiền
(1, 'so_tien',    'Số tiền (1)', 'decimal', 'query', 1, 1, 'none', 'C', 3, TRUE);

-- 1c. Insert formula field for Tổng cộng (FormulaId linked in Part 3)
INSERT INTO TemplateFieldMappings
(TemplateVersionId, FieldCode, FieldLabel, FieldType, SourceType, FormulaExpression, CalculationOrder, ExportColumn, SortOrder, IsRequired)
VALUES
(1, 'tong_cong', 'Tổng cộng', 'decimal', 'formula', 'SUM(so_tien)', 1, 'C', 10, TRUE);

-- ═══════════════════════════════════════════
-- PART 2: Fix TemplateRowDefinitions (TemplateVersionId = 1)
-- ═══════════════════════════════════════════

-- 2a. Remove old non-standard row types
DELETE FROM TemplateRowDefinitions
WHERE TemplateVersionId = 1
  AND RowType IN ('monthly_total', 'quarterly_total', 'data_placeholder', 'grand_total');

-- 2b. Insert corrected row definitions
INSERT INTO TemplateRowDefinitions
(TemplateVersionId, RowType, RowLabel, Position, SortOrder, GroupByField, SectionType, VisibleFieldCodes, FormulaId, TaxType)
VALUES
-- Data rows (flat — no industry grouping for S1a)
(1, 'data_placeholder', NULL,        'per_group',  1, NULL, NULL, NULL,                        NULL, NULL),
-- Footer: Tổng cộng (FormulaId linked in Part 3)
(1, 'grand_total', 'Tổng cộng',      'end_of_book', 1, NULL, NULL, '["dien_giai","so_tien"]', NULL, NULL);

-- ═══════════════════════════════════════════
-- PART 3: Link FormulaId to FieldMapping + RowDefinition
-- ═══════════════════════════════════════════

-- Link tong_cong FieldMapping → S1A_QUARTERLY_TOTAL
UPDATE TemplateFieldMappings
SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S1A_QUARTERLY_TOTAL' LIMIT 1)
WHERE TemplateVersionId = 1 AND FieldCode = 'tong_cong' AND SourceType = 'formula';

-- Link grand_total RowDefinition → S1A_QUARTERLY_TOTAL
UPDATE TemplateRowDefinitions
SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S1A_QUARTERLY_TOTAL' LIMIT 1)
WHERE TemplateVersionId = 1 AND RowType = 'grand_total' AND Position = 'end_of_book';

-- =============================================
-- Migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('092_fix_s1a_template_structure', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
