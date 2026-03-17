-- Migration: 040_insert_financial_mock_data
-- Description: Insert mock data for Debtors, DebtorPaymentTransactions, Orders, Revenues, Costs, GeneralLedgerEntries, StockMovements
-- Date: 2026-03-17
-- Depends on: Users, BusinessLocations, Products, SaleItems from 008 & 020.

SET NAMES 'utf8mb4';
SET CHARACTER SET utf8mb4;

-- Users
SET @shinkiri = '550e8400-e29b-41d4-a716-446655440001';
SET @ngan     = '550e8400-e29b-41d4-a716-446655440002';

-- =============================================
-- DEBTORS
-- =============================================
INSERT IGNORE INTO Debtors (BusinessLocationId, Name, Phone, Address, CreditLimit, CurrentBalance, Notes, CreatedByUserId, IsActive, CreatedAt, UpdatedAt) VALUES
-- Shinkiri Tech Store HCM (Loc 1)
(1, 'Anh Tuan IT', '0911222333', 'Quan 1, HCM', 50000000.00, 15000000.00, 'VIP Customer', @shinkiri, 1, DATE_SUB(NOW(), INTERVAL 5 DAY), NOW()),
-- Ngan Cafe (Loc 3)
(3, 'Chi Hoa VP', '0944555666', 'Quan 3, HCM', 2000000.00, 50000.00, 'Thuong xuyen mua cafe no', @ngan, 1, DATE_SUB(NOW(), INTERVAL 10 DAY), NOW());

SET @debtor1 = (SELECT DebtorId FROM Debtors WHERE Phone = '0911222333' LIMIT 1);
SET @debtor2 = (SELECT DebtorId FROM Debtors WHERE Phone = '0944555666' LIMIT 1);

-- =============================================
-- DEBTOR PAYMENT TRANSACTIONS
-- =============================================
INSERT IGNORE INTO DebtorPaymentTransactions (DebtorId, Amount, PaymentMethod, PaidAt, BalanceBefore, BalanceAfter, Notes, CreatedByUserId) VALUES
(@debtor1, 5000000.00, 'bank', DATE_SUB(NOW(), INTERVAL 2 DAY), 20000000.00, 15000000.00, 'Chuyen khoan truoc 1 phan', @shinkiri),
(@debtor2, 100000.00,  'cash', DATE_SUB(NOW(), INTERVAL 3 DAY), 150000.00, 50000.00, 'Tra bang tien mat', @ngan);

-- =============================================
-- ORDERS & ORDER DETAILS
-- =============================================
-- Order 1: Completed, Full Cash (Loc 1) - iPhone
INSERT IGNORE INTO Orders (OrderCode, CustomerName, CustomerPhone, TotalAmount, SubTotal, Discount, CashAmount, BankAmount, DebtAmount, DebtorId, Status, CreatedBy, CompletedBy, Note, CreatedAt, CompletedAt) VALUES
('ORD-20260310-001', 'Khach Vang Lai 1', '0901000001', 30000000.00, 30000000.00, 0.00, 30000000.00, 0.00, 0.00, NULL, 'completed', @shinkiri, @shinkiri, 'Ban tai quay', DATE_SUB(NOW(), INTERVAL 7 DAY), DATE_SUB(NOW(), INTERVAL 7 DAY));
SET @order1 = (SELECT OrderId FROM Orders WHERE OrderCode = 'ORD-20260310-001' LIMIT 1);

INSERT IGNORE INTO OrderDetails (OrderId, SaleItemId, Quantity, UnitPrice, Discount, Amount, CreatedAt) VALUES
(@order1, 1, 1, 30000000.00, 0.00, 30000000.00, DATE_SUB(NOW(), INTERVAL 7 DAY));

-- Order 2: Completed, Debt (Loc 1) - MacBook Pro for Debtor 1
INSERT IGNORE INTO Orders (OrderCode, CustomerName, CustomerPhone, TotalAmount, SubTotal, Discount, CashAmount, BankAmount, DebtAmount, DebtorId, Status, CreatedBy, CompletedBy, Note, CreatedAt, CompletedAt) VALUES
('ORD-20260312-002', 'Anh Tuan IT', '0911222333', 52000000.00, 52000000.00, 0.00, 32000000.00, 0.00, 20000000.00, @debtor1, 'completed', @shinkiri, @shinkiri, 'Ghi no mot phan', DATE_SUB(NOW(), INTERVAL 5 DAY), DATE_SUB(NOW(), INTERVAL 5 DAY));
SET @order2 = (SELECT OrderId FROM Orders WHERE OrderCode = 'ORD-20260312-002' LIMIT 1);

INSERT IGNORE INTO OrderDetails (OrderId, SaleItemId, Quantity, UnitPrice, Discount, Amount, CreatedAt) VALUES
(@order2, 3, 1, 52000000.00, 0.00, 52000000.00, DATE_SUB(NOW(), INTERVAL 5 DAY));

-- Order 3: Pending (Loc 3) - Cafe
INSERT IGNORE INTO Orders (OrderCode, CustomerName, CustomerPhone, TotalAmount, SubTotal, Discount, CashAmount, BankAmount, DebtAmount, DebtorId, Status, CreatedBy, Note, CreatedAt) VALUES
('ORD-20260317-003', 'Guest', NULL, 95000.00, 95000.00, 0.00, 0.00, 0.00, 0.00, NULL, 'pending', @ngan, 'Chua thanh toan', NOW());
SET @order3 = (SELECT OrderId FROM Orders WHERE OrderCode = 'ORD-20260317-003' LIMIT 1);

