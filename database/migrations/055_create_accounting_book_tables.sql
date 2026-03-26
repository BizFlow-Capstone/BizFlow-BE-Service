-- =============================================
-- Migration 055: AccountingBooks + AccountingBookBusinessTypes + AccountingExports
-- Module: Accounting Book — Book Generation
-- =============================================

-- AccountingBooks (Sổ kế toán — live view instances)
CREATE TABLE AccountingBooks (
    BookId BIGINT AUTO_INCREMENT PRIMARY KEY,
    BusinessLocationId INT NOT NULL,
    PeriodId BIGINT NOT NULL,
    TemplateVersionId INT NOT NULL,

    -- Context (nhóm + cách tính Owner đã chọn)
    GroupNumber TINYINT NOT NULL COMMENT 'Nhóm HKD: 1, 2, 3, 4',
    TaxMethod VARCHAR(20) DEFAULT NULL COMMENT 'method_1 | method_2 | exempt',
    RulesetId INT NOT NULL COMMENT 'Ruleset version dùng lúc tạo',

    -- Status
    Status VARCHAR(20) NOT NULL DEFAULT 'active' COMMENT 'active | archived',

    -- Timestamps
    CreatedByUserId CHAR(36) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ArchivedAt DATETIME DEFAULT NULL,

    -- Indexes & FKs
    CONSTRAINT fk_book_location FOREIGN KEY (BusinessLocationId)
        REFERENCES BusinessLocations(BusinessLocationId),
    CONSTRAINT fk_book_period FOREIGN KEY (PeriodId)
        REFERENCES AccountingPeriods(PeriodId),
    CONSTRAINT fk_book_template_version FOREIGN KEY (TemplateVersionId)
        REFERENCES AccountingTemplateVersions(TemplateVersionId),
    CONSTRAINT fk_book_ruleset FOREIGN KEY (RulesetId)
        REFERENCES TaxRulesets(RulesetId),
    INDEX idx_book_location_period (BusinessLocationId, PeriodId),
    INDEX idx_book_status (Status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- AccountingBookBusinessTypes (bảng trung gian — 1 book gộp nhiều ngành cùng tax rate)
CREATE TABLE AccountingBookBusinessTypes (
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    BookId BIGINT NOT NULL,
    BusinessTypeId CHAR(36) NOT NULL,

    -- Tax profile key để debug/trace vì sao 2 ngành được gộp chung
    TaxProfileKey VARCHAR(100) NOT NULL
        COMMENT 'Readable key: VAT_1.00|PIT_0.50|METHOD_method_1',

    -- FKs & Indexes
    CONSTRAINT fk_abbt_book FOREIGN KEY (BookId)
        REFERENCES AccountingBooks(BookId) ON DELETE CASCADE,
    CONSTRAINT fk_abbt_business_type FOREIGN KEY (BusinessTypeId)
        REFERENCES BusinessTypes(BusinessTypeId),
    UNIQUE INDEX idx_abbt_book_bt (BookId, BusinessTypeId),
    INDEX idx_abbt_profile (TaxProfileKey)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- AccountingExports (Lịch sử xuất sổ — snapshot)
CREATE TABLE AccountingExports (
    ExportId BIGINT AUTO_INCREMENT PRIMARY KEY,
    BookId BIGINT NOT NULL,

    -- Snapshot context (ghi lại tại thời điểm xuất)
    GroupNumber TINYINT NOT NULL,
    TaxMethod VARCHAR(20) DEFAULT NULL,
    RulesetVersion VARCHAR(20) NOT NULL,

    -- Data snapshot
    SummaryJson LONGTEXT NOT NULL
        COMMENT 'Tóm tắt: tổng DT, tổng CP, thuế phải nộp, số dòng...',
    DataRowCount INT NOT NULL DEFAULT 0 COMMENT 'Số dòng dữ liệu',

    -- File output
    ExportFormat VARCHAR(10) NOT NULL COMMENT 'pdf | xlsx',
    FileUrl VARCHAR(500) DEFAULT NULL COMMENT 'URL file đã export',
    FilePublicId VARCHAR(255) DEFAULT NULL COMMENT 'Cloudinary public ID',

    -- Audit
    ExportedByUserId CHAR(36) NOT NULL,
    ExportedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Notes TEXT DEFAULT NULL,

    -- Indexes & FKs
    CONSTRAINT fk_export_book FOREIGN KEY (BookId)
        REFERENCES AccountingBooks(BookId),
    INDEX idx_export_book (BookId),
    INDEX idx_export_date (ExportedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('055_create_accounting_book_tables', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
