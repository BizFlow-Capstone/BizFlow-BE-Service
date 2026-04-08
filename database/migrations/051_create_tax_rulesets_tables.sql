-- =============================================
-- Migration 051: Tax Rulesets + Group Rules + Industry Tax Rates
-- Module: Rule Engine
-- =============================================

-- TaxRulesets (Version container cho Rule Engine)
CREATE TABLE TaxRulesets (
    RulesetId INT AUTO_INCREMENT PRIMARY KEY,

    -- Identity
    Code VARCHAR(50) NOT NULL COMMENT 'Unique code: TT152_2025',
    Name VARCHAR(200) NOT NULL COMMENT 'Thông tư 152/2025/TT-BTC',
    Description TEXT DEFAULT NULL,
    Version VARCHAR(20) NOT NULL COMMENT 'Semantic versioning: 1.0.0',

    -- Lifecycle
    EffectiveFrom DATE NOT NULL COMMENT 'Ngày bắt đầu hiệu lực',
    EffectiveTo DATE DEFAULT NULL COMMENT 'NULL = vô thời hạn',
    IsActive BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'Chỉ 1 active tại 1 thời điểm',

    -- Audit
    CreatedByUserId CHAR(36) DEFAULT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    UNIQUE INDEX idx_ruleset_code_version (Code, Version)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- TaxGroupRules (Phân nhóm HKD — SQL + JSON Hybrid)
CREATE TABLE TaxGroupRules (
    RuleId INT AUTO_INCREMENT PRIMARY KEY,
    RulesetId INT NOT NULL,

    -- Group Identity (structured — ít thay đổi)
    GroupNumber TINYINT NOT NULL COMMENT '1, 2, 3, 4',
    GroupName VARCHAR(100) NOT NULL COMMENT 'Nhóm 1, Nhóm 2...',
    GroupDescription TEXT DEFAULT NULL,

    -- Matching Conditions (JSON — linh hoạt)
    ConditionsJson JSON NOT NULL COMMENT 'Tiêu chí phân nhóm: revenue, employees, capital...',

    -- Outcomes (JSON — linh hoạt)
    OutcomesJson JSON NOT NULL COMMENT 'Kết quả khi match: tax methods, rates, books, reporting...',

    -- Ordering
    SortOrder INT NOT NULL DEFAULT 0,

    -- Indexes & FKs
    CONSTRAINT fk_tgr_ruleset FOREIGN KEY (RulesetId)
        REFERENCES TaxRulesets(RulesetId),
    INDEX idx_tgr_ruleset (RulesetId),
    UNIQUE INDEX idx_tgr_ruleset_group (RulesetId, GroupNumber)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- IndustryTaxRates (Thuế suất theo ngành nghề)
CREATE TABLE IndustryTaxRates (
    RateId INT AUTO_INCREMENT PRIMARY KEY,
    RulesetId INT NOT NULL,
    BusinessTypeId CHAR(36) NOT NULL,

    -- Tax type
    TaxType VARCHAR(20) NOT NULL COMMENT 'VAT | PIT_METHOD_1',

    -- Rate
    TaxRate DECIMAL(5,4) NOT NULL COMMENT 'VD: 0.0100 = 1%, 0.0050 = 0.5%',

    -- Description
    Description VARCHAR(200) DEFAULT NULL,

    -- Indexes & FKs
    CONSTRAINT fk_itr_ruleset FOREIGN KEY (RulesetId)
        REFERENCES TaxRulesets(RulesetId),
    CONSTRAINT fk_itr_business_type FOREIGN KEY (BusinessTypeId)
        REFERENCES BusinessTypes(BusinessTypeId),
    INDEX idx_itr_ruleset (RulesetId),
    UNIQUE INDEX idx_itr_unique (RulesetId, BusinessTypeId, TaxType)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('051_create_tax_rulesets_tables', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
