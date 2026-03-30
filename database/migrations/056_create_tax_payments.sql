-- =============================================
-- Migration 056: TaxPayments
-- Module: Accounting Book — Tax Payments
-- =============================================

CREATE TABLE TaxPayments (
    TaxPaymentId BIGINT AUTO_INCREMENT PRIMARY KEY,
    BusinessLocationId INT NOT NULL,
    PeriodId BIGINT DEFAULT NULL COMMENT 'Thuộc kỳ kế toán nào (optional)',

    -- Tax info
    TaxType VARCHAR(10) NOT NULL COMMENT 'VAT | PIT',
    Amount DECIMAL(15,2) NOT NULL COMMENT 'Số tiền đã nộp',
    PaidAt DATE NOT NULL COMMENT 'Ngày nộp',

    -- Payment details
    PaymentMethod VARCHAR(20) DEFAULT NULL COMMENT 'cash | bank',
    ReferenceNumber VARCHAR(100) DEFAULT NULL COMMENT 'Số biên lai / mã giao dịch',
    Notes TEXT DEFAULT NULL,

    -- Audit
    CreatedByUserId CHAR(36) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    DeletedAt DATETIME DEFAULT NULL COMMENT 'Soft delete',

    -- Indexes & FKs
    CONSTRAINT fk_taxpay_location FOREIGN KEY (BusinessLocationId)
        REFERENCES BusinessLocations(BusinessLocationId),
    CONSTRAINT fk_taxpay_period FOREIGN KEY (PeriodId)
        REFERENCES AccountingPeriods(PeriodId),
    INDEX idx_taxpay_location (BusinessLocationId),
    INDEX idx_taxpay_type (TaxType),
    INDEX idx_taxpay_period (PeriodId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('056_create_tax_payments', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
