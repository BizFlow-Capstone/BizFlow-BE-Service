-- Migration: 082_add_data_source_type_to_templates
-- Purpose: Add DataSourceType column to AccountingTemplates so rendering code
--          dispatches data queries based on DB config instead of hardcoded template codes.
-- Values: revenues (default), revenue_cost, gl_entries, stock_movements
-- Idempotent: safe to re-run.

DROP PROCEDURE IF EXISTS migrate_082;
DELIMITER $$
CREATE PROCEDURE migrate_082()
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'AccountingTemplates' AND COLUMN_NAME = 'DataSourceType'
    ) THEN
        ALTER TABLE AccountingTemplates
        ADD COLUMN DataSourceType VARCHAR(30) NOT NULL DEFAULT 'revenues'
            COMMENT 'Data source for rows: revenues | revenue_cost | gl_entries | stock_movements';
    END IF;
END$$
DELIMITER ;

CALL migrate_082();
DROP PROCEDURE IF EXISTS migrate_082;

-- S1a, S2a, S2b → revenues (already default)
-- S2c → revenue_cost (merges revenue + cost rows)
UPDATE AccountingTemplates SET DataSourceType = 'revenue_cost' WHERE TemplateCode = 'S2c';
-- S2d → stock_movements
UPDATE AccountingTemplates SET DataSourceType = 'stock_movements' WHERE TemplateCode = 'S2d';
-- S2e → gl_entries
UPDATE AccountingTemplates SET DataSourceType = 'gl_entries' WHERE TemplateCode = 'S2e';

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('082_add_data_source_type_to_templates', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
