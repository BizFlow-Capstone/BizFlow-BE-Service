-- =============================================
-- Migration 128: Restore S2A_PIT_V2 deduction logic
--
-- Behavior:
--   - Apply deduction 500,000,000 VND to the highest-revenue industry group
--   - Tax per group = MAX(0, group_revenue - group_deduction) × PIT_rate
--   - Total tax = SUM(all industry groups)
-- =============================================
SET NAMES utf8mb4;

UPDATE FormulaDefinitions
SET ExpressionJson = '{"foreach":"industry","source":"revenues","field":"Amount","groupBy":"BusinessTypeId","deduction":{"amount":500000000,"target":"highest_revenue"},"apply":{"op":"MULTIPLY","left":{"fn":"MAX","args":[{"literal":0},{"op":"SUBTRACT","left":{"context":"group_amount"},"right":{"context":"group_deduction"}}]},"right":{"lookup":{"entity":"IndustryTaxRates","field":"TaxRate","filter":{"TaxType":"PIT_METHOD_1"}}}},"reduce":"SUM"}',
    Description = 'MAX(0, doanh_thu_i - giam_tru_i) × PIT_rate_i — giảm trừ 500 triệu vào ngành cao nhất'
WHERE Code = 'S2A_PIT_V2';

-- ═══════════════════════════════════════════════════════════
-- Migration history
-- ═══════════════════════════════════════════════════════════
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('128_restore_s2a_pit_v2_deduction_highest_revenue', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
