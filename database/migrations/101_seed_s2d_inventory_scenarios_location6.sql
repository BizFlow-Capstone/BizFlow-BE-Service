-- =============================================
-- Migration 101: Seed S2d inventory scenarios for location 6
--
-- Purpose:
--   Seed inventory test data for S2d with 2 scenarios:
--   1) Weighted-average cost with changing import prices in Q2/2026
--   2) Opening-balance-only product with no Q2 movement
-- =============================================

SET @groceryId = COALESCE(
    @groceryId,
    (SELECT bt.BusinessTypeId FROM BusinessTypes bt WHERE bt.Code = 'GROCERY' LIMIT 1),
    '650e8400-e29b-41d4-a716-446655440003'
);

-- ─────────────────────────────────────────────────────────────
-- 4. S2d TEST DATA — Inventory book scenarios for location 6
--    Case A: One product with continuously changing import prices
--            to test weighted-average (gia binh quan gia quyen)
--    Case B: One product with opening balance only, no movement in Q2
--            to verify it still appears in S2d sections
-- ─────────────────────────────────────────────────────────────

-- 4.1 Ensure test products exist
INSERT INTO Products
    (BusinessLocationId, BusinessTypeId, ProductName, Sku, CostPrice, SellingPrice, Stock, Unit, Status, TrackInventory)
SELECT 6, @groceryId, 'S2D Test - Gao ST25', 'S2D-LOC6-WAVG', 15000.00, 19000.00, 170, 'kg', 'active', 1
WHERE NOT EXISTS (
    SELECT 1 FROM Products p
    WHERE p.BusinessLocationId = 6 AND p.Sku = 'S2D-LOC6-WAVG' AND p.DeletedAt IS NULL
);

INSERT INTO Products
    (BusinessLocationId, BusinessTypeId, ProductName, Sku, CostPrice, SellingPrice, Stock, Unit, Status, TrackInventory)
SELECT 6, @groceryId, 'S2D Test - Mi Goi Ton Dau Ky', 'S2D-LOC6-NOMOVE', 25000.00, 30000.00, 40, 'thung', 'active', 1
WHERE NOT EXISTS (
    SELECT 1 FROM Products p
    WHERE p.BusinessLocationId = 6 AND p.Sku = 'S2D-LOC6-NOMOVE' AND p.DeletedAt IS NULL
);

SET @productWavgId = (
    SELECT p.ProductId FROM Products p
    WHERE p.BusinessLocationId = 6 AND p.Sku = 'S2D-LOC6-WAVG' AND p.DeletedAt IS NULL
    ORDER BY p.ProductId DESC LIMIT 1
);

SET @productNoMoveId = (
    SELECT p.ProductId FROM Products p
    WHERE p.BusinessLocationId = 6 AND p.Sku = 'S2D-LOC6-NOMOVE' AND p.DeletedAt IS NULL
    ORDER BY p.ProductId DESC LIMIT 1
);

-- 4.2 CASE A (WAVG): opening import before Q2 + multiple Q2 imports with changing unit prices
-- Opening stock before period: 100 @ 10,000
INSERT IGNORE INTO Imports
    (ImportCode, ImportType, Status, BusinessLocationId, Supplier, HasInvoice, TotalAmount, CreatedAt, ConfirmedAt, ReceivedAt, Note)
VALUES
    ('S2D-LOC6-WAVG-OPENING', 'INVOICE', 'CONFIRMED', 6, 'NCC S2D TEST', 1, 1000000.00,
     '2026-03-28 08:00:00', '2026-03-28 08:10:00', '2026-03-28 08:00:00', 'S2d test opening import before Q2');
SET @importWavgOpeningId = (
    SELECT i.ImportId FROM Imports i
    WHERE i.BusinessLocationId = 6 AND i.ImportCode = 'S2D-LOC6-WAVG-OPENING'
    ORDER BY i.ImportId DESC LIMIT 1
);

INSERT IGNORE INTO ProductsImports
    (ImportId, ProductId, Quantity, CostPrice, TotalPrice, BaseUnit, CreatedAt)
VALUES
    (@importWavgOpeningId, @productWavgId, 100, 10000.00, 1000000.00, 'kg', '2026-03-28 08:00:00');

