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
    business_location_id CHAR(36) NOT NULL PRIMARY KEY,
    name VARCHAR(255) NOT NULL COMMENT 'Location/store name',
    address TEXT NOT NULL,
    district VARCHAR(100) DEFAULT NULL,
    city VARCHAR(100) DEFAULT NULL,
    phone VARCHAR(20) DEFAULT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    tax_code VARCHAR(50) DEFAULT NULL COMMENT 'Tax identification number',
    INDEX idx_business_location_name (name),
    INDEX idx_business_location_is_active (is_active),
    INDEX idx_business_location_city (city)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- USER LOCATION ASSIGNMENT TABLE (User assignment to locations)
-- Assign users/employees to business locations
-- Medium data volume => UUID
-- =============================================
CREATE TABLE IF NOT EXISTS UserLocationAssignment (
    user_location_assignment_id CHAR(36) NOT NULL PRIMARY KEY,
    user_id CHAR(36) NOT NULL COMMENT 'Assigned user',
    business_location_id CHAR(36) NOT NULL COMMENT 'Assigned location',
    is_owner BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'Is the owner of this location',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT fk_user_location_assignment_location FOREIGN KEY (business_location_id) 
        REFERENCES BusinessLocation(business_location_id) ON DELETE CASCADE ON UPDATE CASCADE,
    INDEX idx_user_location_assignment_user (user_id),
    INDEX idx_user_location_assignment_location (business_location_id),
    UNIQUE INDEX idx_user_location_unique (user_id, business_location_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- PRODUCT TABLE (Products)
-- Products belong to warehouse (business location)
-- High data volume => BIGINT AUTO_INCREMENT
-- =============================================
CREATE TABLE IF NOT EXISTS Product (
    product_id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    business_location_id CHAR(36) NOT NULL COMMENT 'Warehouse/location of product',
    BusinessTypeId CHAR(36) NOT NULL COMMENT 'Business type category',
    product_name VARCHAR(255) NOT NULL,
    cost_price DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Cost price',
    stock INT NOT NULL DEFAULT 0 COMMENT 'Stock quantity',
    unit VARCHAR(50) NOT NULL DEFAULT 'Unit' COMMENT 'Unit of measurement',
    image_url VARCHAR(500) DEFAULT NULL,
    manufacturer VARCHAR(255) DEFAULT NULL COMMENT 'Manufacturer name',
    CONSTRAINT fk_product_business_location FOREIGN KEY (business_location_id) 
        REFERENCES BusinessLocation(business_location_id) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_product_business_type FOREIGN KEY (BusinessTypeId) 
        REFERENCES BusinessType(BusinessTypeId) ON DELETE RESTRICT ON UPDATE CASCADE,
    INDEX idx_product_business_location (business_location_id),
    INDEX idx_product_business_type (BusinessTypeId),
    INDEX idx_product_name (product_name),
    INDEX idx_product_manufacturer (manufacturer)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- SALE ITEM TABLE (Sale items)
-- High data volume => BIGINT AUTO_INCREMENT
-- =============================================
CREATE TABLE IF NOT EXISTS SaleItem (
    sale_item_id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    product_id BIGINT NOT NULL,
    unit VARCHAR(50) NOT NULL DEFAULT 'Unit' COMMENT 'Unit of measurement',
    quantity INT NOT NULL DEFAULT 1 COMMENT 'Quantity',
    CONSTRAINT fk_sale_item_product FOREIGN KEY (product_id) 
        REFERENCES Product(product_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    INDEX idx_sale_item_product (product_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- PRODUCT PRICE POLICY TABLE (Product pricing policies)
-- Allows different prices by time period/campaign
-- High data volume => BIGINT AUTO_INCREMENT
-- =============================================
CREATE TABLE IF NOT EXISTS ProductPricePolicy (
    product_price_policy_id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    sale_item_id BIGINT NOT NULL,
    price DECIMAL(15,2) NOT NULL COMMENT 'Applied price',
    is_default BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'Is default price',
    start_at DATETIME DEFAULT NULL,
    end_at DATETIME DEFAULT NULL,
    CONSTRAINT fk_product_price_policy_sale_item FOREIGN KEY (sale_item_id) 
        REFERENCES SaleItem(sale_item_id) ON DELETE CASCADE ON UPDATE CASCADE,
    INDEX idx_price_policy_sale_item (sale_item_id),
    INDEX idx_price_policy_is_default (is_default),
    INDEX idx_price_policy_date_range (start_at, end_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- IMPORT TABLE (Import receipts)
-- Imports belong to warehouse (business location)
-- High data volume => BIGINT AUTO_INCREMENT
-- =============================================
CREATE TABLE IF NOT EXISTS Import (
    import_id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    schema_json JSON DEFAULT NULL COMMENT 'Import data schema',
    total_amount DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Total amount',
    date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Import date',
    import_image VARCHAR(500) DEFAULT NULL COMMENT 'Import receipt image',
    Description TEXT,
    INDEX idx_import_date (date)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- PRODUCT_IMPORT TABLE (Import receipt details)
-- High data volume => Composite Primary Key
-- =============================================
CREATE TABLE IF NOT EXISTS Product_Import (
    import_id BIGINT NOT NULL,
    product_id BIGINT NOT NULL,
    quantity INT NOT NULL DEFAULT 0 COMMENT 'Import quantity',
    total_price DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Total price',
    PRIMARY KEY (import_id, product_id),
    CONSTRAINT fk_product_import_import FOREIGN KEY (import_id) 
        REFERENCES Import(import_id) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_product_import_product FOREIGN KEY (product_id) 
        REFERENCES Product(product_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    INDEX idx_product_import_product (product_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Insert this migration
INSERT INTO __MigrationHistory (MigrationId, ProductVersion) 
VALUES ('005_add_location_flow_table', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
