-- Migration 110: Fix seed data for AI jobs
-- 1. Add orders on April 4 and April 7 → anomaly job needs 7 distinct days in last 7 days
-- 2. Add Mi Tom (SaleItemId=15, ProductId=14) to existing orders → reorder job needs 14+ distinct days per product
-- 3. UPDATE Products stock to trigger reorder suggestions (stock < reorder_point)

-- ============================================================
-- NEW ORDERS: April 4 and April 7 (to complete 7-day anomaly window)
-- ============================================================
INSERT IGNORE INTO Orders
    (OrderId, OrderCode, SubTotal, TotalAmount, CashAmount, BankAmount, Status, CreatedBy, CompletedAt, CreatedAt)
VALUES
(120, 'ORD-20260404-120', 60000.00, 60000.00, 60000.00, 0.00, 'completed', 'ff45309c-7b0b-4012-933b-042405d75685', '2026-04-04 16:30:00', '2026-04-04 09:00:00'),
(121, 'ORD-20260407-121', 57000.00, 57000.00, 57000.00, 0.00, 'completed', 'ff45309c-7b0b-4012-933b-042405d75685', '2026-04-07 11:00:00', '2026-04-07 09:00:00');

-- ============================================================
-- ORDER DETAILS for new orders
-- ============================================================
INSERT IGNORE INTO OrderDetails
    (OrderId, SaleItemId, Quantity, UnitPrice, Discount, Amount, CreatedAt)
VALUES
-- Order 120 (04-04): 2 Goi Mi Tom
(120, 15, 2, 30000.00, 0.00, 60000.00, '2026-04-04 09:00:00'),
-- Order 121 (04-07): 3 Goi Mi Tom
(121, 15, 3, 19000.00, 0.00, 57000.00, '2026-04-07 09:00:00');

-- ============================================================
-- ADD Mi Tom (SaleItemId=15, ProductId=14) to existing orders
-- to give ProductId=14 sufficient distinct sale dates (14+) for reorder job
-- Current P14 distinct dates: 03-10, 03-14, 03-17, 03-20, 03-25, 03-28, 04-01, 04-03, 04-05, 04-06 = 10 dates
-- Adding via orders that have ONLY Gao currently: 03-08(100), 03-09(101), 03-12(103), 03-21(109)
-- Result: 10 + 4 + 2 new (04-04, 04-07) = 16 distinct dates ✅
-- ============================================================
INSERT IGNORE INTO OrderDetails
    (OrderId, SaleItemId, Quantity, UnitPrice, Discount, Amount, CreatedAt)
VALUES
-- Order 100 (03-08): add 1 Goi Mi Tom → P14 date 03-08
(100, 15, 1, 30000.00, 0.00, 30000.00, '2026-03-08 10:00:00'),
-- Order 101 (03-09): add 1 Goi Mi Tom → P14 date 03-09
(101, 15, 1, 30000.00, 0.00, 30000.00, '2026-03-09 09:30:00'),
-- Order 103 (03-12): add 2 Goi Mi Tom → P14 date 03-12
(103, 15, 2, 30000.00, 0.00, 60000.00, '2026-03-12 08:00:00'),
-- Order 109 (03-21): add 1 Goi Mi Tom → P14 date 03-21
(109, 15, 1, 30000.00, 0.00, 30000.00, '2026-03-21 08:30:00');

-- ============================================================
-- REVENUES for new orders
-- ============================================================
INSERT IGNORE INTO Revenues
    (RevenueId, BusinessLocationId, OrderId, BusinessTypeId, RevenueType, Amount, RevenueDate, Description, MoneyChannel, CreatedBy, CreatedAt, DeletedAt)
VALUES
(220, 6, 120, '650e8400-e29b-41d4-a716-446655440003', 'sale', 60000.00, '2026-04-04', 'Doanh thu ban hang', 'cash', 'ff45309c-7b0b-4012-933b-042405d75685', '2026-04-04 16:30:00', NULL),
(221, 6, 121, '650e8400-e29b-41d4-a716-446655440003', 'sale', 57000.00, '2026-04-07', 'Doanh thu ban hang', 'cash', 'ff45309c-7b0b-4012-933b-042405d75685', '2026-04-07 11:00:00', NULL);

-- ============================================================
-- UPDATE Products stock to trigger reorder suggestions
-- Lower stock below reorder_point so ai_reorder_suggestions gets written
-- P13 (Gao ST25): avg_daily ~1.5 kg → reorder_point ~10.7 → set stock = 5
-- P14 (Mi Tom): avg_daily ~0.3/day → reorder_point ~1.5 → set stock = 1
-- ============================================================
UPDATE Products SET Stock = 5   WHERE ProductId = 13 AND BusinessLocationId = 6;
UPDATE Products SET Stock = 1   WHERE ProductId = 14 AND BusinessLocationId = 6;
