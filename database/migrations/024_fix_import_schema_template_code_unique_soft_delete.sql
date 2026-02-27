-- Migration: 024_fix_import_schema_template_code_unique_soft_delete
-- Description: Drop the unique constraint on TemplateCode.
--              Uniqueness for active schemas is enforced at the application layer.
--              This allows soft-deleted schemas to free up their TemplateCode for reuse.
-- Date: 2026-02-25

DROP INDEX idx_import_schema_template_code ON ImportSchemas;

INSERT INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('024_fix_import_schema_template_code_unique_soft_delete', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
