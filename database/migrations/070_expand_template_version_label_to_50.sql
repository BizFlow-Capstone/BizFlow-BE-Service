-- Migration 070: Expand AccountingTemplateVersions.VersionLabel to 50 chars

ALTER TABLE AccountingTemplateVersions
    MODIFY COLUMN VersionLabel VARCHAR(50) NOT NULL COMMENT 'v1.0, v2.0...';

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('070_expand_template_version_label_to_50', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
