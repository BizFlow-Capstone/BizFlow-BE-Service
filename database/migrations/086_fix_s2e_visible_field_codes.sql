-- Migration: 086_fix_s2e_visible_field_codes
-- Fix: S2e balance_row VisibleFieldCodes referenced "so_tien" which doesn't exist
-- in S2e columns. Should use "thu_vao" (debit/balance column).

UPDATE TemplateRowDefinitions
SET VisibleFieldCodes = JSON_ARRAY('dien_giai','thu_vao')
WHERE TemplateVersionId = 6
  AND RowType = 'balance_row';

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('086_fix_s2e_visible_field_codes', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
