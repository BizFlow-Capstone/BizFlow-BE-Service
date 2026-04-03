-- =============================================
-- Migration 100: Fix S2b per-group tax_line missing FormulaId link
--
-- Bug:
--   Migration 091 linked S2b grand_total VAT → S2B_VAT_V2, and correctly
--   linked S2a per_group tax_line rows, but FORGOT to link the S2b per_group
--   tax_line VAT row (TemplateVersionId = 3) to S2B_VAT_V2.
--
-- Effect of the bug:
--   Per-group tax_line rows in S2b sections show so_tien = 0 for every
--   industry group, because FormulaId is NULL → ResolveFormulaValue returns
--   null → taxAmount falls back to 0.
--   The footer grand_total row was correctly linked, so it shows the true
--   total (e.g. 1,019,000), creating an inconsistency between section rows
--   and the footer.
--
-- Fix:
--   Link S2b per_group tax_line VAT → S2B_VAT_V2 so the foreach breakdown
--   is used to populate each industry group's tax amount.
-- =============================================

UPDATE TemplateRowDefinitions
SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2B_VAT_V2')
WHERE TemplateVersionId = 3
  AND RowType = 'tax_line'
  AND TaxType = 'VAT'
  AND Position = 'per_group';

-- ═══════════════════════════════════════════════════════════
-- Migration history
-- ═══════════════════════════════════════════════════════════
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('100_fix_s2b_per_group_tax_line_formula_link', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
