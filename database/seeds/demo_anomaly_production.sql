-- =============================================================================
-- DEMO: Dữ liệu kinh doanh tiệm tạp hóa — Test Anomaly Detection (Production)
-- =============================================================================
-- Kịch bản : Tiệm tạp hóa Minh Phúc, TP.HCM
-- Bảng     : Revenues, Costs
-- Mục tiêu : Tạo baseline 90 ngày + các bản ghi bất thường để demo tính năng
--
-- Hướng dẫn:
--   1. Thay @LOCATION_ID  → BusinessLocationId của địa điểm muốn demo
--   2. Thay @CREATED_BY   → UUID của user hợp lệ trong hệ thống
--   3. Chạy toàn bộ script
--   4. Xem kết quả SELECT cuối — ghi lại các record_id để gọi API
--   5. Gọi API theo bảng "API CALLS" ở cuối file
--   6. Dọn dữ liệu bằng phần CLEANUP khi demo xong
-- =============================================================================

-- ===== CẤU HÌNH (BẮT BUỘC ĐỔI TRƯỚC KHI CHẠY) ==============================
SET @LOCATION_ID = 18;                                        -- << ĐỔI: BusinessLocationId
SET @CREATED_BY  = '4b985308-c147-4c27-8708-0bed4a2a468c';   -- << ĐỔI: UUID user hợp lệ
-- =============================================================================

SET @TAG = '[DEMO-ANOMALY]';   -- prefix trong Description, dùng để CLEANUP sau

USE bizflow_db;
-- =============================================================================
-- PHẦN 1: BASELINE DOANH THU (12 tuần × ~1 lần/tuần)
-- =============================================================================

-- ─────────────────────────────────────────────────────────────────────────────
-- 1a. Doanh thu bán lẻ hàng tuần  (RevenueType = 'sale')
--
--  Giá trị     : 3.500.000 – 4.150.000đ (dao động nhẹ theo mùa)
--  avg         ≈ 3.837.500đ
--  std         ≈ 199.000đ  (population)
--
--  Ngưỡng anomaly khi check-record:
--    CRITICAL  : z > 5  →  amount > ~4.833.000đ
--    WARNING   : z > 3  →  amount > ~4.435.000đ
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO Revenues
  (BusinessLocationId, RevenueType, Amount, Status, IsReversal,
   RevenueDate, Description, CreatedBy, CreatedAt)
VALUES
  (@LOCATION_ID, 'sale', 3500000, 'posted', 0, DATE_SUB(CURDATE(), INTERVAL 85 DAY), CONCAT(@TAG, ' Doanh thu bán tạp hóa tuần 1'),   @CREATED_BY, NOW()),
  (@LOCATION_ID, 'sale', 3900000, 'posted', 0, DATE_SUB(CURDATE(), INTERVAL 78 DAY), CONCAT(@TAG, ' Doanh thu bán tạp hóa tuần 2'),   @CREATED_BY, NOW()),
  (@LOCATION_ID, 'sale', 3700000, 'posted', 0, DATE_SUB(CURDATE(), INTERVAL 71 DAY), CONCAT(@TAG, ' Doanh thu bán tạp hóa tuần 3'),   @CREATED_BY, NOW()),
  (@LOCATION_ID, 'sale', 4100000, 'posted', 0, DATE_SUB(CURDATE(), INTERVAL 64 DAY), CONCAT(@TAG, ' Doanh thu bán tạp hóa tuần 4'),   @CREATED_BY, NOW()),
  (@LOCATION_ID, 'sale', 3600000, 'posted', 0, DATE_SUB(CURDATE(), INTERVAL 57 DAY), CONCAT(@TAG, ' Doanh thu bán tạp hóa tuần 5'),   @CREATED_BY, NOW()),
  (@LOCATION_ID, 'sale', 4000000, 'posted', 0, DATE_SUB(CURDATE(), INTERVAL 50 DAY), CONCAT(@TAG, ' Doanh thu bán tạp hóa tuần 6'),   @CREATED_BY, NOW()),
  (@LOCATION_ID, 'sale', 3800000, 'posted', 0, DATE_SUB(CURDATE(), INTERVAL 43 DAY), CONCAT(@TAG, ' Doanh thu bán tạp hóa tuần 7'),   @CREATED_BY, NOW()),
  (@LOCATION_ID, 'sale', 3750000, 'posted', 0, DATE_SUB(CURDATE(), INTERVAL 36 DAY), CONCAT(@TAG, ' Doanh thu bán tạp hóa tuần 8'),   @CREATED_BY, NOW()),
  (@LOCATION_ID, 'sale', 4050000, 'posted', 0, DATE_SUB(CURDATE(), INTERVAL 29 DAY), CONCAT(@TAG, ' Doanh thu bán tạp hóa tuần 9'),   @CREATED_BY, NOW()),
  (@LOCATION_ID, 'sale', 3650000, 'posted', 0, DATE_SUB(CURDATE(), INTERVAL 22 DAY), CONCAT(@TAG, ' Doanh thu bán tạp hóa tuần 10'),  @CREATED_BY, NOW()),
  (@LOCATION_ID, 'sale', 4150000, 'posted', 0, DATE_SUB(CURDATE(), INTERVAL 15 DAY), CONCAT(@TAG, ' Doanh thu bán tạp hóa tuần 11'),  @CREATED_BY, NOW()),
  (@LOCATION_ID, 'sale', 3850000, 'posted', 0, DATE_SUB(CURDATE(), INTERVAL 8  DAY), CONCAT(@TAG, ' Doanh thu bán tạp hóa tuần 12'),  @CREATED_BY, NOW());

