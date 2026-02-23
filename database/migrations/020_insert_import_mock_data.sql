-- Migration: 020_insert_import_mock_data
-- Description: Insert mock data for ImportSchema, ImportSchemaVersion, Imports, ProductsImports
-- Date: 2026-02-23
-- NOTE: Run AFTER migrations 016, 017, 018, 019
-- Depends on: BusinessLocations (IDs 1-4), Products (IDs 1-12) from migration 008

-- Fix Vietnamese Unicode encoding for mysql CLI client
SET NAMES 'utf8mb4';
SET CHARACTER SET utf8mb4;

-- =============================================
-- IMPORT SCHEMA (1 schema: phieu nhap kho)
-- =============================================
INSERT IGNORE INTO ImportSchemas (TemplateCode, Name) VALUES
('PHIEU_NHAP_KHO', 'Phieu nhap kho');

-- =============================================
-- IMPORT SCHEMA VERSION (1 active version)
-- =============================================
SET @schema_id = (SELECT ImportSchemaId FROM ImportSchemas WHERE TemplateCode = 'PHIEU_NHAP_KHO' LIMIT 1);

INSERT IGNORE INTO ImportSchemaVersions (ImportSchemaId, SchemaJson, IsActive, CreatedAt) VALUES
(
    @schema_id,
    '{
        "type": "object",
        "required": ["items"],
        "properties": {
            "supplier":   { "type": "string", "maxLength": 200 },
            "note":       { "type": "string" },
            "receivedAt": { "type": "string", "format": "date-time" },
            "items": {
                "type": "array",
                "minItems": 1,
                "items": {
                    "type": "object",
                    "required": ["productId", "quantity", "costPrice"],
                    "properties": {
                        "productId":  { "type": "integer" },
                        "quantity":   { "type": "integer", "minimum": 1 },
                        "costPrice":  { "type": "number",  "minimum": 0 }
                    }
                }
            }
        }
    }',
    TRUE,
    NOW()
);

-- =============================================
-- IMPORTS (8 records, mix DRAFT/CONFIRMED/CANCELLED)
-- Locations: 1=Shinkiri HCM, 2=Shinkiri Hanoi, 3=Ngan Cafe, 4=Ngan Beauty
-- =============================================
INSERT IGNORE INTO Imports (ImportCode, ImportType, Status, BusinessLocationId, Supplier, Note, ReceivedAt, TotalAmount, CreatedAt, UpdatedAt) VALUES
-- 1. CONFIRMED
('PNK-2026-001', 'INVOICE', 'CONFIRMED', 1, 'Apple Authorized Distributor VN',
 'Nhap hang iPhone, MacBook va AirPods thang 1/2026',
 '2026-01-15 10:30:00', 2700000000.00,
 '2026-01-14 09:00:00', '2026-01-15 10:30:00'),

-- 2. CONFIRMED
('PNK-2026-002', 'INVOICE', 'CONFIRMED', 1, 'Samsung Vietnam Co. Ltd',
 'Nhap Samsung Galaxy S24 Ultra dot 1',
 '2026-01-22 14:00:00', 660000000.00,
 '2026-01-21 08:00:00', '2026-01-22 14:00:00'),

-- 3. CONFIRMED
('PNK-2026-003', 'INVOICE', 'CONFIRMED', 2, 'Tech Distributor HN',
 'Nhap laptop Dell va chuot Logitech cho kho Ha Noi',
 '2026-01-28 11:00:00', 613000000.00,
 '2026-01-27 15:00:00', '2026-01-28 11:00:00'),

-- 4. DRAFT
('PNK-2026-004', 'INVOICE', 'DRAFT', 1, 'Apple Authorized Distributor VN',
 'Du kien nhap them iPhone 15 Pro Max',
 NULL, 500000000.00,
 '2026-02-10 10:00:00', NULL),

-- 5. CONFIRMED
('PNK-2026-005', 'INVOICE', 'CONFIRMED', 3, 'Coffee Bean Supplier Saigon',
 'Nhap nguyen lieu cafe va banh thang 2/2026',
 '2026-02-05 08:00:00', 11850000.00,
 '2026-02-04 17:00:00', '2026-02-05 08:00:00'),

-- 6. CANCELLED
('PNK-2026-006', 'INVOICE', 'CANCELLED', 4, 'Beauty Supplies Import Co.',
 'Don nhap my pham bi huy do nha cung cap khong giao dung han',
 NULL, 17500000.00,
 '2026-02-08 11:00:00', '2026-02-09 09:00:00'),

