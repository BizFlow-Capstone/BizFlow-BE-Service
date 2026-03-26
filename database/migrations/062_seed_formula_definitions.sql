-- =============================================
-- Migration 062: Seed FormulaDefinitions (29 formulas for TT152)
-- =============================================

-- ═══ S1a — Sổ chi tiết bán hàng (2 formulas) ═══
INSERT INTO FormulaDefinitions (Code, Name, Description, FormulaType, ExpressionJson, ResultDataType, RoundingPrecision) VALUES

('S1A_MONTHLY_TOTAL', 'Cộng tháng — S1a',
 'SUM doanh thu theo tháng cho Nhóm 1',
 'AGGREGATE',
 '{"aggregate":"SUM","source":"revenues","field":"Amount","scope":"location","filter":{"RevenueType":["sale","manual"]}}',
 'decimal', 0),

('S1A_QUARTERLY_TOTAL', 'Cộng quý — S1a',
 'SUM doanh thu cả quý cho Nhóm 1',
 'AGGREGATE',
 '{"aggregate":"SUM","source":"revenues","field":"Amount","scope":"location","filter":{"RevenueType":["sale","manual"]}}',
 'decimal', 0),


-- ═══ S2a — Sổ doanh thu Cách 1 (4 formulas) ═══

('S2A_QUARTERLY_TOTAL', 'Cộng quý DT — S2a',
 'SUM doanh thu theo ngành (scope: book) cho S2a Cách 1',
 'AGGREGATE',
 '{"aggregate":"SUM","source":"revenues","field":"Amount","scope":"book","filter":{"RevenueType":["sale","manual"]}}',
 'decimal', 0),

('S2A_TOTAL_REVENUE_ALL', 'Tổng DT toàn location — S2a',
 'SUM doanh thu toàn location (scope: location) — dùng cho PIT threshold 500 triệu',
 'AGGREGATE',
 '{"aggregate":"SUM","source":"revenues","field":"Amount","scope":"location","filter":{"RevenueType":["sale","manual"]}}',
 'decimal', 0),

('S2A_VAT', 'Thuế GTGT — S2a',
 'Tổng DT ngành × VAT rate',
 'TAX_RATE',
 '{"op":"MULTIPLY","left":{"ref":"S2A_QUARTERLY_TOTAL"},"right":{"lookup":{"entity":"IndustryTaxRates","field":"TaxRate","filter":{"TaxType":"VAT"}}}}',
 'decimal', 0),

('S2A_PIT', 'Thuế TNCN — S2a',
 'MAX(0, DT toàn bộ - 500 triệu) × PIT rate',
 'TAX_RATE',
 '{"op":"MULTIPLY","left":{"fn":"MAX","args":[{"literal":0},{"op":"SUBTRACT","left":{"ref":"S2A_TOTAL_REVENUE_ALL"},"right":{"literal":500000000}}]},"right":{"lookup":{"entity":"IndustryTaxRates","field":"TaxRate","filter":{"TaxType":"PIT_M1"}}}}',
 'decimal', 0),


-- ═══ S2b — Sổ doanh thu Cách 2 (2 formulas) ═══

('S2B_QUARTERLY_TOTAL', 'Cộng quý DT — S2b',
 'SUM doanh thu theo ngành cho S2b Cách 2',
 'AGGREGATE',
 '{"aggregate":"SUM","source":"revenues","field":"Amount","scope":"book","filter":{"RevenueType":["sale","manual"]}}',
 'decimal', 0),

('S2B_VAT', 'Thuế GTGT — S2b',
 'DT ngành × VAT rate (không có PIT — TNCN tính ở S2c)',
 'TAX_RATE',
 '{"op":"MULTIPLY","left":{"ref":"S2B_QUARTERLY_TOTAL"},"right":{"lookup":{"entity":"IndustryTaxRates","field":"TaxRate","filter":{"TaxType":"VAT"}}}}',
 'decimal', 0),


-- ═══ S2c — Sổ DT, CP (4 formulas) ═══

('S2C_TOTAL_REVENUE', 'Tổng doanh thu — S2c',
 'SUM toàn bộ doanh thu (location scope)',
 'AGGREGATE',
 '{"aggregate":"SUM","source":"revenues","field":"Amount","scope":"location","filter":{"RevenueType":["sale","manual"]}}',
 'decimal', 0),

('S2C_TOTAL_COST', 'Tổng chi phí hợp lý — S2c',
 'SUM toàn bộ chi phí',
 'AGGREGATE',
 '{"aggregate":"SUM","source":"costs","field":"Amount","scope":"location"}',
 'decimal', 0),

