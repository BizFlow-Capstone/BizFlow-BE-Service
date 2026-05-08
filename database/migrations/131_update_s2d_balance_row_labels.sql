-- Update S2d (TemplateVersionId = 5) balance row labels to include product name placeholder
UPDATE TemplateRowDefinitions
SET RowLabel = 'Ton dau ky - {groupName}'
WHERE TemplateVersionId = 5
  AND RowType = 'balance_row'
  AND Position = 'start_of_book';

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('131_update_s2d_balance_row_labels', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
