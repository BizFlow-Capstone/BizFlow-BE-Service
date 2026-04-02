-- =============================================
-- Migration 091: Create v2 tax formulas using foreach node
-- Replaces hardcoded per-industry tax computation in BookRenderingService
-- with data-driven formulas. Re-links FieldMappings and RowDefinitions.
--
-- New formulas:
--   S2A_VAT_V2   — VAT Cách 1: foreach industry → group_amount × VAT rate → SUM
--   S2A_PIT_V2   — PIT Cách 1: foreach industry → group_amount × PIT rate → SUM (threshold 500M)
--   S2B_VAT_V2   — VAT Cách 2: same pattern as S2A_VAT_V2
--   S2C_PIT_V2   — PIT Cách 2: foreach industry → MAX(0, group_amount - group_cost) × PIT rate → SUM
-- =============================================

-- ═══════════════════════════════════════════════════════════
-- PART 1: Insert new v2 formulas
-- ═══════════════════════════════════════════════════════════

INSERT INTO FormulaDefinitions (Code, Name, Description, FormulaType, ExpressionJson, ResultDataType, RoundingPrecision, IsActive) VALUES

-- S2A_VAT_V2: VAT per-industry for Cách 1
('S2A_VAT_V2', 'Thuế GTGT per-industry — S2a',
 'Σ(doanh_thu_i × VAT_rate_i) — tính thuế GTGT theo từng ngành nghề',
 'TAX_RATE',
 '{"foreach":"industry","source":"revenues","field":"Amount","groupBy":"BusinessTypeId","apply":{"op":"MULTIPLY","left":{"context":"group_amount"},"right":{"lookup":{"entity":"IndustryTaxRates","field":"TaxRate","filter":{"TaxType":"VAT"}}}},"reduce":"SUM"}',
 'decimal', 0, TRUE),

-- S2A_PIT_V2: PIT per-industry for Cách 1 (threshold 500M)
('S2A_PIT_V2', 'Thuế TNCN per-industry — S2a',
 'Σ(doanh_thu_i × PIT_rate_i) nếu tổng DT > 500 triệu, ngược lại = 0',
 'TAX_RATE',
 '{"foreach":"industry","source":"revenues","field":"Amount","groupBy":"BusinessTypeId","apply":{"op":"MULTIPLY","left":{"context":"group_amount"},"right":{"lookup":{"entity":"IndustryTaxRates","field":"TaxRate","filter":{"TaxType":"PIT_METHOD_1"}}}},"reduce":"SUM","threshold":{"check":"total_amount","min":500000000,"elseValue":0}}',
 'decimal', 0, TRUE),

-- S2B_VAT_V2: VAT per-industry for Cách 2 (same as S2A but separate for template isolation)
('S2B_VAT_V2', 'Thuế GTGT per-industry — S2b',
 'Σ(doanh_thu_i × VAT_rate_i) — tính thuế GTGT theo từng ngành nghề',
 'TAX_RATE',
 '{"foreach":"industry","source":"revenues","field":"Amount","groupBy":"BusinessTypeId","apply":{"op":"MULTIPLY","left":{"context":"group_amount"},"right":{"lookup":{"entity":"IndustryTaxRates","field":"TaxRate","filter":{"TaxType":"VAT"}}}},"reduce":"SUM"}',
 'decimal', 0, TRUE),

-- S2C_PIT_V2: PIT per-industry for Cách 2 (profit-based)
('S2C_PIT_V2', 'Thuế TNCN per-industry — S2c',
 'Σ MAX(0, doanh_thu_i - chi_phi_i) × PIT_rate_i — tính thuế TNCN theo lợi nhuận từng ngành',
 'TAX_RATE',
 '{"foreach":"industry","source":"revenues","field":"Amount","groupBy":"BusinessTypeId","costSource":"costs","costField":"Amount","apply":{"op":"MULTIPLY","left":{"fn":"MAX","args":[{"literal":0},{"op":"SUBTRACT","left":{"context":"group_amount"},"right":{"context":"group_cost"}}]},"right":{"lookup":{"entity":"IndustryTaxRates","field":"TaxRate","filter":{"TaxType":"PIT_METHOD_1"}}}},"reduce":"SUM"}',
 'decimal', 0, TRUE);

-- ═══════════════════════════════════════════════════════════
-- PART 2: Link v2 formulas to TemplateFieldMappings
-- ═══════════════════════════════════════════════════════════

-- S2a: thue_gtgt → S2A_VAT_V2
UPDATE TemplateFieldMappings
SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2A_VAT_V2')
WHERE TemplateVersionId = 2 AND FieldCode = 'thue_gtgt' AND SourceType = 'formula';

-- S2a: thue_tncn → S2A_PIT_V2
UPDATE TemplateFieldMappings
SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2A_PIT_V2')
WHERE TemplateVersionId = 2 AND FieldCode = 'thue_tncn' AND SourceType = 'formula';

-- S2b: thue_gtgt → S2B_VAT_V2
UPDATE TemplateFieldMappings
SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2B_VAT_V2')
WHERE TemplateVersionId = 3 AND FieldCode = 'thue_gtgt' AND SourceType = 'formula';

-- S2c: thue_tncn → S2C_PIT_V2
UPDATE TemplateFieldMappings
SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2C_PIT_V2')
WHERE TemplateVersionId = 4 AND FieldCode = 'thue_tncn' AND SourceType = 'formula';

-- ═══════════════════════════════════════════════════════════
-- PART 3: Link v2 formulas to TemplateRowDefinitions
-- ═══════════════════════════════════════════════════════════

-- S2a: grand_total VAT → S2A_VAT_V2
UPDATE TemplateRowDefinitions
SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2A_VAT_V2')
WHERE TemplateVersionId = 2 AND RowType = 'grand_total' AND TaxType = 'VAT' AND Position = 'end_of_book';

-- S2a: grand_total PIT → S2A_PIT_V2
UPDATE TemplateRowDefinitions
SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2A_PIT_V2')
WHERE TemplateVersionId = 2 AND RowType = 'grand_total' AND TaxType = 'PIT' AND Position = 'end_of_book';

-- S2a: per_group tax_line VAT → S2A_VAT_V2
UPDATE TemplateRowDefinitions
SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2A_VAT_V2')
WHERE TemplateVersionId = 2 AND RowType = 'tax_line' AND TaxType = 'VAT' AND Position = 'per_group';

-- S2a: per_group tax_line PIT → S2A_PIT_V2
UPDATE TemplateRowDefinitions
SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2A_PIT_V2')
WHERE TemplateVersionId = 2 AND RowType = 'tax_line' AND TaxType = 'PIT' AND Position = 'per_group';

-- S2b: grand_total VAT → S2B_VAT_V2
UPDATE TemplateRowDefinitions
SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2B_VAT_V2')
WHERE TemplateVersionId = 3 AND RowType = 'grand_total' AND TaxType = 'VAT' AND Position = 'end_of_book';

-- S2c: tax_line PIT footer → S2C_PIT_V2
UPDATE TemplateRowDefinitions
SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2C_PIT_V2')
WHERE TemplateVersionId = 4 AND RowType = 'tax_line' AND TaxType = 'PIT' AND Position = 'end_of_book';

-- ═══════════════════════════════════════════════════════════
-- Migration history
-- ═══════════════════════════════════════════════════════════
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('091_create_v2_foreach_tax_formulas', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
