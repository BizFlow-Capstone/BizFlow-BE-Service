-- Migration: 081_link_formula_ids_to_row_definitions
-- Link FormulaId on TemplateRowDefinitions so rendering is data-driven (not hardcoded).

-- ═══ S2a (TemplateVersionId = 2) — grand totals ═══
UPDATE TemplateRowDefinitions
SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2A_VAT')
WHERE TemplateVersionId = 2 AND RowType = 'grand_total' AND TaxType = 'VAT' AND Position = 'end_of_book';

UPDATE TemplateRowDefinitions
SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2A_PIT')
WHERE TemplateVersionId = 2 AND RowType = 'grand_total' AND TaxType = 'PIT' AND Position = 'end_of_book';

-- ═══ S2b (TemplateVersionId = 3) — grand total ═══
UPDATE TemplateRowDefinitions
SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2B_VAT')
WHERE TemplateVersionId = 3 AND RowType = 'grand_total' AND TaxType = 'VAT' AND Position = 'end_of_book';

-- ═══ S2c (TemplateVersionId = 4) — section subtotals + footer ═══
UPDATE TemplateRowDefinitions
SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2C_TOTAL_REVENUE')
WHERE TemplateVersionId = 4 AND RowType = 'section_subtotal' AND SortOrder = 3;

UPDATE TemplateRowDefinitions
SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2C_TOTAL_COST')
WHERE TemplateVersionId = 4 AND RowType = 'section_subtotal' AND SortOrder = 6;

UPDATE TemplateRowDefinitions
SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2C_PROFIT')
WHERE TemplateVersionId = 4 AND RowType = 'profit_row' AND Position = 'end_of_book';

UPDATE TemplateRowDefinitions
SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2C_PIT')
WHERE TemplateVersionId = 4 AND RowType = 'tax_line' AND Position = 'end_of_book';

-- ═══ S2e (TemplateVersionId = 6) — balance rows ═══
-- Cash opening / closing
UPDATE TemplateRowDefinitions
SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2E_CASH_OPENING')
WHERE TemplateVersionId = 6 AND RowType = 'balance_row' AND SortOrder = 2 AND Position = 'per_section';

UPDATE TemplateRowDefinitions
SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2E_CASH_CLOSING')
WHERE TemplateVersionId = 6 AND RowType = 'balance_row' AND SortOrder = 4 AND Position = 'per_section';

-- Bank opening / closing
UPDATE TemplateRowDefinitions
SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2E_BANK_OPENING')
WHERE TemplateVersionId = 6 AND RowType = 'balance_row' AND SortOrder = 6 AND Position = 'per_section';

UPDATE TemplateRowDefinitions
SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2E_BANK_CLOSING')
WHERE TemplateVersionId = 6 AND RowType = 'balance_row' AND SortOrder = 8 AND Position = 'per_section';

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('081_link_formula_ids_to_row_definitions', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
