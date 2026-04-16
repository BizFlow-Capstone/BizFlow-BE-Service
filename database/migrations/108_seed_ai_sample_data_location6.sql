-- ================================================================
-- Migration : 108_seed_ai_sample_data_location6
-- Description: Dữ liệu mẫu cho 3 bảng AI của Business Location ID 6
--              để test các GET API Dashboard:
--                GET /api/my-business/ai/reorder
--                GET /api/my-business/ai/insights
--                GET /api/my-business/ai/anomalies
--
-- Notes:
--   - ai_revenue_forecasts đã có 8 rows từ nightly job (không cần thêm)
--   - location_id lưu dạng VARCHAR '6'
--   - ProductId 13 = Gạo ST25       (Stock=170, Status=active)
--   - ProductId 14 = Mì Tôm Chua Cay (Stock=400, Status=active)
--   - Idempotent: dùng UUIDs cố định + INSERT IGNORE (safe to re-run)
-- Date: 2026-04-07
-- ================================================================

-- ── Guard: chỉ seed nếu AI tables đã được tạo bởi alembic ─────────────────
DROP PROCEDURE IF EXISTS bizflow_migration_108;
DELIMITER $$
CREATE PROCEDURE bizflow_migration_108()
BEGIN
    -- Kiểm tra bảng ai_reorder_suggestions tồn tại chưa (alembic tạo trước)
    IF EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = DATABASE()
          AND table_name = 'ai_reorder_suggestions'
    ) THEN

-- ── 1. AI REORDER SUGGESTIONS ─────────────────────────────────────────────
-- Urgency logic: days_until_stockout <= 7 → HIGH, <= 14 → MEDIUM, > 14 → LOW
-- Product 14 (Mì Tôm): bán ~120 gói/ngày → hết hàng 400/120 ≈ 3 ngày → HIGH
-- Product 13 (Gạo ST25): bán ~5 kg/ngày  → hết hàng 170/5  = 34 ngày → LOW
INSERT IGNORE INTO ai_reorder_suggestions
    (id, location_id, product_id, current_stock, days_until_stockout,
     suggested_quantity, avg_daily_sales, urgency, generated_at)
VALUES
    ('a0b1c2d3-e4f5-6789-abcd-ef0123456701',
     '6', '14', 400, 3, 1680, 120.0, 'HIGH', NOW()),

    ('a0b1c2d3-e4f5-6789-abcd-ef0123456702',
     '6', '13', 170, 34, 35, 5.0, 'LOW', NOW());

-- ── 2. AI PRODUCT INSIGHTS ────────────────────────────────────────────────
-- TOP_SELLER  : metric_value = tổng doanh thu (VND) trong period
-- GROWTH_TREND: metric_value = tỉ lệ velocity_7d / velocity_30d (float >= 1.5)
-- PROMOTE_CANDIDATE: metric_value = biên lợi nhuận gộp (0.0 – 1.0)
INSERT IGNORE INTO ai_product_insights
    (id, location_id, product_id, insight_type, `rank`, metric_value, period_days, generated_at)
