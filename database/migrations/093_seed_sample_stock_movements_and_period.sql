-- Migration: 093_seed_sample_stock_movements_and_period
-- Purpose: Add sample stock movements for location 1 (which has Products)
--          and create an accounting period for location 1 so S2d can be tested.

-- ═══════════════════════════════════════════════════════════
-- PART 1: Create AccountingPeriod for location 1 (Q1 2026)
-- ═══════════════════════════════════════════════════════════
INSERT IGNORE INTO AccountingPeriods
    (BusinessLocationId, PeriodType, Year, Quarter, StartDate, EndDate, Status, OpeningCashBalance, OpeningBankBalance, CreatedAt)
VALUES
    (1, 'quarterly', 2026, 1, '2026-01-01', '2026-03-31', 'open', 50000000.00, 200000000.00, NOW());

SET @periodId = LAST_INSERT_ID();

-- ═══════════════════════════════════════════════════════════
-- PART 2: Seed StockMovements for location 1 products
-- Products: 1 (iPhone 25M), 2 (Samsung 22M), 3 (MacBook 45M), 4 (AirPods 5.5M)
-- Use realistic IN/OUT patterns across Q1 2026
-- ═══════════════════════════════════════════════════════════

-- ── Before period (Dec 2025) — opening stock ──
-- These represent stock that existed before Q1 2026
INSERT INTO StockMovements (ProductId, MovementType, Quantity, ReferenceType, ReferenceId, Memo, BalanceAfter, CreatedAt) VALUES
-- iPhone: had 50 units
(1, 'IN', 50, 'IMPORT', 100, 'Nhap kho dau ky - iPhone 15 Pro Max', 50, '2025-12-15 08:00:00'),
-- Samsung: had 30 units
(2, 'IN', 30, 'IMPORT', 101, 'Nhap kho dau ky - Samsung Galaxy S24', 30, '2025-12-15 08:00:00'),
-- MacBook: had 20 units
(3, 'IN', 20, 'IMPORT', 102, 'Nhap kho dau ky - MacBook Pro M3', 20, '2025-12-15 08:00:00'),
-- AirPods: had 100 units
(4, 'IN', 100, 'IMPORT', 103, 'Nhap kho dau ky - AirPods Pro 2', 100, '2025-12-15 08:00:00');

-- ── January 2026 — imports + sales ──
INSERT INTO StockMovements (ProductId, MovementType, Quantity, ReferenceType, ReferenceId, Memo, BalanceAfter, CreatedAt) VALUES
-- iPhone: import 10, sell 5
(1, 'IN',   10, 'IMPORT', 200, 'Nhap them iPhone thang 1', 60, '2026-01-05 09:00:00'),
(1, 'OUT',  -3, 'ORDER',  300, 'Ban hang iPhone', 57, '2026-01-10 14:30:00'),
(1, 'OUT',  -2, 'ORDER',  301, 'Ban hang iPhone', 55, '2026-01-20 10:00:00'),
-- Samsung: import 5, sell 3
(2, 'IN',    5, 'IMPORT', 201, 'Nhap them Samsung thang 1', 35, '2026-01-08 09:00:00'),
(2, 'OUT',  -2, 'ORDER',  302, 'Ban hang Samsung', 33, '2026-01-15 11:00:00'),
(2, 'OUT',  -1, 'ORDER',  303, 'Ban hang Samsung', 32, '2026-01-25 16:00:00'),
-- MacBook: sell 2
(3, 'OUT',  -1, 'ORDER',  304, 'Ban hang MacBook', 19, '2026-01-12 10:00:00'),
(3, 'OUT',  -1, 'ORDER',  305, 'Ban hang MacBook ghi no', 18, '2026-01-22 14:00:00'),
-- AirPods: import 20, sell 15
(4, 'IN',   20, 'IMPORT', 202, 'Nhap them AirPods thang 1', 120, '2026-01-06 09:00:00'),
(4, 'OUT',  -8, 'ORDER',  306, 'Ban hang AirPods', 112, '2026-01-11 15:00:00'),
(4, 'OUT',  -7, 'ORDER',  307, 'Ban hang AirPods', 105, '2026-01-28 11:00:00');

