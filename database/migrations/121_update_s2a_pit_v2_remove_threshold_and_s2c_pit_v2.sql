-- =============================================
-- Migration 121: Update S2A_PIT_V2 to remove threshold
--
-- Behavior: tax = Σ(doanh_thu_i × PIT_rate_i) (không áp dụng ngưỡng 500 triệu)
-- =============================================
SET NAMES utf8mb4;

UPDATE FormulaDefinitions
SET ExpressionJson = '{"foreach":"industry","source":"revenues","field":"Amount","groupBy":"BusinessTypeId","apply":{"op":"MULTIPLY","left":{"context":"group_amount"},"right":{"lookup":{"entity":"IndustryTaxRates","field":"TaxRate","filter":{"TaxType":"PIT_METHOD_1"}}}},"reduce":"SUM"}',
    Description = 'Σ(doanh_thu_i × PIT_rate_i) — không áp dụng ngưỡng 500 triệu'
WHERE Code = 'S2A_PIT_V2';

UPDATE FormulaDefinitions
SET ExpressionJson = '{"foreach":"industry","source":"revenues","field":"Amount","groupBy":"BusinessTypeId","costSource":"costs","costField":"Amount","apply":{"op":"MULTIPLY","left":{"fn":"MAX","args":[{"literal":0},{"op":"SUBTRACT","left":{"context":"group_amount"},"right":{"context":"group_cost"}}]},"right":{"literal":0.15}},"reduce":"SUM"}',
    Description = 'Σ MAX(0, doanh_thu_i - chi_phi_i) × 15% — PIT co dinh 15% (S2c)'
WHERE Code = 'S2C_PIT_V2';

-- ═══════════════════════════════════════════════════════════
-- Migration history
-- ═══════════════════════════════════════════════════════════
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('121_update_s2a_pit_v2_remove_threshold_and_s2c_pit_v2', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