VALUES
    -- TOP_SELLER 7-day: Mì Tôm #1 (120 gói × 5.000đ × 7 ngày = 4.200.000đ)
    ('b1c2d3e4-f5a6-7890-bcde-f10123456701',
     '6', '14', 'TOP_SELLER', 1, 4200000, 7, NOW()),

    -- TOP_SELLER 7-day: Gạo ST25 #2 (5 kg × 25.000đ × 7 ngày = 875.000đ)
    ('b1c2d3e4-f5a6-7890-bcde-f10123456702',
     '6', '13', 'TOP_SELLER', 2, 875000, 7, NOW()),

    -- TOP_SELLER 30-day: Mì Tôm #1 (120 × 5.000 × 30 = 18.000.000đ)
    ('b1c2d3e4-f5a6-7890-bcde-f10123456703',
     '6', '14', 'TOP_SELLER', 1, 18000000, 30, NOW()),

    -- TOP_SELLER 30-day: Gạo ST25 #2 (5 × 25.000 × 30 = 3.750.000đ)
    ('b1c2d3e4-f5a6-7890-bcde-f10123456704',
     '6', '13', 'TOP_SELLER', 2, 3750000, 30, NOW()),

    -- GROWTH_TREND: Mì Tôm tăng trưởng (vel_7d/vel_30d = 2.1 ≥ ngưỡng 1.5)
    ('b1c2d3e4-f5a6-7890-bcde-f10123456705',
     '6', '14', 'GROWTH_TREND', 1, 2.1, 7, NOW()),

    -- PROMOTE_CANDIDATE: Mì Tôm biên lợi nhuận 30% ((5000-3500)/5000)
    ('b1c2d3e4-f5a6-7890-bcde-f10123456706',
     '6', '14', 'PROMOTE_CANDIDATE', 1, 0.30, 30, NOW()),

    -- PROMOTE_CANDIDATE: Gạo ST25 biên lợi nhuận 20% ((25000-20000)/25000)
    ('b1c2d3e4-f5a6-7890-bcde-f10123456707',
     '6', '13', 'PROMOTE_CANDIDATE', 2, 0.20, 30, NOW());

-- ── 3. AI ANOMALY ALERTS ──────────────────────────────────────────────────
INSERT IGNORE INTO ai_anomaly_alerts
    (id, location_id, alert_type, severity, tier,
     reference_date, description, reference_id, is_acknowledged, generated_at)
VALUES
    -- [RULE_BASED] Nhiều ngày liên tiếp không có giao dịch (chưa xác nhận)
    ('c2d3e4f5-a6b7-8901-cdef-012345678901',
     '6', 'DATA_QUALITY', 'WARNING', 'RULE_BASED',
     '2026-04-06',
     'Phát hiện 3 ngày liên tiếp (04/04 - 06/04/2026) không có giao dịch. Vui lòng kiểm tra lại dữ liệu nhập liệu và đảm bảo shop vẫn hoạt động bình thường.',
     NULL, 0, NOW()),

    -- [RULE_BASED] Doanh thu đột biến cao so với trung bình 7 ngày (chưa xác nhận)
    ('c2d3e4f5-a6b7-8901-cdef-012345678902',
     '6', 'REVENUE_ANOMALY', 'WARNING', 'RULE_BASED',
     '2026-04-05',
     'Doanh thu ngày 05/04/2026 đạt 28.500.000đ, cao hơn 2.4 lần so với trung bình 7 ngày (11.753.700đ). Đây có thể là ngày cao điểm hoặc có lỗi nhập liệu.',
     NULL, 0, NOW()),

    -- [LLM_PATTERN] Mô hình bất thường cuối tháng (đã xác nhận)
    ('c2d3e4f5-a6b7-8901-cdef-012345678903',
     '6', 'REVENUE_ANOMALY', 'CRITICAL', 'LLM_PATTERN',
     '2026-03-31',
     'AI phát hiện mô hình bất thường: doanh thu cuối tháng tăng mạnh nhưng đầu tháng kế tiếp giảm đột ngột. Xu hướng này lặp lại trong 2 tháng gần đây, có thể là dấu hiệu ghi nhận doanh thu chưa đều hoặc tập trung thanh toán vào cuối tháng.',
     NULL, 1, NOW());

-- ── Track migration ────────────────────────────────────────────────────────
INSERT INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('108_seed_ai_sample_data_location6', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;

    ELSE
        -- Không ghi __MigrationHistory → run-migrations.sh sẽ thử lại sau khi AI deploy
        SELECT 'SKIP migration 108: AI tables not yet created. Will retry after bizflow-ai is deployed.' AS Info;
    END IF;
END$$
DELIMITER ;
CALL bizflow_migration_108();
DROP PROCEDURE IF EXISTS bizflow_migration_108;