-- ─────────────────────────────────────────────────────────────────────────────
-- 1b. Thu nhập khác hàng tháng  (RevenueType = 'manual')
--     Ví dụ: tiền thanh lý vỏ thùng, chai lọ — dao động nhẹ ~1,8–2,1M/tháng
--
--  avg         ≈ 1.989.500đ
--  std         ≈ 114.500đ  (population)
--
--  Ngưỡng anomaly khi check-record:
--    CRITICAL  : z > 5  →  amount > ~2.562.000đ
--    WARNING   : z > 3  →  amount > ~2.333.000đ
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO Revenues
  (BusinessLocationId, RevenueType, Amount, Status, IsReversal,
   RevenueDate, Description, CreatedBy, CreatedAt)
VALUES
  (@LOCATION_ID, 'manual', 2005000, 'posted', 0, DATE_SUB(CURDATE(), INTERVAL 80 DAY), CONCAT(@TAG, ' Thanh lý vỏ thùng, chai lọ tháng 2'), @CREATED_BY, NOW()),
  (@LOCATION_ID, 'manual', 1800000, 'posted', 0, DATE_SUB(CURDATE(), INTERVAL 55 DAY), CONCAT(@TAG, ' Thanh lý vỏ thùng, chai lọ tháng 3'), @CREATED_BY, NOW()),
  (@LOCATION_ID, 'manual', 2100000, 'posted', 0, DATE_SUB(CURDATE(), INTERVAL 30 DAY), CONCAT(@TAG, ' Thanh lý vỏ thùng, chai lọ tháng 4'), @CREATED_BY, NOW()),
  (@LOCATION_ID, 'manual', 2053000, 'posted', 0, DATE_SUB(CURDATE(), INTERVAL 10 DAY), CONCAT(@TAG, ' Thanh lý vỏ thùng, chai lọ đầu tháng 5'), @CREATED_BY, NOW());


-- =============================================================================
-- PHẦN 2: BASELINE CHI PHÍ
-- =============================================================================

-- ─────────────────────────────────────────────────────────────────────────────
-- 2a. Tiền thuê mặt bằng cố định hàng tháng  (CostType = 'rent')
--     9.000.000đ/tháng — không đổi → std = 0, dùng ratio check
--
--    CRITICAL  : ratio > 5  →  amount > 45.000.000đ
--    WARNING   : ratio > 2  →  amount > 18.000.000đ
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO Costs
  (BusinessLocationId, CostType, Amount, Status, IsReversal,
   CostDate, Description, CreatedBy, CreatedAt)
VALUES
  (@LOCATION_ID, 'rent', 9000000, 'posted', 0, DATE_SUB(CURDATE(), INTERVAL 75 DAY), CONCAT(@TAG, ' Tiền thuê mặt bằng tháng 2/2025'), @CREATED_BY, NOW()),
  (@LOCATION_ID, 'rent', 9000000, 'posted', 0, DATE_SUB(CURDATE(), INTERVAL 45 DAY), CONCAT(@TAG, ' Tiền thuê mặt bằng tháng 3/2025'), @CREATED_BY, NOW()),
  (@LOCATION_ID, 'rent', 9000000, 'posted', 0, DATE_SUB(CURDATE(), INTERVAL 15 DAY), CONCAT(@TAG, ' Tiền thuê mặt bằng tháng 4/2025'), @CREATED_BY, NOW());

-- ─────────────────────────────────────────────────────────────────────────────
-- 2b. Lương nhân viên hàng tháng  (CostType = 'salary')
--
--  Giá trị     : 6.500.000 – 7.200.000đ (có tháng tăng ca / thưởng nhỏ)
--  avg         ≈ 6.840.000đ
--  std         ≈ 241.700đ  (population)
--
--  Ngưỡng anomaly khi check-record:
--    CRITICAL  : z > 5  →  amount > ~8.049.000đ
--    WARNING   : z > 3  →  amount > ~7.566.000đ
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO Costs
  (BusinessLocationId, CostType, Amount, Status, IsReversal,
   CostDate, Description, CreatedBy, CreatedAt)
