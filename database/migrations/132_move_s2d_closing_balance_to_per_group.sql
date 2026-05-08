-- Move S2d (TemplateVersionId = 5) "Ton cuoi ky" balance row from global footer
-- to per-product section so it appears once per product, not as a single global row.
UPDATE TemplateRowDefinitions
SET Position  = 'per_group',
    SortOrder = 3,
    RowLabel  = 'Ton cuoi ky - {groupName}'
WHERE TemplateVersionId = 5
  AND RowType            = 'balance_row'
  AND Position           = 'end_of_book';

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('132_move_s2d_closing_balance_to_per_group', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