INSERT INTO StockMovements
    (ProductId, MovementType, Quantity, ReferenceType, ReferenceId, Memo, BalanceAfter, CreatedAt)
VALUES
    (@productWavgId, 'IN', 100, 'IMPORT', @importWavgOpeningId, 'S2d WAVG opening import', 100, '2026-03-28 08:10:00');

-- Q2 import #1: 80 @ 12,000
INSERT IGNORE INTO Imports
    (ImportCode, ImportType, Status, BusinessLocationId, Supplier, HasInvoice, TotalAmount, CreatedAt, ConfirmedAt, ReceivedAt, Note)
VALUES
    ('S2D-LOC6-WAVG-IMP-20260405', 'INVOICE', 'CONFIRMED', 6, 'NCC S2D TEST', 1, 960000.00,
     '2026-04-05 09:00:00', '2026-04-05 09:15:00', '2026-04-05 09:00:00', 'S2d test import price changed to 12,000');
SET @importWavgQ2AId = (
    SELECT i.ImportId FROM Imports i
    WHERE i.BusinessLocationId = 6 AND i.ImportCode = 'S2D-LOC6-WAVG-IMP-20260405'
    ORDER BY i.ImportId DESC LIMIT 1
);

INSERT IGNORE INTO ProductsImports
    (ImportId, ProductId, Quantity, CostPrice, TotalPrice, BaseUnit, CreatedAt)
VALUES
    (@importWavgQ2AId, @productWavgId, 80, 12000.00, 960000.00, 'kg', '2026-04-05 09:00:00');

INSERT INTO StockMovements
    (ProductId, MovementType, Quantity, ReferenceType, ReferenceId, Memo, BalanceAfter, CreatedAt)
VALUES
    (@productWavgId, 'IN', 80, 'IMPORT', @importWavgQ2AId, 'S2d WAVG import 80 @ 12,000', 180, '2026-04-05 09:15:00');

-- Q2 export #1: -70
INSERT INTO StockMovements
    (ProductId, MovementType, Quantity, ReferenceType, ReferenceId, Memo, BalanceAfter, CreatedAt)
VALUES
    (@productWavgId, 'OUT', -70, 'ADJUSTMENT', NULL, 'S2d WAVG export #1', 110, '2026-04-20 10:00:00');

-- Q2 import #2: 60 @ 9,000
INSERT IGNORE INTO Imports
    (ImportCode, ImportType, Status, BusinessLocationId, Supplier, HasInvoice, TotalAmount, CreatedAt, ConfirmedAt, ReceivedAt, Note)
VALUES
    ('S2D-LOC6-WAVG-IMP-20260510', 'INVOICE', 'CONFIRMED', 6, 'NCC S2D TEST', 1, 540000.00,
     '2026-05-10 09:00:00', '2026-05-10 09:15:00', '2026-05-10 09:00:00', 'S2d test import price changed to 9,000');
SET @importWavgQ2BId = (
    SELECT i.ImportId FROM Imports i
    WHERE i.BusinessLocationId = 6 AND i.ImportCode = 'S2D-LOC6-WAVG-IMP-20260510'
    ORDER BY i.ImportId DESC LIMIT 1
);

INSERT IGNORE INTO ProductsImports
    (ImportId, ProductId, Quantity, CostPrice, TotalPrice, BaseUnit, CreatedAt)
VALUES
    (@importWavgQ2BId, @productWavgId, 60, 9000.00, 540000.00, 'kg', '2026-05-10 09:00:00');

INSERT INTO StockMovements
    (ProductId, MovementType, Quantity, ReferenceType, ReferenceId, Memo, BalanceAfter, CreatedAt)
VALUES
    (@productWavgId, 'IN', 60, 'IMPORT', @importWavgQ2BId, 'S2d WAVG import 60 @ 9,000', 170, '2026-05-10 09:15:00');

-- Q2 export #2: -50
INSERT INTO StockMovements
    (ProductId, MovementType, Quantity, ReferenceType, ReferenceId, Memo, BalanceAfter, CreatedAt)