('S2C_PROFIT', 'Chênh lệch DT - CP — S2c',
 'Tổng doanh thu trừ tổng chi phí = thu nhập chịu thuế TNCN',
 'CELL_REF',
 '{"op":"SUBTRACT","left":{"ref":"S2C_TOTAL_REVENUE"},"right":{"ref":"S2C_TOTAL_COST"}}',
 'decimal', 0),

('S2C_PIT', 'Thuế TNCN Cách 2 — S2c',
 'MAX(0, chênh lệch) × PIT rate Cách 2',
 'TAX_RATE',
 '{"op":"MULTIPLY","left":{"fn":"MAX","args":[{"literal":0},{"ref":"S2C_PROFIT"}]},"right":{"lookup":{"entity":"IndustryTaxRates","field":"TaxRate","filter":{"TaxType":"PIT_M1"}}}}',
 'decimal', 0),


-- ═══ S2d — Sổ kho XNT (9 formulas, per product) ═══

('S2D_OPENING_QTY', 'SL tồn đầu kỳ — S2d',
 'SUM QuantityDelta trước kỳ (per product)',
 'AGGREGATE',
 '{"aggregate":"SUM","source":"stock_movements","field":"QuantityDelta","scope":"book","periodFilter":"before"}',
 'decimal', 2),

('S2D_OPENING_VALUE', 'Giá trị tồn đầu kỳ — S2d',
 'SUM TotalValue nhập trước kỳ (per product)',
 'AGGREGATE',
 '{"aggregate":"SUM","source":"stock_movements","field":"TotalValue","scope":"book","periodFilter":"before","sign":"positive"}',
 'decimal', 0),

('S2D_IMPORT_QTY', 'SL nhập trong kỳ — S2d',
 'SUM QuantityDelta dương trong kỳ (per product)',
 'AGGREGATE',
 '{"aggregate":"SUM","source":"stock_movements","field":"QuantityDelta","scope":"book","periodFilter":"current","sign":"positive"}',
 'decimal', 2),

('S2D_IMPORT_VALUE', 'Giá trị nhập trong kỳ — S2d',
 'SUM TotalValue nhập trong kỳ (per product)',
 'AGGREGATE',
 '{"aggregate":"SUM","source":"stock_movements","field":"TotalValue","scope":"book","periodFilter":"current","sign":"positive"}',
 'decimal', 0),

('S2D_WEIGHTED_AVG', 'Đơn giá xuất kho BQ gia quyền — S2d',
 '(Giá trị tồn ĐK + Giá trị nhập) / (SL tồn ĐK + SL nhập)',
 'WEIGHTED_AVG',
 '{"op":"DIVIDE","left":{"op":"ADD","left":{"ref":"S2D_OPENING_VALUE"},"right":{"ref":"S2D_IMPORT_VALUE"}},"right":{"op":"ADD","left":{"ref":"S2D_OPENING_QTY"},"right":{"ref":"S2D_IMPORT_QTY"}}}',
 'decimal', 0),

('S2D_EXPORT_QTY', 'SL xuất trong kỳ — S2d',
 'ABS(SUM QuantityDelta âm trong kỳ) (per product)',
 'AGGREGATE',
 '{"fn":"ABS","args":[{"aggregate":"SUM","source":"stock_movements","field":"QuantityDelta","scope":"book","periodFilter":"current","sign":"negative"}]}',
 'decimal', 2),

('S2D_EXPORT_VALUE', 'Giá trị xuất trong kỳ — S2d',
 'SL xuất × đơn giá BQ gia quyền',
 'CELL_REF',
 '{"op":"MULTIPLY","left":{"ref":"S2D_EXPORT_QTY"},"right":{"ref":"S2D_WEIGHTED_AVG"}}',
 'decimal', 0),

('S2D_CLOSING_QTY', 'SL tồn cuối kỳ — S2d',
 'Tồn ĐK + Nhập - Xuất',
 'CELL_REF',
 '{"op":"SUBTRACT","left":{"op":"ADD","left":{"ref":"S2D_OPENING_QTY"},"right":{"ref":"S2D_IMPORT_QTY"}},"right":{"ref":"S2D_EXPORT_QTY"}}',
 'decimal', 2),

('S2D_CLOSING_VALUE', 'Giá trị tồn cuối kỳ — S2d',
 'SL tồn CK × đơn giá BQ',
 'CELL_REF',
 '{"op":"MULTIPLY","left":{"ref":"S2D_CLOSING_QTY"},"right":{"ref":"S2D_WEIGHTED_AVG"}}',
 'decimal', 0),


-- ═══ S2e — Sổ chi tiết tiền (8 formulas, per section) ═══

