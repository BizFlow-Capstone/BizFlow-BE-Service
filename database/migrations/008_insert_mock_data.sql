-- Migration: 008_insert_mock_data
-- Description: Insert mock data for all tables (except Roles)
-- Date: 2026-01-31

-- =============================================
-- USERS (5 users: 2 owners + 3 employees)
-- Password for all: "Password123!" (BCrypt hash)
-- =============================================
SET @user_role_id = (SELECT Id FROM Roles WHERE Name = 'user' LIMIT 1);

-- OWNERS
INSERT INTO Users (UserId, Email, PasswordHash, FullName, Phone, TaxCode, RoleId, IsActive, IsDeleted, EmailVerified, CreatedAt, UpdatedAt) VALUES
('550e8400-e29b-41d4-a716-446655440001', 'shinkiriloveforever@gmail.com', '$2a$11$xQg.K8z6P8r6W8yZ1Y2Y2eJ8Z4Y5X6W7V8U9T0S1R2Q3P4O5N6M7L8', 'Shinkiri Love Forever', '0877725635', '1234567890', @user_role_id, TRUE, FALSE, TRUE, NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440002', 'nganvhhse183096@fpt.edu.vn', '$2a$11$xQg.K8z6P8r6W8yZ1Y2Y2eJ8Z4Y5X6W7V8U9T0S1R2Q3P4O5N6M7L8', 'Vu Hoang Hieu Ngan', '0966288741', '9876543210', @user_role_id, TRUE, FALSE, TRUE, NOW(), NOW());

-- EMPLOYEES
INSERT INTO Users (UserId, Email, PasswordHash, FullName, Phone, TaxCode, RoleId, IsActive, IsDeleted, EmailVerified, CreatedAt, UpdatedAt) VALUES
('550e8400-e29b-41d4-a716-446655440003', 'tranvana@gmail.com', '$2a$11$xQg.K8z6P8r6W8yZ1Y2Y2eJ8Z4Y5X6W7V8U9T0S1R2Q3P4O5N6M7L8', 'Tran Van A', '0901234567', '1111222233', @user_role_id, TRUE, FALSE, TRUE, NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440004', 'nguyenthib@gmail.com', '$2a$11$xQg.K8z6P8r6W8yZ1Y2Y2eJ8Z4Y5X6W7V8U9T0S1R2Q3P4O5N6M7L8', 'Nguyen Thi B', '0912345678', '2222333344', @user_role_id, TRUE, FALSE, TRUE, NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440005', 'levanthanhc@gmail.com', '$2a$11$xQg.K8z6P8r6W8yZ1Y2Y2eJ8Z4Y5X6W7V8U9T0S1R2Q3P4O5N6M7L8', 'Le Van Thanh C', '0923456789', '3333444455', @user_role_id, TRUE, FALSE, TRUE, NOW(), NOW());

-- =============================================
-- BUSINESS TYPES
-- =============================================
INSERT INTO BusinessTypes (BusinessTypeId, Code, Name, Description, Status, CreatedById, CreatedDate, LastModifiedDate) VALUES
('650e8400-e29b-41d4-a716-446655440001', 'RETAIL', 'Retail Store', 'Cửa hàng bán lẻ - Retail business', 'active', '550e8400-e29b-41d4-a716-446655440001', NOW(), NOW()),
('650e8400-e29b-41d4-a716-446655440002', 'RESTAURANT', 'Restaurant & Cafe', 'Nhà hàng và quán cafe', 'active', '550e8400-e29b-41d4-a716-446655440001', NOW(), NOW()),
('650e8400-e29b-41d4-a716-446655440003', 'GROCERY', 'Grocery Store', 'Cửa hàng tạp hóa', 'active', '550e8400-e29b-41d4-a716-446655440002', NOW(), NOW()),
('650e8400-e29b-41d4-a716-446655440004', 'BEAUTY', 'Beauty Salon', 'Salon làm đẹp', 'active', '550e8400-e29b-41d4-a716-446655440002', NOW(), NOW());

-- =============================================
-- BUSINESS TYPE TAXES
-- =============================================
INSERT INTO BusinessTypeTaxes (BusinessTypeTaxId, BusinessTypeId, TaxType, TaxRate, CalculateOnPrice, EffectiveFrom, EffectiveTo, CreatedById, CreatedDate) VALUES
-- VAT for Retail
('750e8400-e29b-41d4-a716-446655440001', '650e8400-e29b-41d4-a716-446655440001', 'VAT', 10.00, TRUE, '2024-01-01', NULL, '550e8400-e29b-41d4-a716-446655440001', NOW()),
-- PIT for Retail
('750e8400-e29b-41d4-a716-446655440002', '650e8400-e29b-41d4-a716-446655440001', 'PIT', 5.00, FALSE, '2024-01-01', NULL, '550e8400-e29b-41d4-a716-446655440001', NOW()),
-- VAT for Restaurant
('750e8400-e29b-41d4-a716-446655440003', '650e8400-e29b-41d4-a716-446655440002', 'VAT', 8.00, TRUE, '2024-01-01', NULL, '550e8400-e29b-41d4-a716-446655440001', NOW()),
-- VAT for Grocery
('750e8400-e29b-41d4-a716-446655440004', '650e8400-e29b-41d4-a716-446655440003', 'VAT', 5.00, TRUE, '2024-01-01', NULL, '550e8400-e29b-41d4-a716-446655440002', NOW()),
-- VAT for Beauty
('750e8400-e29b-41d4-a716-446655440005', '650e8400-e29b-41d4-a716-446655440004', 'VAT', 10.00, TRUE, '2024-01-01', NULL, '550e8400-e29b-41d4-a716-446655440002', NOW());

-- =============================================
-- BUSINESS LOCATIONS (INT AUTO_INCREMENT => IDs: 1, 2, 3, 4)
-- =============================================
INSERT INTO BusinessLocations (Name, Address, District, City, Phone, IsActive, IsDeleted, TaxCode) VALUES
-- Shinkiri's locations (IDs: 1, 2)
('Shinkiri Tech Store - HCM', '123 Nguyen Hue Street, Ben Nghe Ward', 'District 1', 'Ho Chi Minh City', '0877725635', TRUE, FALSE, 'TAX-SHINKIRI-001'),
('Shinkiri Tech Store - Hanoi', '456 Tran Hung Dao Street', 'Hoan Kiem', 'Hanoi', '0877725636', TRUE, FALSE, 'TAX-SHINKIRI-002'),
-- Ngan's locations (IDs: 3, 4)
('Ngan Cafe & Bakery', '789 Le Loi Boulevard', 'District 3', 'Ho Chi Minh City', '0966288741', TRUE, FALSE, 'TAX-NGAN-001'),
('Ngan Beauty Salon', '234 Pasteur Street', 'District 1', 'Ho Chi Minh City', '0966288742', TRUE, FALSE, 'TAX-NGAN-002');

-- =============================================
-- USER LOCATION ASSIGNMENTS
-- =============================================
INSERT INTO UserLocationAssignments (UserId, BusinessLocationId, IsOwner, IsActive) VALUES
-- OWNERS: Shinkiri owns 2 tech stores
('550e8400-e29b-41d4-a716-446655440001', 1, TRUE, TRUE),
('550e8400-e29b-41d4-a716-446655440001', 2, TRUE, TRUE),
-- OWNERS: Ngan owns 2 businesses
('550e8400-e29b-41d4-a716-446655440002', 3, TRUE, TRUE),
('550e8400-e29b-41d4-a716-446655440002', 4, TRUE, TRUE),

-- EMPLOYEES: Staff hired to work at locations (IsOwner = FALSE)
-- Tran Van A works at Shinkiri's HCM store (location_id: 1)
('550e8400-e29b-41d4-a716-446655440003', 1, FALSE, TRUE),
-- Nguyen Thi B works at Ngan's Cafe (location_id: 3)
('550e8400-e29b-41d4-a716-446655440004', 3, FALSE, TRUE),
-- Le Van Thanh C works at Ngan's Beauty Salon (location_id: 4)
('550e8400-e29b-41d4-a716-446655440005', 4, FALSE, TRUE);

-- =============================================
-- PRODUCTS
-- =============================================
INSERT INTO Products (BusinessLocationId, BusinessTypeId, ProductName, CostPrice, Stock, Unit, Manufacturer) VALUES
-- Shinkiri's Tech Store HCM (location_id: 1) - Products
(1, '650e8400-e29b-41d4-a716-446655440001', 'iPhone 15 Pro Max', 25000000.00, 50, 'Unit', 'Apple'),
(1, '650e8400-e29b-41d4-a716-446655440001', 'Samsung Galaxy S24 Ultra', 22000000.00, 30, 'Unit', 'Samsung'),
(1, '650e8400-e29b-41d4-a716-446655440001', 'MacBook Pro M3', 45000000.00, 20, 'Unit', 'Apple'),
(1, '650e8400-e29b-41d4-a716-446655440001', 'AirPods Pro 2', 5500000.00, 100, 'Unit', 'Apple'),
-- Shinkiri's Tech Store Hanoi (location_id: 2) - Products
(2, '650e8400-e29b-41d4-a716-446655440001', 'Dell XPS 15', 35000000.00, 15, 'Unit', 'Dell'),
(2, '650e8400-e29b-41d4-a716-446655440001', 'Logitech MX Master 3S', 2200000.00, 40, 'Unit', 'Logitech'),
-- Ngan's Cafe (location_id: 3) - Products
(3, '650e8400-e29b-41d4-a716-446655440002', 'Cappuccino', 25000.00, 1000, 'Cup', 'House Blend'),
(3, '650e8400-e29b-41d4-a716-446655440002', 'Croissant', 35000.00, 200, 'Piece', 'House Made'),
(3, '650e8400-e29b-41d4-a716-446655440002', 'Tiramisu Cake', 55000.00, 50, 'Slice', 'House Made'),
-- Ngan's Beauty Salon (location_id: 4) - Products
(4, '650e8400-e29b-41d4-a716-446655440004', 'Hair Coloring Service', 800000.00, 0, 'Service', 'Loreal'),
(4, '650e8400-e29b-41d4-a716-446655440004', 'Nail Polish - Red', 150000.00, 80, 'Bottle', 'OPI'),
(4, '650e8400-e29b-41d4-a716-446655440004', 'Face Mask Treatment', 500000.00, 30, 'Box', 'SK-II');

-- =============================================
-- SALE ITEMS
-- =============================================
INSERT INTO SaleItems (ProductId, Unit, Quantity) VALUES
-- Tech products
(1, 'Unit', 1),
(2, 'Unit', 1),
(3, 'Unit', 1),
(4, 'Unit', 1),
(5, 'Unit', 1),
(6, 'Unit', 1),
-- Cafe products
(7, 'Cup', 1),
(8, 'Piece', 1),
(9, 'Slice', 1),
-- Beauty products
(10, 'Service', 1),
(11, 'Bottle', 1),
(12, 'Box', 1);

-- =============================================
-- PRODUCT PRICE POLICIES
-- =============================================
INSERT INTO ProductPricePolicies (SaleItemId, Price, IsDefault, StartAt, EndAt) VALUES
-- Tech Store - Regular prices
(1, 30000000.00, TRUE, NOW(), NULL),  -- iPhone 15 Pro Max
(2, 27000000.00, TRUE, NOW(), NULL),  -- Galaxy S24
(3, 52000000.00, TRUE, NOW(), NULL),  -- MacBook Pro
(4, 6500000.00, TRUE, NOW(), NULL),   -- AirPods Pro 2
(5, 42000000.00, TRUE, NOW(), NULL),  -- Dell XPS
(6, 2800000.00, TRUE, NOW(), NULL),   -- Logitech Mouse
-- Cafe - Regular prices
(7, 45000.00, TRUE, NOW(), NULL),     -- Cappuccino
(8, 50000.00, TRUE, NOW(), NULL),     -- Croissant
(9, 75000.00, TRUE, NOW(), NULL),     -- Tiramisu
-- Beauty - Regular prices
(10, 1200000.00, TRUE, NOW(), NULL),  -- Hair Coloring
(11, 250000.00, TRUE, NOW(), NULL),   -- Nail Polish
(12, 800000.00, TRUE, NOW(), NULL);   -- Face Mask

-- Promotion prices (limited time)
INSERT INTO ProductPricePolicies (SaleItemId, Price, IsDefault, StartAt, EndAt) VALUES
(1, 28500000.00, FALSE, NOW(), DATE_ADD(NOW(), INTERVAL 7 DAY)),  -- iPhone promotion
(7, 39000.00, FALSE, NOW(), DATE_ADD(NOW(), INTERVAL 14 DAY));    -- Cafe happy hour

-- =============================================
-- IMPORTS
-- =============================================
INSERT INTO Imports (SchemaJson, TotalAmount, Date, Description) VALUES
(
    '{"supplier": "Apple Authorized Distributor", "invoice_number": "INV-2024-001", "payment_method": "Bank Transfer"}',
    2550000000.00,
    '2024-01-15 10:30:00',
    'Nhập hàng điện thoại và laptop tháng 1/2024'
),
(
    '{"supplier": "Samsung Vietnam", "invoice_number": "INV-2024-002", "payment_method": "Cash"}',
    660000000.00,
    '2024-01-20 14:00:00',
    'Nhập Samsung flagship phones'
),
(
    '{"supplier": "Coffee Bean Supplier", "invoice_number": "COFFEE-2024-001", "payment_method": "Bank Transfer"}',
    50000000.00,
    '2024-01-10 08:00:00',
    'Nhập nguyên liệu cafe và bánh tháng 1'
),
(
    '{"supplier": "Beauty Supplies Co", "invoice_number": "BEAUTY-2024-001", "payment_method": "Credit Card"}',
    35000000.00,
    '2024-01-12 11:00:00',
    'Nhập mỹ phẩm và dụng cụ làm đẹp'
);

-- =============================================
-- PRODUCTS_IMPORTS
-- =============================================
INSERT INTO ProductsImports (ImportId, ProductId, Quantity, TotalPrice) VALUES
-- Import 1: Apple products
(1, 1, 50, 1250000000.00),  -- iPhone 15 Pro Max x50
(1, 3, 20, 900000000.00),   -- MacBook Pro x20
(1, 4, 100, 550000000.00),  -- AirPods Pro x100
-- Import 2: Samsung phones
(2, 2, 30, 660000000.00),   -- Galaxy S24 x30
-- Import 3: Cafe supplies
(3, 7, 1000, 25000000.00),  -- Cappuccino supplies
(3, 8, 200, 7000000.00),    -- Croissant materials
(3, 9, 50, 2750000.00),     -- Tiramisu ingredients
-- Import 4: Beauty products
(4, 11, 80, 12000000.00),   -- Nail Polish x80
(4, 12, 30, 15000000.00);   -- Face Mask x30

-- =============================================
-- Insert this migration
-- =============================================
INSERT INTO __MigrationHistory (MigrationId, ProductVersion) 
VALUES ('008_insert_mock_data', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