-- 7. DRAFT
('PNK-2026-007', 'INVOICE', 'DRAFT', 1, NULL,
 'Du kien nhap bo sung MacBook Pro M3',
 NULL, 225000000.00,
 '2026-02-18 14:00:00', NULL),

-- 8. CONFIRMED
('PNK-2026-008', 'INVOICE', 'CONFIRMED', 4, 'Beauty Supplies Import Co.',
 'Nhap nail polish va face mask thang 2',
 '2026-02-15 10:00:00', 16500000.00,
 '2026-02-14 16:00:00', '2026-02-15 10:00:00');

-- =============================================
-- PRODUCTS_IMPORTS
-- ProductId: 1=iPhone, 2=Galaxy, 3=MacBook, 4=AirPods,
--            5=Dell XPS, 6=Logitech, 7=Cappuccino,
--            8=Croissant, 9=Tiramisu, 11=Nail Polish, 12=Face Mask
-- =============================================
SET @i1 = (SELECT ImportId FROM Imports WHERE ImportCode = 'PNK-2026-001' LIMIT 1);
SET @i2 = (SELECT ImportId FROM Imports WHERE ImportCode = 'PNK-2026-002' LIMIT 1);
SET @i3 = (SELECT ImportId FROM Imports WHERE ImportCode = 'PNK-2026-003' LIMIT 1);
SET @i4 = (SELECT ImportId FROM Imports WHERE ImportCode = 'PNK-2026-004' LIMIT 1);
SET @i5 = (SELECT ImportId FROM Imports WHERE ImportCode = 'PNK-2026-005' LIMIT 1);
SET @i6 = (SELECT ImportId FROM Imports WHERE ImportCode = 'PNK-2026-006' LIMIT 1);
SET @i7 = (SELECT ImportId FROM Imports WHERE ImportCode = 'PNK-2026-007' LIMIT 1);
SET @i8 = (SELECT ImportId FROM Imports WHERE ImportCode = 'PNK-2026-008' LIMIT 1);

INSERT IGNORE INTO ProductsImports (ImportId, ProductId, Quantity, CostPrice, TotalPrice, BaseUnit) VALUES
-- PNK-2026-001: Apple — Shinkiri HCM (CONFIRMED)
(@i1, 1,  50, 25000000.00, 1250000000.00, 'Unit'),  -- iPhone 15 Pro Max x50
(@i1, 3,  20, 45000000.00,  900000000.00, 'Unit'),  -- MacBook Pro M3 x20
(@i1, 4, 100,  5500000.00,  550000000.00, 'Unit'),  -- AirPods Pro 2 x100

-- PNK-2026-002: Samsung — Shinkiri HCM (CONFIRMED)
(@i2, 2,  30, 22000000.00, 660000000.00, 'Unit'),   -- Galaxy S24 Ultra x30

-- PNK-2026-003: Dell + Logitech — Shinkiri Hanoi (CONFIRMED)
(@i3, 5,  15, 35000000.00, 525000000.00, 'Unit'),   -- Dell XPS 15 x15
(@i3, 6,  40,  2200000.00,  88000000.00, 'Unit'),   -- Logitech MX Master x40

-- PNK-2026-004: DRAFT — iPhone
(@i4, 1,  20, 25000000.00, 500000000.00, 'Unit'),   -- iPhone 15 Pro Max x20

-- PNK-2026-005: Cafe supplies (CONFIRMED)
(@i5, 7, 500,    15000.00,   7500000.00, 'Cup'),    -- Cappuccino x500
(@i5, 8, 100,    30000.00,   3000000.00, 'Piece'),  -- Croissant x100
(@i5, 9,  30,    45000.00,   1350000.00, 'Slice'),  -- Tiramisu x30

-- PNK-2026-006: CANCELLED — Beauty (khong anh huong stock)
(@i6, 11,  50,  150000.00,  7500000.00, 'Bottle'), -- Nail Polish x50
(@i6, 12,  20,  500000.00, 10000000.00, 'Box'),    -- Face Mask x20

-- PNK-2026-007: DRAFT — MacBook
(@i7, 3,   5, 45000000.00, 225000000.00, 'Unit'),  -- MacBook Pro M3 x5

-- PNK-2026-008: Beauty nhap (CONFIRMED)
(@i8, 11,  60,  150000.00,  9000000.00, 'Bottle'), -- Nail Polish x60
(@i8, 12,  15,  500000.00,  7500000.00, 'Box');    -- Face Mask x15

-- =============================================
-- Insert this migration
-- =============================================
INSERT INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('020_insert_import_mock_data', '3.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '3.0.0';
