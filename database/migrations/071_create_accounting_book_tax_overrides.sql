-- =============================================
-- Migration 071: Create AccountingBookTaxOverrides
-- Purpose: Store VAT/PIT override percentages per book and business type
-- =============================================

CREATE TABLE IF NOT EXISTS AccountingBookTaxOverrides (
    OverrideId BIGINT AUTO_INCREMENT PRIMARY KEY,
    BookId BIGINT NOT NULL,
    BusinessTypeId CHAR(36) NOT NULL,

    -- Override rates (0..1, e.g. 0.01 = 1%)
    VatRate DECIMAL(8,6) NOT NULL,
    PitRate DECIMAL(8,6) NOT NULL,

    -- User explanation
    Note TEXT NOT NULL,

    -- Audit
    UpdatedByUserId CHAR(36) NOT NULL,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    -- FKs and indexes
    CONSTRAINT fk_abto_book FOREIGN KEY (BookId)
        REFERENCES AccountingBooks(BookId) ON DELETE CASCADE,
    CONSTRAINT fk_abto_business_type FOREIGN KEY (BusinessTypeId)
        REFERENCES BusinessTypes(BusinessTypeId),
    UNIQUE INDEX uq_abto_book_business_type (BookId, BusinessTypeId),
    INDEX idx_abto_book (BookId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('071_create_accounting_book_tax_overrides', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
