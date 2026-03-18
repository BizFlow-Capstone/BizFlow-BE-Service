-- =============================================
-- Migration  : 037_create_costs
-- Description: Create Costs table - the source-of-truth
--              for all expense records of a business location.
--              Costs can be generated automatically from imports
--              (CostType = 'import', linked by ImportId) or
--              entered manually (other CostType values).
--              Each Cost record can be reflected by one or more
--              GeneralLedgerEntry records.
-- Date       : 2025-06-09
-- =============================================

-- =============================================
-- 1. COSTS: Source cost table
--    CostType 'import' -> linked to ImportId, auto-created when import is confirmed
--    Other CostType    -> manually created by users
-- =============================================
CREATE TABLE IF NOT EXISTS Costs (
    CostId             BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    BusinessLocationId INT NOT NULL COMMENT 'FK to BusinessLocations',
    CostType           VARCHAR(30) NOT NULL COMMENT 'import | salary | rent | utilities | transport | marketing | maintenance | other | manual',
    ImportId           BIGINT DEFAULT NULL COMMENT 'FK to Imports (only when CostType = import)',
    Description        VARCHAR(500) NOT NULL COMMENT 'Cost description',
    Amount             DECIMAL(15,2) NOT NULL COMMENT 'Cost amount',
    CostDate           DATE NOT NULL COMMENT 'Cost occurrence date',
    PaymentMethod      VARCHAR(20) DEFAULT NULL COMMENT 'cash | bank',
    DocumentUrl        VARCHAR(500) DEFAULT NULL COMMENT 'Document/receipt URL (Cloudinary)',
    DocumentPublicId   VARCHAR(255) DEFAULT NULL COMMENT 'Cloudinary public ID of the document',
    CreatedBy          CHAR(36) NOT NULL COMMENT 'UserId who created the record',
    CreatedAt          DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt          DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    DeletedAt          DATETIME DEFAULT NULL COMMENT 'Soft delete',

    CONSTRAINT fk_cost_location FOREIGN KEY (BusinessLocationId)
        REFERENCES BusinessLocations(BusinessLocationId) ON DELETE RESTRICT ON UPDATE CASCADE,

    CONSTRAINT fk_cost_import FOREIGN KEY (ImportId)
        REFERENCES Imports(ImportId) ON DELETE RESTRICT ON UPDATE CASCADE,

    INDEX idx_cost_location (BusinessLocationId),
    INDEX idx_cost_location_date (BusinessLocationId, CostDate),
    INDEX idx_cost_import (ImportId),
    INDEX idx_cost_type (BusinessLocationId, CostType)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
    COMMENT='Business cost source-of-truth table';

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('037_create_costs', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
