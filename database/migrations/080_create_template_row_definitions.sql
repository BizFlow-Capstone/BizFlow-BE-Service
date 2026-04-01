-- Migration: 080_create_template_row_definitions
-- Create TemplateRowDefinitions table to define rendering rows per template version.

CREATE TABLE IF NOT EXISTS TemplateRowDefinitions (
    RowDefId INT AUTO_INCREMENT PRIMARY KEY,
    TemplateVersionId INT NOT NULL,

    -- Row identity
    RowType VARCHAR(30) NOT NULL
        COMMENT 'industry_header | data_placeholder | subtotal | tax_line | grand_total | section_header | section_subtotal | balance_row | monthly_total | quarterly_total | profit_row',
    RowLabel VARCHAR(200) DEFAULT NULL
        COMMENT 'Label template: "{groupIndex}. {businessTypeName}", "Tong cong ({groupIndex})"',

    -- Positioning
    Position VARCHAR(20) NOT NULL DEFAULT 'per_group'
        COMMENT 'per_group | per_section | end_of_book | start_of_book',
    SortOrder INT NOT NULL DEFAULT 0
        COMMENT 'Sort order in group/section',

    -- Grouping
    GroupByField VARCHAR(50) DEFAULT NULL
        COMMENT 'Grouping field: BusinessTypeId | Section | ProductId',
    SectionType VARCHAR(30) DEFAULT NULL
        COMMENT 'industry_group | revenue_cost | cash_bank | per_product',

    -- Data binding
    VisibleFieldCodes JSON DEFAULT NULL
        COMMENT 'Visible columns: ["dien_giai","so_tien"]. NULL = all',
    FormulaId BIGINT DEFAULT NULL,

    -- Tax metadata
    TaxType VARCHAR(10) DEFAULT NULL COMMENT 'VAT | PIT | NULL',

    -- Audit
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    -- CHECK CONSTRAINTS
    CONSTRAINT chk_rowdef_row_type CHECK (
        RowType IN ('industry_header', 'data_placeholder', 'subtotal', 'tax_line',
                    'grand_total', 'section_header', 'section_subtotal',
                    'balance_row', 'monthly_total', 'quarterly_total', 'profit_row')
    ),
    CONSTRAINT chk_rowdef_position CHECK (
        Position IN ('per_group', 'per_section', 'end_of_book', 'start_of_book')
    ),
    CONSTRAINT chk_rowdef_section_type CHECK (
        SectionType IS NULL OR SectionType IN ('industry_group', 'revenue_cost', 'cash_bank', 'per_product')
    ),
    CONSTRAINT chk_rowdef_tax_type CHECK (
        TaxType IS NULL OR TaxType IN ('VAT', 'PIT')
    ),

    -- FKs & indexes
    CONSTRAINT fk_rowdef_version FOREIGN KEY (TemplateVersionId)
        REFERENCES AccountingTemplateVersions(TemplateVersionId) ON DELETE CASCADE,
    CONSTRAINT fk_rowdef_formula FOREIGN KEY (FormulaId)
        REFERENCES FormulaDefinitions(FormulaId) ON DELETE SET NULL,
    INDEX idx_rowdef_version_sort (TemplateVersionId, Position, SortOrder)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('080_create_template_row_definitions', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
