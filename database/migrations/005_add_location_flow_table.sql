-- Migration: 005_add_location_flow_table
-- Description: Add BusinessType, BusinessTypeTax, BusinessLocation, UserLocationAssignment, Product, ProductPricePolicy, Import, Product_Import, SaleItem tables
-- Date: 2026-01-31

-- =============================================
-- BUSINESS TYPE TABLE (Business category)
-- Low data volume => UUID
-- =============================================
CREATE TABLE IF NOT EXISTS BusinessType (
    BusinessTypeId CHAR(36) NOT NULL PRIMARY KEY,
    Code VARCHAR(50) NOT NULL,
    Name VARCHAR(255) NOT NULL,
    Description TEXT,
    Status VARCHAR(50) NOT NULL DEFAULT 'active',
    CreatedById CHAR(36) DEFAULT NULL,
    ModifiedById CHAR(36) DEFAULT NULL,
    CreatedDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    LastModifiedDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_business_type_code (Code),
    INDEX idx_business_type_name (Name),
    INDEX idx_business_type_status (Status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- BUSINESS TYPE TAX TABLE (Tax rates by business type)
-- Low data volume => UUID
-- =============================================
CREATE TABLE IF NOT EXISTS BusinessTypeTax (
    BusinessTypeTaxId CHAR(36) NOT NULL PRIMARY KEY,
    BusinessTypeId CHAR(36) NOT NULL,
    TaxType VARCHAR(50) NOT NULL COMMENT 'VAT, PIT',
    TaxRate DECIMAL(5,2) NOT NULL COMMENT 'Tax rate percentage',
    CalculateOnPrice BOOLEAN NOT NULL,
    EffectiveFrom DATE NOT NULL,
    EffectiveTo DATE DEFAULT NULL,
    CreatedById CHAR(36) DEFAULT NULL,
    CreatedDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_business_type_tax_business_type FOREIGN KEY (BusinessTypeId) 
        REFERENCES BusinessType(BusinessTypeId) ON DELETE CASCADE ON UPDATE CASCADE,
    INDEX idx_business_type_tax_type (TaxType),
    INDEX idx_business_type_tax_business_type (BusinessTypeId),
    INDEX idx_business_type_tax_effective (EffectiveFrom, EffectiveTo)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- BUSINESS LOCATION TABLE (Business location/store)
-- Each user can have multiple business locations
-- Each location has one warehouse
-- Medium data volume => UUID
-- =============================================
CREATE TABLE IF NOT EXISTS BusinessLocation (
    BusinessLocationId CHAR(36) NOT NULL PRIMARY KEY,
    Name VARCHAR(255) NOT NULL COMMENT 'Location/store name',
    Address TEXT NOT NULL,
    District VARCHAR(100) DEFAULT NULL,
    City VARCHAR(100) DEFAULT NULL,
    Phone VARCHAR(20) DEFAULT NULL,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    TaxCode VARCHAR(50) DEFAULT NULL COMMENT 'Tax identification number',
    INDEX idx_business_location_name (Name),
    INDEX idx_business_location_is_active (IsActive),
    INDEX idx_business_location_city (City)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- USER LOCATION ASSIGNMENT TABLE (User assignment to locations)
-- Assign users/employees to business locations
-- Medium data volume => UUID
-- =============================================
CREATE TABLE IF NOT EXISTS UserLocationAssignment (
    UserLocationAssignmentId CHAR(36) NOT NULL PRIMARY KEY,
    UserId CHAR(36) NOT NULL COMMENT 'Assigned user',
    BusinessLocationId CHAR(36) NOT NULL COMMENT 'Assigned location',
    IsOwner BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'Is the owner of this location',
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT fk_user_location_assignment_location FOREIGN KEY (BusinessLocationId) 
        REFERENCES BusinessLocation(BusinessLocationId) ON DELETE CASCADE ON UPDATE CASCADE,
    INDEX idx_user_location_assignment_user (UserId),
    INDEX idx_user_location_assignment_location (BusinessLocationId),
    UNIQUE INDEX idx_user_location_unique (UserId, BusinessLocationId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- PRODUCT TABLE (Products)
-- Products belong to warehouse (business location)
-- High data volume => BIGINT AUTO_INCREMENT
-- =============================================
CREATE TABLE IF NOT EXISTS Product (
    ProductId BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    BusinessLocationId CHAR(36) NOT NULL COMMENT 'Warehouse/location of product',
    BusinessTypeId CHAR(36) NOT NULL COMMENT 'Business type category',
    ProductName VARCHAR(255) NOT NULL,
    CostPrice DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Cost price',
    Stock INT NOT NULL DEFAULT 0 COMMENT 'Stock quantity',
    Unit VARCHAR(50) NOT NULL DEFAULT 'Unit' COMMENT 'Unit of measurement',
    ImageUrl VARCHAR(500) DEFAULT NULL,
    Manufacturer VARCHAR(255) DEFAULT NULL COMMENT 'Manufacturer name',
    CONSTRAINT fk_product_business_location FOREIGN KEY (BusinessLocationId) 
        REFERENCES BusinessLocation(BusinessLocationId) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_product_business_type FOREIGN KEY (BusinessTypeId) 
        REFERENCES BusinessType(BusinessTypeId) ON DELETE RESTRICT ON UPDATE CASCADE,
    INDEX idx_product_business_location (BusinessLocationId),
    INDEX idx_product_business_type (BusinessTypeId),
    INDEX idx_product_name (ProductName),
    INDEX idx_product_manufacturer (Manufacturer)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- SALE ITEM TABLE (Sale items)
-- High data volume => BIGINT AUTO_INCREMENT
-- =============================================
CREATE TABLE IF NOT EXISTS SaleItem (
    SaleItemId BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    ProductId BIGINT NOT NULL,
    Unit VARCHAR(50) NOT NULL DEFAULT 'Unit' COMMENT 'Unit of measurement',
    Quantity INT NOT NULL DEFAULT 1 COMMENT 'Quantity',
    CONSTRAINT fk_sale_item_product FOREIGN KEY (ProductId) 
        REFERENCES Product(ProductId) ON DELETE RESTRICT ON UPDATE CASCADE,
    INDEX idx_sale_item_product (ProductId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- PRODUCT PRICE POLICY TABLE (Product pricing policies)
-- Allows different prices by time period/campaign
-- High data volume => BIGINT AUTO_INCREMENT
-- =============================================
CREATE TABLE IF NOT EXISTS ProductPricePolicy (
    ProductPricePolicyId BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    SaleItemId BIGINT NOT NULL,
    Price DECIMAL(15,2) NOT NULL COMMENT 'Applied price',
    IsDefault BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'Is default price',
    StartAt DATETIME DEFAULT NULL,
    EndAt DATETIME DEFAULT NULL,
    CONSTRAINT fk_product_price_policy_sale_item FOREIGN KEY (SaleItemId) 
        REFERENCES SaleItem(SaleItemId) ON DELETE CASCADE ON UPDATE CASCADE,
    INDEX idx_price_policy_sale_item (SaleItemId),
    INDEX idx_price_policy_is_default (IsDefault),
    INDEX idx_price_policy_date_range (StartAt, EndAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- IMPORT TABLE (Import receipts)
-- Imports belong to warehouse (business location)
-- High data volume => BIGINT AUTO_INCREMENT
-- =============================================
CREATE TABLE IF NOT EXISTS Import (
    ImportId BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    SchemaJson JSON DEFAULT NULL COMMENT 'Import data schema',
    TotalAmount DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Total amount',
    Date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Import date',
    ImportImage VARCHAR(500) DEFAULT NULL COMMENT 'Import receipt image',
    Description TEXT,
    INDEX idx_import_date (Date)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- PRODUCT_IMPORT TABLE (Import receipt details)
-- High data volume => Composite Primary Key
-- =============================================
CREATE TABLE IF NOT EXISTS Product_Import (
    ImportId BIGINT NOT NULL,
    ProductId BIGINT NOT NULL,
    Quantity INT NOT NULL DEFAULT 0 COMMENT 'Import quantity',
    TotalPrice DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Total price',
    PRIMARY KEY (ImportId, ProductId),
    CONSTRAINT fk_product_import_import FOREIGN KEY (ImportId) 
        REFERENCES Import(ImportId) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_product_import_product FOREIGN KEY (ProductId) 
        REFERENCES Product(ProductId) ON DELETE RESTRICT ON UPDATE CASCADE,
    INDEX idx_product_import_product (ProductId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Insert this migration
INSERT INTO __MigrationHistory (MigrationId, ProductVersion) 
VALUES ('005_add_location_flow_table', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