VALUES
  (@LOCATION_ID, 'salary', 6500000, 'posted', 0, DATE_SUB(CURDATE(), INTERVAL 80 DAY), CONCAT(@TAG, ' Lương nhân viên tháng 2/2025'),                    @CREATED_BY, NOW()),
  (@LOCATION_ID, 'salary', 7000000, 'posted', 0, DATE_SUB(CURDATE(), INTERVAL 55 DAY), CONCAT(@TAG, ' Lương nhân viên tháng 3/2025 (có tăng ca cuối tháng)'), @CREATED_BY, NOW()),
  (@LOCATION_ID, 'salary', 6800000, 'posted', 0, DATE_SUB(CURDATE(), INTERVAL 35 DAY), CONCAT(@TAG, ' Lương nhân viên tháng 4/2025'),                    @CREATED_BY, NOW()),
  (@LOCATION_ID, 'salary', 7200000, 'posted', 0, DATE_SUB(CURDATE(), INTERVAL 20 DAY), CONCAT(@TAG, ' Lương nhân viên tháng 4/2025 (thưởng lễ 30/4)'),    @CREATED_BY, NOW()),
  (@LOCATION_ID, 'salary', 6700000, 'posted', 0, DATE_SUB(CURDATE(), INTERVAL 10 DAY), CONCAT(@TAG, ' Lương tạm ứng đầu tháng 5/2025'),                   @CREATED_BY, NOW());


-- =============================================================================
-- DEMO LIVE — tạo bản ghi sai ngay trong buổi demo
-- =============================================================================
-- Chạy từng block bên dưới theo kịch bản, ghi lại ID để gọi API.

-- ── Act 1: Nhân viên nhập doanh thu thừa một số 0 ───────────────────────────
-- Bình thường: 3.850.000đ  →  Nhập sai: 38.500.000đ
-- z ≈ 174 (>> 5) → CRITICAL
/*
INSERT INTO Revenues
  (BusinessLocationId, RevenueType, Amount, Status, IsReversal,
   RevenueDate, Description, CreatedBy, CreatedAt)
VALUES (@LOCATION_ID, 'sale', 38500000, 'posted', 0, CURDATE(),
        'Doanh thu bán lẻ chiều nay', @CREATED_BY, NOW());
SET @LIVE_REV_ID = LAST_INSERT_ID();
SELECT @LIVE_REV_ID AS live_revenue_id;

-- Gọi API:
-- POST /anomaly/check-record
-- { "location_id": "<LOCATION_ID>", "record_type": "revenue", "record_id": "<live_revenue_id>" }
-- Kỳ vọng: CRITICAL — "cao bất thường so với mức trung bình 90 ngày (3.837.500đ ± 199.000đ)"
*/

-- ── Act 2: Nhân viên nhập tiền thuê nhầm thêm một số 0 ──────────────────────
-- Bình thường: 9.000.000đ  →  Nhập sai: 90.000.000đ
-- std=0, ratio = 10,0 (> 5) → CRITICAL
/*
INSERT INTO Costs
  (BusinessLocationId, CostType, Amount, Status, IsReversal,
   CostDate, Description, CreatedBy, CreatedAt)
VALUES (@LOCATION_ID, 'rent', 90000000, 'posted', 0, CURDATE(),
        'Tiền thuê mặt bằng tháng 5/2025', @CREATED_BY, NOW());
SET @LIVE_COST_ID = LAST_INSERT_ID();
SELECT @LIVE_COST_ID AS live_cost_id;

-- Gọi API:
-- POST /anomaly/check-record
-- { "location_id": "<LOCATION_ID>", "record_type": "cost", "record_id": "<live_cost_id>" }
-- Kỳ vọng: CRITICAL — "cao bất thường gấp 10,0 lần mức cố định (9.000.000đ)"
*/

-- ── Act 3: Quét tổng thể cuối ngày (Tier 2a) ────────────────────────────────
-- POST /anomaly  { "location_ids": ["<LOCATION_ID>"] }
-- AI quét toàn bộ revenues hôm nay → phát hiện 38,5M là spike


-- =============================================================================
-- XEM ALERTS ĐÃ SINH
-- =============================================================================
/*
SELECT alert_type, severity, tier, record_type, reference_id, description
FROM   ai_anomaly_alerts
WHERE  location_id = '<LOCATION_ID>'
ORDER  BY generated_at DESC;
*/


-- =============================================================================
-- CLEANUP — chạy sau khi demo xong
-- =============================================================================
/*
-- Xóa baseline đã seed
DELETE FROM Revenues WHERE BusinessLocationId = @LOCATION_ID AND Description LIKE '%[DEMO-ANOMALY]%';
DELETE FROM Costs     WHERE BusinessLocationId = @LOCATION_ID AND Description LIKE '%[DEMO-ANOMALY]%';

-- Xóa bản ghi tạo live trong demo (nếu không có @LIVE_REV_ID / @LIVE_COST_ID thì thay bằng ID cụ thể)
DELETE FROM Revenues WHERE RevenueId = @LIVE_REV_ID;
DELETE FROM Costs     WHERE CostId   = @LIVE_COST_ID;

-- Xóa alerts đã sinh hôm nay
DELETE FROM ai_anomaly_alerts WHERE location_id = '<LOCATION_ID>' AND DATE(generated_at) = CURDATE();
*/
