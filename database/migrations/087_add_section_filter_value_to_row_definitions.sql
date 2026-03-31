-- Migration: 087_add_section_filter_value_to_row_definitions
-- Purpose: Add SectionFilterValue column to TemplateRowDefinitions so section
--          filter values (e.g. "cash", "bank") are data-driven instead of hardcoded.

-- 1. Add column (idempotent)
SET @col_exists = (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'TemplateRowDefinitions' AND COLUMN_NAME = 'SectionFilterValue'
);

SET @sql = IF(@col_exists = 0,
    'ALTER TABLE TemplateRowDefinitions ADD COLUMN SectionFilterValue VARCHAR(50) NULL AFTER SectionType',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- 2. Seed filter values for existing section_header rows
-- S2c (TemplateVersionId=4): revenue_cost sections
UPDATE TemplateRowDefinitions SET SectionFilterValue = 'revenue'
WHERE RowDefId = 16 AND RowType = 'section_header' AND SectionFilterValue IS NULL;

UPDATE TemplateRowDefinitions SET SectionFilterValue = 'cost'
WHERE RowDefId = 19 AND RowType = 'section_header' AND SectionFilterValue IS NULL;

-- S2e (TemplateVersionId=6): cash_bank sections
UPDATE TemplateRowDefinitions SET SectionFilterValue = 'cash'
WHERE RowDefId = 27 AND RowType = 'section_header' AND SectionFilterValue IS NULL;

UPDATE TemplateRowDefinitions SET SectionFilterValue = 'bank'
WHERE RowDefId = 31 AND RowType = 'section_header' AND SectionFilterValue IS NULL;