INSERT IGNORE INTO OrderDetails (OrderId, SaleItemId, Quantity, UnitPrice, Discount, Amount, CreatedAt) VALUES
(@order3, 7, 1, 45000.00, 0.00, 45000.00, NOW()), -- Cappuccino
(@order3, 8, 1, 50000.00, 0.00, 50000.00, NOW()); -- Croissant

-- =============================================
-- STOCK MOVEMENTS (For Completed Orders)
-- =============================================
INSERT IGNORE INTO StockMovements (ProductId, MovementType, Quantity, ReferenceType, ReferenceId, Memo, BalanceAfter, CreatedAt) VALUES
-- Order 1: iPhone (-1)
(1, 'OUT', -1, 'ORDER', @order1, 'Ban hang', 49, DATE_SUB(NOW(), INTERVAL 7 DAY)),
-- Order 2: MacBook (-1)
(3, 'OUT', -1, 'ORDER', @order2, 'Ban hang ghi no', 19, DATE_SUB(NOW(), INTERVAL 5 DAY));

-- UPDATE Product Stocks to reflect these sales (iPhone: 50 -> 49, MacBook: 20 -> 19)
UPDATE Products SET Stock = Stock - 1 WHERE ProductId IN (1, 3);

-- =============================================
-- COSTS
-- =============================================
INSERT IGNORE INTO Costs (BusinessLocationId, CostType, Amount, CostDate, Description, PaymentMethod, CreatedBy, CreatedAt) VALUES
(1, 'rent', 15000000.00, DATE_SUB(NOW(), INTERVAL 15 DAY), 'Tien thue mat bang thang 3', 'bank', @shinkiri, DATE_SUB(NOW(), INTERVAL 15 DAY)),
(3, 'utilities', 2000000.00, DATE_SUB(NOW(), INTERVAL 12 DAY), 'Tien dien nuoc thang 2', 'cash', @ngan, DATE_SUB(NOW(), INTERVAL 12 DAY));
-- Notice: LAST_INSERT_ID is not reliable with INSERT IGNORE if data already exists, but Costs don't have natural keys defined here.
-- Using explicit select by description (hacky but works for mock data)
SET @cost1 = (SELECT CostId FROM Costs WHERE Description = 'Tien thue mat bang thang 3' LIMIT 1);
SET @cost2 = (SELECT CostId FROM Costs WHERE Description = 'Tien dien nuoc thang 2' LIMIT 1);

-- =============================================
-- REVENUES (Manual/Non-Order)
-- =============================================
INSERT IGNORE INTO Revenues (BusinessLocationId, RevenueType, Amount, RevenueDate, Description, MoneyChannel, CreatedBy, CreatedAt) VALUES
(1, 'manual', 500000.00, DATE_SUB(NOW(), INTERVAL 6 DAY), 'Pha gia man hinh dich vu', 'cash', @shinkiri, DATE_SUB(NOW(), INTERVAL 6 DAY));
SET @rev1 = (SELECT RevenueId FROM Revenues WHERE Description = 'Pha gia man hinh dich vu' LIMIT 1);

-- =============================================
-- GENERAL LEDGER ENTRIES
-- =============================================
INSERT IGNORE INTO GeneralLedgerEntries (BusinessLocationId, EntryDate, TransactionType, Description, CreditAmount, DebitAmount, MoneyChannel, ReferenceType, ReferenceId, CreatedAt) VALUES
-- Order 1: Cash Sale
(1, DATE_SUB(NOW(), INTERVAL 7 DAY), 'sale', 'Thu tien ban hang: ORD-20260310-001', 30000000.00, 0.00, 'cash', 'order', @order1, DATE_SUB(NOW(), INTERVAL 7 DAY)),
-- Order 2: Mixed Sale (Cash + Debt)
(1, DATE_SUB(NOW(), INTERVAL 5 DAY), 'sale', 'Thu tien mat ban hang: ORD-20260312-002', 32000000.00, 0.00, 'cash', 'order', @order2, DATE_SUB(NOW(), INTERVAL 5 DAY)),
(1, DATE_SUB(NOW(), INTERVAL 5 DAY), 'sale', 'Ghi no ban hang: ORD-20260312-002', 20000000.00, 0.00, 'debt', 'order', @order2, DATE_SUB(NOW(), INTERVAL 5 DAY)),
-- Debtor Payments
(1, DATE_SUB(NOW(), INTERVAL 2 DAY), 'debt_payment', 'Thu no khach: Anh Tuan IT', 5000000.00, 0.00, 'bank', 'debtor_payment', @debtor1, DATE_SUB(NOW(), INTERVAL 2 DAY)),
(3, DATE_SUB(NOW(), INTERVAL 3 DAY), 'debt_payment', 'Thu no khach: Chi Hoa VP', 100000.00, 0.00, 'cash', 'debtor_payment', @debtor2, DATE_SUB(NOW(), INTERVAL 3 DAY)),
-- Costs
(1, DATE_SUB(NOW(), INTERVAL 15 DAY), 'manual_cost', 'Chi: Tien thue mat bang thang 3', 0.00, 15000000.00, 'bank', 'cost', @cost1, DATE_SUB(NOW(), INTERVAL 15 DAY)),
(3, DATE_SUB(NOW(), INTERVAL 12 DAY), 'manual_cost', 'Chi: Tien dien nuoc thang 2', 0.00, 2000000.00, 'cash', 'cost', @cost2, DATE_SUB(NOW(), INTERVAL 12 DAY)),
-- Manual Revenue
(1, DATE_SUB(NOW(), INTERVAL 6 DAY), 'manual_revenue', 'Thu: Pha gia man hinh dich vu', 500000.00, 0.00, 'cash', 'revenue', @rev1, DATE_SUB(NOW(), INTERVAL 6 DAY));

-- =============================================
-- Insert this migration
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion) 
VALUES ('040_insert_financial_mock_data', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
