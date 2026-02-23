-- Migration: 018_drop_unit_from_products_imports
-- Description: Remove Unit column from ProductsImports (quantity is always in base unit)
-- Date: 2026-02-19

ALTER TABLE ProductsImports
    DROP COLUMN Unit;

-- Insert this migration
INSERT INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('018_drop_unit_from_products_imports', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
