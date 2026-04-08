-- =============================================
-- Migration 094: Fix S2A_PIT_V2 formula to use deduction instead of threshold
--
-- Problem: S2A_PIT_V2 used a binary threshold (if total > 500M, tax = Σ revenue_i × rate_i)
--          but never subtracted 500M from revenue.
--
-- Fix: Replace "threshold" with "deduction" node:
--   - Single industry: MAX(0, Revenue - 500M) × PIT_rate
--   - Multi-industry: subtract 500M from highest-revenue industry (optimize),
--     other industries: revenue × PIT_rate (no deduction)
--   - If deducted revenue goes to 0, that group contributes 0 tax (MAX with 0)
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
VALUES ('094_fix_s2a_pit_v2_deduction', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