-- ── February 2026 — imports + sales ──
INSERT INTO StockMovements (ProductId, MovementType, Quantity, ReferenceType, ReferenceId, Memo, BalanceAfter, CreatedAt) VALUES
-- iPhone: import 15, sell 8
(1, 'IN',   15, 'IMPORT', 203, 'Nhap them iPhone thang 2', 70, '2026-02-03 09:00:00'),
(1, 'OUT',  -5, 'ORDER',  310, 'Ban hang iPhone', 65, '2026-02-10 14:00:00'),
(1, 'OUT',  -3, 'ORDER',  311, 'Ban hang iPhone', 62, '2026-02-22 10:00:00'),
-- Samsung: import 10, sell 5
(2, 'IN',   10, 'IMPORT', 204, 'Nhap them Samsung thang 2', 42, '2026-02-05 09:00:00'),
(2, 'OUT',  -3, 'ORDER',  312, 'Ban hang Samsung', 39, '2026-02-14 11:00:00'),
(2, 'OUT',  -2, 'ORDER',  313, 'Ban hang Samsung', 37, '2026-02-25 16:00:00'),
-- MacBook: import 5, sell 3
(3, 'IN',    5, 'IMPORT', 205, 'Nhap them MacBook thang 2', 23, '2026-02-07 09:00:00'),
(3, 'OUT',  -2, 'ORDER',  314, 'Ban hang MacBook', 21, '2026-02-15 13:00:00'),
(3, 'OUT',  -1, 'ORDER',  315, 'Ban hang MacBook', 20, '2026-02-28 10:00:00'),
-- AirPods: import 30, sell 20
(4, 'IN',   30, 'IMPORT', 206, 'Nhap them AirPods thang 2', 135, '2026-02-04 09:00:00'),
(4, 'OUT', -10, 'ORDER',  316, 'Ban hang AirPods', 125, '2026-02-12 15:00:00'),
(4, 'OUT', -10, 'ORDER',  317, 'Ban hang AirPods', 115, '2026-02-26 11:00:00');

-- ── March 2026 — imports + sales + adjustments ──
INSERT INTO StockMovements (ProductId, MovementType, Quantity, ReferenceType, ReferenceId, Memo, BalanceAfter, CreatedAt) VALUES
-- iPhone: import 20, sell 10, adjustment -2
(1, 'IN',   20, 'IMPORT', 207, 'Nhap them iPhone thang 3', 82, '2026-03-02 09:00:00'),
(1, 'OUT',  -5, 'ORDER',  320, 'Ban hang iPhone', 77, '2026-03-08 14:00:00'),
(1, 'OUT',  -3, 'ORDER',  321, 'Ban hang iPhone', 74, '2026-03-15 10:00:00'),
(1, 'OUT',  -2, 'ORDER',  322, 'Ban hang iPhone', 72, '2026-03-22 16:00:00'),
(1, 'ADJUSTMENT', -2, 'ADJUSTMENT', NULL, 'Kiem kho thieu 2 iPhone', 70, '2026-03-30 17:00:00'),
-- Samsung: sell 4
(2, 'OUT',  -2, 'ORDER',  323, 'Ban hang Samsung', 35, '2026-03-10 11:00:00'),
(2, 'OUT',  -2, 'ORDER',  324, 'Ban hang Samsung', 33, '2026-03-20 16:00:00'),
-- MacBook: import 3, sell 2
(3, 'IN',    3, 'IMPORT', 208, 'Nhap them MacBook thang 3', 23, '2026-03-05 09:00:00'),
(3, 'OUT',  -1, 'ORDER',  325, 'Ban hang MacBook', 22, '2026-03-12 13:00:00'),
(3, 'OUT',  -1, 'ORDER',  326, 'Ban hang MacBook', 21, '2026-03-25 10:00:00'),
-- AirPods: import 25, sell 18
(4, 'IN',   25, 'IMPORT', 209, 'Nhap them AirPods thang 3', 140, '2026-03-03 09:00:00'),
(4, 'OUT', -10, 'ORDER',  327, 'Ban hang AirPods', 130, '2026-03-10 15:00:00'),
(4, 'OUT',  -8, 'ORDER',  328, 'Ban hang AirPods', 122, '2026-03-28 11:00:00');

