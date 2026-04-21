-- =============================================
-- Migration  : 116_add_document_url_and_public_id_to_revenues
-- Description: Add DocumentUrl and DocumentPublicId to Revenues
-- Date       : 2026-04-21
-- =============================================

SET @has_document_url = (
    SELECT COUNT(*)
    FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = 'Revenues'
      AND column_name = 'DocumentUrl'
);

SET @sql = IF(
    @has_document_url = 0,
    'ALTER TABLE Revenues ADD COLUMN DocumentUrl VARCHAR(500) NULL COMMENT ''Voucher/invoice URL (Cloudinary)'' AFTER MoneyChannel',
    'SELECT ''Skip: Revenues.DocumentUrl already exists'' AS Info'
);

PREPARE stmt_add_document_url FROM @sql;
EXECUTE stmt_add_document_url;
DEALLOCATE PREPARE stmt_add_document_url;

SET @has_document_public_id = (
    SELECT COUNT(*)
    FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = 'Revenues'
      AND column_name = 'DocumentPublicId'
);

SET @sql = IF(
    @has_document_public_id = 0,
    'ALTER TABLE Revenues ADD COLUMN DocumentPublicId VARCHAR(255) NULL COMMENT ''Cloudinary public ID of the voucher'' AFTER DocumentUrl',
    'SELECT ''Skip: Revenues.DocumentPublicId already exists'' AS Info'
);

PREPARE stmt_add_document_public_id FROM @sql;
EXECUTE stmt_add_document_public_id;
DEALLOCATE PREPARE stmt_add_document_public_id;

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('116_add_document_url_and_public_id_to_revenues', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
