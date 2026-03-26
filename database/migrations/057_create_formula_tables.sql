-- =============================================
-- Migration 057: FormulaDefinitions + FormulaResults + FK from TemplateFieldMappings
-- Module: Formula Engine
-- =============================================

-- FormulaDefinitions — Pool công thức tính toán, reusable across templates
CREATE TABLE FormulaDefinitions (
    FormulaId BIGINT AUTO_INCREMENT PRIMARY KEY,

    -- Identity
    Code VARCHAR(50) NOT NULL
        COMMENT 'Mã công thức: S2A_QUARTERLY_TOTAL, S2C_PROFIT, S2D_WEIGHTED_AVG...',
    Name VARCHAR(255) NOT NULL
        COMMENT 'Tên: "Tổng doanh thu quý", "Chênh lệch DT-CP"',
    Description TEXT DEFAULT NULL
        COMMENT 'Giải thích công thức và cách áp dụng',

    -- Type
    FormulaType VARCHAR(20) NOT NULL
        COMMENT 'AGGREGATE | CELL_REF | TAX_RATE | WEIGHTED_AVG | EXTERNAL_LOOKUP',

    -- Expression (structured JSON AST)
    ExpressionJson JSON NOT NULL
        COMMENT 'Cây biểu thức JSON — xem tax-formular-engine.md Section 4',

    -- Result
    ResultDataType VARCHAR(10) NOT NULL DEFAULT 'decimal'
        COMMENT 'decimal | integer',
    RoundingMode VARCHAR(20) DEFAULT NULL
        COMMENT 'floor | ceil | round_half_up | null = không làm tròn',
    RoundingPrecision TINYINT DEFAULT 0
        COMMENT 'Số chữ số thập phân (0 = làm tròn đến đơn vị)',

    -- Status
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,

    -- Audit
    CreatedByUserId CHAR(36) DEFAULT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,

    UNIQUE INDEX idx_fd_code (Code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- FormulaResults — Cache computation results
CREATE TABLE FormulaResults (
    ResultId BIGINT AUTO_INCREMENT PRIMARY KEY,

    -- Keys
    BookId BIGINT NOT NULL
        COMMENT 'FK → AccountingBooks — sổ nào',
    FormulaId BIGINT NOT NULL
        COMMENT 'FK → FormulaDefinitions — công thức nào',

    -- Context (scope cho kết quả)
    -- Dùng empty string thay vì NULL để UNIQUE INDEX hoạt động đúng
    ProductId CHAR(36) NOT NULL DEFAULT ''
        COMMENT 'Scope theo sản phẩm (S2d). Empty = không scope',
    BusinessTypeId CHAR(36) NOT NULL DEFAULT ''
        COMMENT 'Scope theo ngành (S2a/S2b). Empty = không scope',
    SectionCode VARCHAR(50) NOT NULL DEFAULT ''
        COMMENT 'Scope theo phần: cash | bank (S2e), revenue | cost (S2c). Empty = không scope',

    -- Result
    ResultValue DECIMAL(18,4) NOT NULL
        COMMENT 'Giá trị kết quả sau tính toán',

    -- Cache Management
    ComputedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
        COMMENT 'Thời điểm tính gần nhất',
    IsStale BOOLEAN NOT NULL DEFAULT FALSE
        COMMENT 'TRUE = data gốc đã thay đổi, cần tính lại',

    -- FKs & Indexes
    CONSTRAINT fk_fr_book FOREIGN KEY (BookId)
        REFERENCES AccountingBooks(BookId),
    CONSTRAINT fk_fr_formula FOREIGN KEY (FormulaId)
        REFERENCES FormulaDefinitions(FormulaId),

    UNIQUE INDEX idx_fr_composite (BookId, FormulaId, ProductId, BusinessTypeId, SectionCode),
    INDEX idx_fr_stale (BookId, IsStale)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- Add FormulaId FK to TemplateFieldMappings (deferred from 054)
ALTER TABLE TemplateFieldMappings
    ADD CONSTRAINT fk_tfm_formula FOREIGN KEY (FormulaId)
        REFERENCES FormulaDefinitions(FormulaId),
    ADD INDEX idx_tfm_formula (FormulaId);

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('057_create_formula_tables', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
