-- =============================================
-- Migration 089: Unlink broken tax formulas from RowDefinitions & FieldMappings
-- + Soft-deprecate single-rate tax formulas (S2A_VAT, S2A_PIT, S2B_VAT, S2C_PIT)
--
-- Why:
--   These formulas multiply TOTAL revenue by a SINGLE tax rate (FirstOrDefault from
--   IndustryTaxRates). This is incorrect for multi-industry locations. The rendering
--   pipeline now computes taxes per-group using correct per-industry rates, so these
--   formula links are no longer needed.
--
-- Affected:
--   S2A_VAT, S2A_PIT  → unlink from RowDefinitions grand_total + FieldMappings thue_gtgt/thue_tncn
--   S2B_VAT            → unlink from RowDefinitions grand_total + FieldMappings thue_gtgt
--   S2C_PIT            → unlink from RowDefinitions tax_line footer + FieldMappings thue_tncn
--
-- NOT affected (these are correct aggregates/cell-refs):
--   S2A_QUARTERLY_TOTAL, S2A_TOTAL_REVENUE_ALL, S2B_QUARTERLY_TOTAL
--   S2C_TOTAL_REVENUE, S2C_TOTAL_COST, S2C_PROFIT
--   All S2d, S2e formulas
-- =============================================

-- ═══════════════════════════════════════════════════════════
-- PART 1: Unlink FormulaId from TemplateRowDefinitions
-- ═══════════════════════════════════════════════════════════

-- S2a grand_total VAT & PIT
UPDATE TemplateRowDefinitions
SET FormulaId = NULL
WHERE TemplateVersionId = 2
  AND RowType = 'grand_total'
  AND Position = 'end_of_book'
  AND TaxType IN ('VAT', 'PIT');

-- S2b grand_total VAT
UPDATE TemplateRowDefinitions
SET FormulaId = NULL
WHERE TemplateVersionId = 3
  AND RowType = 'grand_total'
  AND Position = 'end_of_book'
  AND TaxType = 'VAT';

-- S2c tax_line PIT (footer)
UPDATE TemplateRowDefinitions
SET FormulaId = NULL
WHERE TemplateVersionId = 4
  AND RowType = 'tax_line'
  AND Position = 'end_of_book'
  AND TaxType = 'PIT';

-- ═══════════════════════════════════════════════════════════
-- PART 2: Unlink FormulaId from TemplateFieldMappings (tax columns)
-- ═══════════════════════════════════════════════════════════

-- S2a: thue_gtgt (was S2A_VAT), thue_tncn (was S2A_PIT)
UPDATE TemplateFieldMappings
SET FormulaId = NULL
WHERE TemplateVersionId = 2
  AND FieldCode IN ('thue_gtgt', 'thue_tncn')
  AND SourceType = 'formula';

-- S2b: thue_gtgt (was S2B_VAT)
UPDATE TemplateFieldMappings
SET FormulaId = NULL
WHERE TemplateVersionId = 3
  AND FieldCode = 'thue_gtgt'
  AND SourceType = 'formula';

-- S2c: thue_tncn (was S2C_PIT)
UPDATE TemplateFieldMappings
SET FormulaId = NULL
WHERE TemplateVersionId = 4
  AND FieldCode = 'thue_tncn'
  AND SourceType = 'formula';

-- ═══════════════════════════════════════════════════════════
-- PART 3: Soft-deprecate the broken tax formulas
-- Mark IsActive=0 so they're excluded from formula evaluation
-- but keep data for audit trail
-- ═══════════════════════════════════════════════════════════

UPDATE FormulaDefinitions
SET IsActive = FALSE,
    Description = CONCAT('[DEPRECATED-089] ', Description, ' — replaced by per-group rendering pipeline')
WHERE Code IN ('S2A_VAT', 'S2A_PIT', 'S2B_VAT', 'S2C_PIT');

-- ═══════════════════════════════════════════════════════════
-- Migration history
-- ═══════════════════════════════════════════════════════════
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('089_unlink_deprecate_tax_formulas', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
