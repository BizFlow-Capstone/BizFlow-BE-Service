-- Migration: 029_rename_productimports_to_productsimports
-- Description: Rename ProductImports table back to ProductsImports
-- Date: 2026-03-11

RENAME TABLE ProductImports TO ProductsImports;

-- Insert this migration
INSERT INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('029_rename_productimports_to_productsimports', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
