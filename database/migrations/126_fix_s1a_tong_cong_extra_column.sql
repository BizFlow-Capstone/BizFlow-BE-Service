-- =============================================
-- Migration 126: Fix S1a "Tổng cộng" extra column bug
--
-- Problem:
--   Migration 092 incorrectly created a TemplateFieldMapping with
--   FieldCode = 'tong_cong' for S1a. This FieldMapping generates a
--   4th column header "Tổng cộng" in the UI, but S1a only has 3
--   columns: A (ngay_thang) | B (dien_giai) | C (so_tien).
--
--   Additionally, grand_total RowDefinitions on some cloned versions
--   have VisibleFieldCodes pointing to "tong_cong" instead of
--   "so_tien", causing the total value to render in the phantom 4th
--   column instead of the correct "Số tiền" column.
--
-- Fix:
--   1. Delete 'tong_cong' FieldMapping from ALL S1a template versions.
--   2. Update grand_total RowDefinitions for ALL S1a versions to use
--      VisibleFieldCodes = ["dien_giai","so_tien"].
--   FormulaId on the grand_total RowDefinition is preserved.
-- =============================================

-- PART 1: Remove the phantom "tong_cong" column from all S1a versions
DELETE fm
FROM TemplateFieldMappings fm
INNER JOIN AccountingTemplateVersions atv ON atv.TemplateVersionId = fm.TemplateVersionId
INNER JOIN AccountingTemplates at ON at.TemplateId = atv.TemplateId
WHERE at.TemplateCode = 'S1a'
  AND fm.FieldCode = 'tong_cong';

-- PART 2: Fix VisibleFieldCodes on grand_total rows for all S1a versions
--   so the total value renders in the "so_tien" (Số tiền) column, not
--   in the now-deleted "tong_cong" column.
UPDATE TemplateRowDefinitions rd
INNER JOIN AccountingTemplateVersions atv ON atv.TemplateVersionId = rd.TemplateVersionId
INNER JOIN AccountingTemplates at ON at.TemplateId = atv.TemplateId
SET rd.VisibleFieldCodes = '["dien_giai","so_tien"]'
WHERE at.TemplateCode = 'S1a'
  AND rd.RowType = 'grand_total';

-- =============================================
-- Migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('126_fix_s1a_tong_cong_extra_column', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
