-- =============================================
-- Migration  : 036_create_revenues
-- Description: Create Revenues table - the source-of-truth
--              for all income records of a business location.
--              Revenue can be generated from sales or
--              entered manually.
--              Each Revenue record can be reflected by one or more
--              GeneralLedgerEntry records.
-- Date       : 2025-06-09
-- =============================================

-- =============================================
-- 1. REVENUES: Source revenue table
--    RevenueType = 'sale'   -> sales revenue
--    RevenueType = 'manual' -> manually entered revenue
-- =============================================
CREATE TABLE IF NOT EXISTS Revenues (
    RevenueId          BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  BusinessLocationId INT NOT NULL COMMENT 'FK to BusinessLocations',
    RevenueType        VARCHAR(20) NOT NULL COMMENT 'sale | manual',
  Amount             DECIMAL(15,2) NOT NULL COMMENT 'Revenue amount',
  RevenueDate        DATE NOT NULL COMMENT 'Revenue recognition date',
  Description        VARCHAR(500) NOT NULL COMMENT 'Revenue description',
    MoneyChannel       VARCHAR(10) DEFAULT NULL COMMENT 'cash | bank | debt',
  CreatedBy          CHAR(36) NOT NULL COMMENT 'UserId who created the record',
    CreatedAt          DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    DeletedAt          DATETIME DEFAULT NULL COMMENT 'Soft delete',

    CONSTRAINT fk_revenue_location FOREIGN KEY (BusinessLocationId)
        REFERENCES BusinessLocations(BusinessLocationId) ON DELETE RESTRICT ON UPDATE CASCADE,

    INDEX idx_revenue_location (BusinessLocationId),
    INDEX idx_revenue_location_date (BusinessLocationId, RevenueDate),
    INDEX idx_revenue_type (RevenueType)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Business revenue source-of-truth table';

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('036_create_revenues', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