('S2E_CASH_OPENING', 'Tiền mặt đầu kỳ — S2e',
 'Lấy từ AccountingPeriods.OpeningCashBalance',
 'EXTERNAL_LOOKUP',
 '{"lookup":{"entity":"AccountingPeriods","field":"OpeningCashBalance"}}',
 'decimal', 0),

('S2E_CASH_IN', 'Tổng thu tiền mặt — S2e',
 'SUM GL DebitAmount WHERE MoneyChannel = cash',
 'AGGREGATE',
 '{"aggregate":"SUM","source":"gl_entries","field":"DebitAmount","scope":"location","filter":{"MoneyChannel":"cash"}}',
 'decimal', 0),

('S2E_CASH_OUT', 'Tổng chi tiền mặt — S2e',
 'SUM GL CreditAmount WHERE MoneyChannel = cash',
 'AGGREGATE',
 '{"aggregate":"SUM","source":"gl_entries","field":"CreditAmount","scope":"location","filter":{"MoneyChannel":"cash"}}',
 'decimal', 0),

('S2E_CASH_CLOSING', 'Tiền mặt tồn cuối kỳ — S2e',
 'Opening + In - Out',
 'CELL_REF',
 '{"op":"SUBTRACT","left":{"op":"ADD","left":{"ref":"S2E_CASH_OPENING"},"right":{"ref":"S2E_CASH_IN"}},"right":{"ref":"S2E_CASH_OUT"}}',
 'decimal', 0),

('S2E_BANK_OPENING', 'Tiền gửi đầu kỳ — S2e',
 'Lấy từ AccountingPeriods.OpeningBankBalance',
 'EXTERNAL_LOOKUP',
 '{"lookup":{"entity":"AccountingPeriods","field":"OpeningBankBalance"}}',
 'decimal', 0),

('S2E_BANK_IN', 'Tổng gửi vào — S2e',
 'SUM GL DebitAmount WHERE MoneyChannel = bank',
 'AGGREGATE',
 '{"aggregate":"SUM","source":"gl_entries","field":"DebitAmount","scope":"location","filter":{"MoneyChannel":"bank"}}',
 'decimal', 0),

('S2E_BANK_OUT', 'Tổng rút ra — S2e',
 'SUM GL CreditAmount WHERE MoneyChannel = bank',
 'AGGREGATE',
 '{"aggregate":"SUM","source":"gl_entries","field":"CreditAmount","scope":"location","filter":{"MoneyChannel":"bank"}}',
 'decimal', 0),

('S2E_BANK_CLOSING', 'Dư tiền gửi cuối kỳ — S2e',
 'Opening + In - Out',
 'CELL_REF',
 '{"op":"SUBTRACT","left":{"op":"ADD","left":{"ref":"S2E_BANK_OPENING"},"right":{"ref":"S2E_BANK_IN"}},"right":{"ref":"S2E_BANK_OUT"}}',
 'decimal', 0);


-- ═══ Link FormulaIds to TemplateFieldMappings ═══
-- Update S2a formula mappings (TemplateVersionId = 2)
UPDATE TemplateFieldMappings SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2A_QUARTERLY_TOTAL')
WHERE TemplateVersionId = 2 AND FieldCode = 'cong_quy';
UPDATE TemplateFieldMappings SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2A_VAT')
WHERE TemplateVersionId = 2 AND FieldCode = 'thue_gtgt';
UPDATE TemplateFieldMappings SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2A_PIT')
WHERE TemplateVersionId = 2 AND FieldCode = 'thue_tncn';

-- Update S2b formula mappings (TemplateVersionId = 3)
UPDATE TemplateFieldMappings SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2B_QUARTERLY_TOTAL')
WHERE TemplateVersionId = 3 AND FieldCode = 'cong_quy';
UPDATE TemplateFieldMappings SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2B_VAT')
WHERE TemplateVersionId = 3 AND FieldCode = 'thue_gtgt';

-- Update S2c formula mappings (TemplateVersionId = 4)
UPDATE TemplateFieldMappings SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2C_TOTAL_REVENUE')
WHERE TemplateVersionId = 4 AND FieldCode = 'tong_dt';
UPDATE TemplateFieldMappings SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2C_TOTAL_COST')
WHERE TemplateVersionId = 4 AND FieldCode = 'tong_cp';
UPDATE TemplateFieldMappings SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2C_PROFIT')
WHERE TemplateVersionId = 4 AND FieldCode = 'chenh_lech';
UPDATE TemplateFieldMappings SET FormulaId = (SELECT FormulaId FROM FormulaDefinitions WHERE Code = 'S2C_PIT')
WHERE TemplateVersionId = 4 AND FieldCode = 'thue_tncn';

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('062_seed_formula_definitions', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
