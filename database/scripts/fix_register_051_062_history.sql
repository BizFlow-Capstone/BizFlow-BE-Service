-- =============================================
-- FIX SCRIPT: Register migration history for already-applied migrations (051-062)
-- Run this ONCE if tables already exist but __MigrationHistory is missing
-- =============================================

-- 1. Register migration history (so apply_new_migrations.bat skips these)
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES
('051_create_tax_rulesets_tables', '1.0.0'),
('052_create_accounting_template_tables', '1.0.0'),
('053_create_metadata_registry_tables', '1.0.0'),
('054_create_template_field_mappings', '1.0.0'),
('055_create_accounting_book_tables', '1.0.0'),
('056_create_tax_payments', '1.0.0'),
('057_create_formula_tables', '1.0.0'),
('058_seed_tax_rulesets', '1.0.0'),
('059_seed_accounting_templates', '1.0.0'),
('060_seed_metadata_registry', '1.0.0'),
('061_seed_template_field_mappings', '1.0.0'),
('062_seed_formula_definitions', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);

SELECT 'Migration history registered for 051-062' AS Status;