VALUES
    (@productWavgId, 'OUT', -50, 'ADJUSTMENT', NULL, 'S2d WAVG export #2', 120, '2026-05-26 10:00:00');

-- Q2 import #3: 90 @ 15,000
INSERT IGNORE INTO Imports
    (ImportCode, ImportType, Status, BusinessLocationId, Supplier, HasInvoice, TotalAmount, CreatedAt, ConfirmedAt, ReceivedAt, Note)
VALUES
    ('S2D-LOC6-WAVG-IMP-20260615', 'INVOICE', 'CONFIRMED', 6, 'NCC S2D TEST', 1, 1350000.00,
     '2026-06-15 09:00:00', '2026-06-15 09:15:00', '2026-06-15 09:00:00', 'S2d test import price changed to 15,000');
SET @importWavgQ2CId = (
    SELECT i.ImportId FROM Imports i
    WHERE i.BusinessLocationId = 6 AND i.ImportCode = 'S2D-LOC6-WAVG-IMP-20260615'
    ORDER BY i.ImportId DESC LIMIT 1
);

INSERT IGNORE INTO ProductsImports
    (ImportId, ProductId, Quantity, CostPrice, TotalPrice, BaseUnit, CreatedAt)
VALUES
    (@importWavgQ2CId, @productWavgId, 90, 15000.00, 1350000.00, 'kg', '2026-06-15 09:00:00');

INSERT INTO StockMovements
    (ProductId, MovementType, Quantity, ReferenceType, ReferenceId, Memo, BalanceAfter, CreatedAt)
VALUES
    (@productWavgId, 'IN', 90, 'IMPORT', @importWavgQ2CId, 'S2d WAVG import 90 @ 15,000', 210, '2026-06-15 09:15:00');

-- Q2 export #3: -40
INSERT INTO StockMovements
    (ProductId, MovementType, Quantity, ReferenceType, ReferenceId, Memo, BalanceAfter, CreatedAt)
VALUES
    (@productWavgId, 'OUT', -40, 'ADJUSTMENT', NULL, 'S2d WAVG export #3', 170, '2026-06-27 10:00:00');

-- Keep product state aligned with seeded stock movements
UPDATE Products
SET Stock = 170, CostPrice = 15000.00
WHERE ProductId = @productWavgId;

-- 4.3 CASE B (NO-MOVE): opening import before Q2 only, no movement during Q2
INSERT IGNORE INTO Imports
    (ImportCode, ImportType, Status, BusinessLocationId, Supplier, HasInvoice, TotalAmount, CreatedAt, ConfirmedAt, ReceivedAt, Note)
VALUES
    ('S2D-LOC6-NOMOVE-OPENING', 'INVOICE', 'CONFIRMED', 6, 'NCC S2D TEST', 1, 1000000.00,
     '2026-03-25 07:50:00', '2026-03-25 08:00:00', '2026-03-25 07:50:00', 'S2d no-move product opening import before Q2');
SET @importNoMoveOpeningId = (
    SELECT i.ImportId FROM Imports i
    WHERE i.BusinessLocationId = 6 AND i.ImportCode = 'S2D-LOC6-NOMOVE-OPENING'
    ORDER BY i.ImportId DESC LIMIT 1
);

INSERT IGNORE INTO ProductsImports
    (ImportId, ProductId, Quantity, CostPrice, TotalPrice, BaseUnit, CreatedAt)
VALUES
    (@importNoMoveOpeningId, @productNoMoveId, 40, 25000.00, 1000000.00, 'thung', '2026-03-25 07:50:00');

INSERT INTO StockMovements
    (ProductId, MovementType, Quantity, ReferenceType, ReferenceId, Memo, BalanceAfter, CreatedAt)
VALUES
    (@productNoMoveId, 'IN', 40, 'IMPORT', @importNoMoveOpeningId, 'S2d no-move opening import', 40, '2026-03-25 08:00:00');

UPDATE Products
SET Stock = 40, CostPrice = 25000.00
WHERE ProductId = @productNoMoveId;

-- ═══════════════════════════════════════════════════════════
-- Migration history
-- ═══════════════════════════════════════════════════════════
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('101_seed_s2d_inventory_scenarios_location6', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);

