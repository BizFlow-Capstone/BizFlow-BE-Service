-- Migration: 005_add_location_flow_table
-- Description: Add BusinessTypes, BusinessTypeTaxes, BusinessLocations, UserLocationAssignments, Products, ProductPricePolicies, Imports, ProductsImports, SaleItems tables
-- Date: 2026-01-31

-- =============================================
-- BUSINESS TYPES TABLE (Business category)
-- Low data volume => UUID
-- =============================================
CREATE TABLE IF NOT EXISTS BusinessTypes (
    BusinessTypeId CHAR(36) NOT NULL PRIMARY KEY,
    Code VARCHAR(50) NOT NULL,
    Name VARCHAR(255) NOT NULL,
    Description TEXT DEFAULT NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'active' COMMENT 'active, inactive',
    CreatedById CHAR(36) DEFAULT NULL,
    ModifiedById CHAR(36) DEFAULT NULL,
    CreatedDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    LastModifiedDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE INDEX idx_business_type_code (Code),
    INDEX idx_business_type_name (Name),
    INDEX idx_business_type_status (Status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- BUSINESS TYPE TAXES TABLE (Tax rates by business type)
-- Low data volume => UUID
-- =============================================
CREATE TABLE IF NOT EXISTS BusinessTypeTaxes (
    BusinessTypeTaxId CHAR(36) NOT NULL PRIMARY KEY,
    BusinessTypeId CHAR(36) NOT NULL,
    TaxType VARCHAR(50) NOT NULL COMMENT 'VAT, PIT',
    TaxRate DECIMAL(5,2) NOT NULL DEFAULT 0 COMMENT 'Tax rate percentage',
    CalculateOnPrice BOOLEAN NOT NULL DEFAULT TRUE COMMENT 'TRUE = calculate on price, FALSE = calculate on revenue',
    EffectiveFrom DATE NOT NULL,
    EffectiveTo DATE DEFAULT NULL,
    CreatedById CHAR(36) DEFAULT NULL,
    CreatedDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_business_type_tax_business_type FOREIGN KEY (BusinessTypeId) 
        REFERENCES BusinessTypes(BusinessTypeId) ON DELETE CASCADE ON UPDATE CASCADE,
    INDEX idx_business_type_tax_type (TaxType),
    INDEX idx_business_type_tax_business_type (BusinessTypeId),
    INDEX idx_business_type_tax_effective (EffectiveFrom, EffectiveTo)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- BUSINESS LOCATIONS TABLE (Business location/store)
-- Each user can have multiple business locations
-- Each location has one warehouse
-- Medium data volume => INT AUTO_INCREMENT
-- =============================================
CREATE TABLE IF NOT EXISTS BusinessLocations (
    BusinessLocationId INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(255) NOT NULL COMMENT 'Location/store name',
    Address TEXT NOT NULL,
    District VARCHAR(100) DEFAULT NULL,
    City VARCHAR(100) DEFAULT NULL,
    Phone VARCHAR(20) DEFAULT NULL,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    IsDeleted BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'Soft delete flag',
    TaxCode VARCHAR(50) DEFAULT NULL COMMENT 'Business tax code',
    INDEX idx_business_location_name (Name),
    INDEX idx_business_location_city (City),
    INDEX idx_business_location_is_active (IsActive),
    INDEX idx_business_location_is_deleted (IsDeleted)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- USER LOCATION ASSIGNMENTS TABLE (User assignment to locations)
-- Assign users/employees to business locations
-- Medium data volume => INT AUTO_INCREMENT
-- =============================================
CREATE TABLE IF NOT EXISTS UserLocationAssignments (
    UserLocationAssignmentId INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    UserId CHAR(36) NOT NULL COMMENT 'Assigned user',
    BusinessLocationId INT NOT NULL COMMENT 'Assigned location',
    IsOwner BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'Is the owner of this location',
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT fk_user_location_assignment_location FOREIGN KEY (BusinessLocationId) 
        REFERENCES BusinessLocations(BusinessLocationId) ON DELETE CASCADE ON UPDATE CASCADE,
    INDEX idx_user_location_assignment_user (UserId),
    INDEX idx_user_location_assignment_location (BusinessLocationId),
    UNIQUE INDEX idx_user_location_unique (UserId, BusinessLocationId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- PRODUCTS TABLE (Products)
-- Products belong to warehouse (business location)
-- High data volume => BIGINT AUTO_INCREMENT
-- =============================================
CREATE TABLE IF NOT EXISTS Products (
    ProductId BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    BusinessLocationId INT NOT NULL COMMENT 'Warehouse/location of product',
    BusinessTypeId CHAR(36) NOT NULL COMMENT 'Business type category',
    ProductName VARCHAR(255) NOT NULL,
    CostPrice DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Cost price',
    Stock INT NOT NULL DEFAULT 0 COMMENT 'Quantity in stock',
    Unit VARCHAR(50) NOT NULL DEFAULT 'Unit' COMMENT 'Unit of measurement',
    ImageUrl VARCHAR(500) DEFAULT NULL,
    Manufacturer VARCHAR(255) DEFAULT NULL COMMENT 'Manufacturer name',
    CONSTRAINT fk_product_business_location FOREIGN KEY (BusinessLocationId) 
        REFERENCES BusinessLocations(BusinessLocationId) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_product_business_type FOREIGN KEY (BusinessTypeId) 
        REFERENCES BusinessTypes(BusinessTypeId) ON DELETE RESTRICT ON UPDATE CASCADE,
    INDEX idx_product_business_location (BusinessLocationId),
    INDEX idx_product_business_type (BusinessTypeId),
    INDEX idx_product_name (ProductName),
    INDEX idx_product_manufacturer (Manufacturer)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- SALE ITEMS TABLE (Sale items)
-- High data volume => BIGINT AUTO_INCREMENT
-- =============================================
CREATE TABLE IF NOT EXISTS SaleItems (
    SaleItemId BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    ProductId BIGINT NOT NULL,
    Unit VARCHAR(50) NOT NULL DEFAULT 'Unit' COMMENT 'Unit of measurement',
    Quantity INT NOT NULL DEFAULT 1,
    CONSTRAINT fk_sale_item_product FOREIGN KEY (ProductId) 
        REFERENCES Products(ProductId) ON DELETE RESTRICT ON UPDATE CASCADE,
    INDEX idx_sale_item_product (ProductId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- PRODUCT PRICE POLICIES TABLE (Product pricing policies)
-- Allows different prices by time period/campaign
-- High data volume => BIGINT AUTO_INCREMENT
-- =============================================
CREATE TABLE IF NOT EXISTS ProductPricePolicies (
    ProductPricePolicyId BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    SaleItemId BIGINT NOT NULL,
    Price DECIMAL(15,2) NOT NULL COMMENT 'Applied price',
    IsDefault BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'Is default price',
    StartAt DATETIME DEFAULT NULL,
    EndAt DATETIME DEFAULT NULL,
    CONSTRAINT fk_product_price_policy_sale_item FOREIGN KEY (SaleItemId) 
        REFERENCES SaleItems(SaleItemId) ON DELETE CASCADE ON UPDATE CASCADE,
    INDEX idx_price_policy_sale_item (SaleItemId),
    INDEX idx_price_policy_is_default (IsDefault),
    INDEX idx_price_policy_date_range (StartAt, EndAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- IMPORTS TABLE (Import receipts)
-- Imports belong to warehouse (business location)
-- High data volume => BIGINT AUTO_INCREMENT
-- =============================================
CREATE TABLE IF NOT EXISTS Imports (
    ImportId BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    SchemaJson JSON DEFAULT NULL COMMENT 'Import data schema',
    TotalAmount DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Total amount',
    Date DATETIME NOT NULL COMMENT 'Import date',
    Description TEXT DEFAULT NULL,
    INDEX idx_import_date (Date),
    INDEX idx_import_total_amount (TotalAmount)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- PRODUCTS_IMPORTS TABLE (Import receipt details)
-- High data volume => Composite Primary Key
-- =============================================
CREATE TABLE IF NOT EXISTS ProductsImports (
    ImportId BIGINT NOT NULL,
    ProductId BIGINT NOT NULL,
    Quantity INT NOT NULL DEFAULT 0 COMMENT 'Import quantity',
    TotalPrice DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Total price',
    PRIMARY KEY (ImportId, ProductId),
    CONSTRAINT fk_product_import_import FOREIGN KEY (ImportId) 
        REFERENCES Imports(ImportId) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_product_import_product FOREIGN KEY (ProductId) 
        REFERENCES Products(ProductId) ON DELETE RESTRICT ON UPDATE CASCADE,
    INDEX idx_product_import_product (ProductId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- Insert this migration
-- =============================================
INSERT INTO __MigrationHistory (MigrationId, ProductVersion) 
VALUES ('005_add_location_flow_table', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
