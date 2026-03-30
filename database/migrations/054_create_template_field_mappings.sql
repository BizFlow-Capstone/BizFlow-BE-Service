-- =============================================
-- Migration 054: TemplateFieldMappings (v2 — FK refs + FormulaId)
-- Module: Accounting Book — Template Management (v2)
-- =============================================

CREATE TABLE TemplateFieldMappings (
    MappingId INT AUTO_INCREMENT PRIMARY KEY,
    TemplateVersionId INT NOT NULL,

    -- Field Identity
    FieldCode VARCHAR(50) NOT NULL
        COMMENT 'Code: stt, date, description, revenue, vat_amount...',
    FieldLabel VARCHAR(200) NOT NULL
        COMMENT 'Nhãn hiển thị: STT, Ngày tháng, Diễn giải...',
    FieldType VARCHAR(20) NOT NULL
        COMMENT 'auto_increment | date | text | decimal | computed',

    -- Data Source (v2: FK → Metadata Registry)
    SourceType VARCHAR(30) DEFAULT NULL
        COMMENT 'query | formula | static | auto',
    SourceEntityId INT DEFAULT NULL
        COMMENT 'FK → MappableEntities. NULL khi SourceType = auto | formula | static',
    SourceFieldId INT DEFAULT NULL
        COMMENT 'FK → MappableFields. NULL khi SourceType = auto | formula | static',
    FilterJson JSON DEFAULT NULL
        COMMENT 'Filter khi query: {"transactionType":"sale","moneyChannel":"cash"}',
    AggregationType VARCHAR(20) DEFAULT NULL
        COMMENT 'sum | count | avg | none',

    -- Formula (computed fields)
    FormulaId BIGINT DEFAULT NULL
        COMMENT 'FK → FormulaDefinitions. Set khi SourceType = formula',
    FormulaExpression VARCHAR(500) DEFAULT NULL
        COMMENT 'Legacy: Công thức tham chiếu FieldCode khác',
    DependsOn JSON DEFAULT NULL
        COMMENT 'Danh sách FieldCode phụ thuộc: ["revenue","vat_rate"]',
    CalculationOrder INT DEFAULT NULL
        COMMENT 'Thứ tự tính. Formula ở order thấp phải tính trước',

    -- Export Positioning
    ExportColumn VARCHAR(10) DEFAULT NULL COMMENT 'Excel column: A, B, C...',
    SortOrder INT NOT NULL DEFAULT 0,
    IsRequired BOOLEAN NOT NULL DEFAULT TRUE,

    -- FKs & Indexes
    CONSTRAINT fk_tfm_version FOREIGN KEY (TemplateVersionId)
        REFERENCES AccountingTemplateVersions(TemplateVersionId),
    CONSTRAINT fk_tfm_source_entity FOREIGN KEY (SourceEntityId)
        REFERENCES MappableEntities(EntityId),
    CONSTRAINT fk_tfm_source_field FOREIGN KEY (SourceFieldId)
        REFERENCES MappableFields(FieldId),
    -- FormulaId FK added in 057 after FormulaDefinitions table is created
    INDEX idx_tfm_version (TemplateVersionId),
    INDEX idx_tfm_source (SourceEntityId, SourceFieldId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('054_create_template_field_mappings', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
