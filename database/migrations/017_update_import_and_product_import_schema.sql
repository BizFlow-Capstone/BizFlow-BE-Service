-- Migration: 017_update_import_and_product_import_schema
-- Description: Add new fields to Imports and ProductsImports tables to support standardized API
-- Date: 2026-02-19

-- =============================================
-- ALTER Imports TABLE
-- Add fields required by the standardized Import API
-- =============================================
ALTER TABLE Imports
    ADD COLUMN ImportCode VARCHAR(50) NULL COMMENT 'Auto-generated import code (e.g. PNK-2026-001)' AFTER ImportId,
    ADD COLUMN ImportType VARCHAR(50) NOT NULL DEFAULT 'INVOICE' COMMENT 'INVOICE, INVENTORY_ADJUSTMENT, RETURN' AFTER ImportCode,
    ADD COLUMN Status VARCHAR(20) NOT NULL DEFAULT 'DRAFT' COMMENT 'DRAFT, CONFIRMED, CANCELLED' AFTER ImportType,
    ADD COLUMN BusinessLocationId INT NOT NULL DEFAULT 1 COMMENT 'FK to BusinessLocations' AFTER Status,
    ADD COLUMN Supplier VARCHAR(200) NULL COMMENT 'Supplier name (free text)' AFTER BusinessLocationId,
    ADD COLUMN ImageUrl VARCHAR(500) NULL COMMENT 'URL of attached image/document' AFTER Description,
    ADD COLUMN ImagePublicId VARCHAR(100) NULL COMMENT 'Cloudinary public ID for image deletion' AFTER ImageUrl,
    ADD COLUMN CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Record creation timestamp' AFTER TotalAmount,
    ADD COLUMN UpdatedAt DATETIME NULL ON UPDATE CURRENT_TIMESTAMP COMMENT 'Last update timestamp' AFTER CreatedAt,
    MODIFY COLUMN Date DATETIME NULL COMMENT 'Date goods were received / import date';

-- Add FK constraint for BusinessLocationId
ALTER TABLE Imports
    ADD CONSTRAINT fk_import_business_location
        FOREIGN KEY (BusinessLocationId) REFERENCES BusinessLocations(BusinessLocationId)
        ON DELETE RESTRICT ON UPDATE CASCADE;

-- Add indexes
ALTER TABLE Imports
    ADD INDEX idx_import_status (Status),
    ADD INDEX idx_import_type (ImportType),
    ADD INDEX idx_import_business_location (BusinessLocationId),
    ADD INDEX idx_import_created_at (CreatedAt),
    ADD UNIQUE INDEX idx_import_code (ImportCode);

-- =============================================
-- ALTER ProductsImports TABLE
-- Add fields for unit conversion and cost tracking
-- =============================================
ALTER TABLE ProductsImports
    ADD COLUMN Unit VARCHAR(50) NOT NULL DEFAULT 'Unit' COMMENT 'Import unit (e.g. thùng, gói)' AFTER Quantity,
    ADD COLUMN CostPrice DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Cost price per import unit' AFTER Unit,
    ADD COLUMN BaseUnit VARCHAR(50) NOT NULL DEFAULT 'Unit' COMMENT 'Base/smallest inventory unit' AFTER TotalPrice;

-- Insert this migration
INSERT INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('017_update_import_and_product_import_schema', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