-- ═══════════════════════════════════════════════════════════
-- PART 3: Seed some Revenues for location 1 Q1 2026
-- (needed for S2a/S2b if tested on location 1)
-- ═══════════════════════════════════════════════════════════
INSERT IGNORE INTO Revenues
    (BusinessLocationId, BusinessTypeId, RevenueDate, Amount, Description, RevenueType, MoneyChannel, CreatedBy, CreatedAt)
VALUES
    (1, '650e8400-e29b-41d4-a716-446655440001', '2026-01-10', 90000000, 'Doanh thu ban dien thoai thang 1', 'sale', 'cash', '00000000-0000-0000-0000-000000000001', NOW()),
    (1, '650e8400-e29b-41d4-a716-446655440001', '2026-01-20', 60000000, 'Doanh thu ban laptop thang 1', 'sale', 'bank', '00000000-0000-0000-0000-000000000001', NOW()),
    (1, '650e8400-e29b-41d4-a716-446655440001', '2026-02-14', 120000000, 'Doanh thu Valentine sale', 'sale', 'cash', '00000000-0000-0000-0000-000000000001', NOW()),
    (1, '650e8400-e29b-41d4-a716-446655440001', '2026-02-25', 75000000, 'Doanh thu ban dien thoai thang 2', 'sale', 'bank', '00000000-0000-0000-0000-000000000001', NOW()),
    (1, '650e8400-e29b-41d4-a716-446655440001', '2026-03-08', 150000000, 'Doanh thu 8/3 sale', 'sale', 'cash', '00000000-0000-0000-0000-000000000001', NOW()),
    (1, '650e8400-e29b-41d4-a716-446655440001', '2026-03-22', 85000000, 'Doanh thu ban phu kien thang 3', 'sale', 'bank', '00000000-0000-0000-0000-000000000001', NOW());

-- ═══════════════════════════════════════════════════════════
-- PART 4: Seed GL entries for location 1 Q1 2026
-- (needed for S2e if tested on location 1)
-- ═══════════════════════════════════════════════════════════
INSERT IGNORE INTO GeneralLedgerEntries
    (BusinessLocationId, TransactionType, ReferenceType, ReferenceId, EntryDate, Description, DebitAmount, CreditAmount, MoneyChannel, IsReversal, CreatedAt)
VALUES
    -- Cash transactions
    (1, 'sale', 'revenue', 1, '2026-01-10', 'Thu tien mat ban hang', 90000000, 0, 'cash', 0, NOW()),
    (1, 'sale', 'revenue', 3, '2026-02-14', 'Thu tien mat Valentine sale', 120000000, 0, 'cash', 0, NOW()),
    (1, 'sale', 'revenue', 5, '2026-03-08', 'Thu tien mat 8/3 sale', 150000000, 0, 'cash', 0, NOW()),
    (1, 'manual_expense', 'cost', 1, '2026-01-15', 'Chi tien mat mua vat tu', 0, 15000000, 'cash', 0, NOW()),
    (1, 'manual_expense', 'cost', 2, '2026-02-20', 'Chi tien mat thue mat bang', 0, 25000000, 'cash', 0, NOW()),
    (1, 'manual_expense', 'cost', 3, '2026-03-15', 'Chi tien mat luong nhan vien', 0, 30000000, 'cash', 0, NOW()),
    -- Bank transactions
    (1, 'sale', 'revenue', 2, '2026-01-20', 'Chuyen khoan ban laptop', 60000000, 0, 'bank', 0, NOW()),
    (1, 'sale', 'revenue', 4, '2026-02-25', 'Chuyen khoan ban hang', 75000000, 0, 'bank', 0, NOW()),
    (1, 'sale', 'revenue', 6, '2026-03-22', 'Chuyen khoan ban phu kien', 85000000, 0, 'bank', 0, NOW()),
    (1, 'import_cost', 'import', 200, '2026-01-05', 'Thanh toan nhap hang iPhone', 0, 250000000, 'bank', 0, NOW()),
    (1, 'import_cost', 'import', 203, '2026-02-03', 'Thanh toan nhap hang iPhone', 0, 375000000, 'bank', 0, NOW()),
    (1, 'import_cost', 'import', 207, '2026-03-02', 'Thanh toan nhap hang iPhone', 0, 500000000, 'bank', 0, NOW());

-- ═══════════════════════════════════════════════════════════
-- Migration history
-- ═══════════════════════════════════════════════════════════
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('093_seed_sample_stock_movements_and_period', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
