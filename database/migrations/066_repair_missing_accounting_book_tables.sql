-- =============================================
-- Migration 066: Repair missing accounting book tables
-- Purpose: create missing tables if migration history was marked but DDL was not fully applied
-- =============================================

CREATE TABLE IF NOT EXISTS AccountingBookBusinessTypes (
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    BookId BIGINT NOT NULL,
    BusinessTypeId CHAR(36) NOT NULL,
    TaxProfileKey VARCHAR(100) NOT NULL COMMENT 'Readable key: VAT_1.00|PIT_0.50|METHOD_method_1',

    CONSTRAINT fk_abbt_book FOREIGN KEY (BookId)
        REFERENCES AccountingBooks(BookId) ON DELETE CASCADE,
    CONSTRAINT fk_abbt_business_type FOREIGN KEY (BusinessTypeId)
        REFERENCES BusinessTypes(BusinessTypeId),
    UNIQUE INDEX idx_abbt_book_bt (BookId, BusinessTypeId),
    INDEX idx_abbt_profile (TaxProfileKey)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS AccountingExports (
    ExportId BIGINT AUTO_INCREMENT PRIMARY KEY,
    BookId BIGINT NOT NULL,

    GroupNumber TINYINT NOT NULL,
    TaxMethod VARCHAR(20) DEFAULT NULL,
    RulesetVersion VARCHAR(20) NOT NULL,

    SummaryJson LONGTEXT NOT NULL COMMENT 'Tóm tắt: tổng DT, tổng CP, thuế phải nộp, số dòng...',
    DataRowCount INT NOT NULL DEFAULT 0 COMMENT 'Số dòng dữ liệu',

    ExportFormat VARCHAR(10) NOT NULL COMMENT 'pdf | xlsx',
    FileUrl VARCHAR(500) DEFAULT NULL,
    FilePublicId VARCHAR(255) DEFAULT NULL,

    ExportedByUserId CHAR(36) NOT NULL,
    ExportedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Notes TEXT DEFAULT NULL,

    CONSTRAINT fk_export_book FOREIGN KEY (BookId)
        REFERENCES AccountingBooks(BookId),
    INDEX idx_export_book (BookId),
    INDEX idx_export_date (ExportedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('066_repair_missing_accounting_book_tables', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
