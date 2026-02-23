-- Migration: 021_cleanup_legacy_imports
-- Description: Remove the 4 legacy import rows inserted by migration 008
--              before ImportCode and other columns were added.
--              These rows have NULL ImportCode and are superseded by migration 020 data.
-- Date: 2026-02-23

-- Delete legacy ProductsImports rows first (FK constraint)
DELETE FROM ProductsImports
WHERE ImportId IN (
    SELECT ImportId FROM Imports WHERE ImportCode IS NULL
);

-- Delete legacy Import rows (NULL ImportCode = old data from migration 008)
DELETE FROM Imports WHERE ImportCode IS NULL;

-- Insert this migration
INSERT INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('021_cleanup_legacy_imports', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
