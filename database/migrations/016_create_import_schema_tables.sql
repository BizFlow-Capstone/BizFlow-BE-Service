-- Migration: 016_create_import_schema_tables
-- Description: Add ImportSchema and ImportSchemaVersion tables for dynamic import templates
-- Date: 2026-02-19

-- =============================================
-- IMPORT SCHEMA TABLE
-- Defines the template types for import operations
-- =============================================
CREATE TABLE IF NOT EXISTS ImportSchemas (
    ImportSchemaId INT NOT NULL AUTO_INCREMENT,
    TemplateCode VARCHAR(50) NOT NULL COMMENT 'Unique code identifying the template type',
    Name VARCHAR(100) NOT NULL COMMENT 'Human-readable name of the template',
    PRIMARY KEY (ImportSchemaId),
    UNIQUE INDEX idx_import_schema_template_code (TemplateCode)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- IMPORT SCHEMA VERSION TABLE
-- Stores versioned JSON schema definitions for each template
-- Only one version per schema should be IsActive = TRUE at a time
-- =============================================
CREATE TABLE IF NOT EXISTS ImportSchemaVersions (
    ImportSchemaVersionId INT NOT NULL AUTO_INCREMENT,
    ImportSchemaId INT NOT NULL COMMENT 'Reference to parent ImportSchema',
    SchemaJson LONGTEXT NOT NULL COMMENT 'JSON schema definition for the import template',
    IsActive BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'Only one active version per schema at a time',
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'When this version was created',
    PRIMARY KEY (ImportSchemaVersionId),
    CONSTRAINT fk_import_schema_version_schema FOREIGN KEY (ImportSchemaId)
        REFERENCES ImportSchemas(ImportSchemaId) ON DELETE CASCADE ON UPDATE CASCADE,
    INDEX idx_import_schema_version_schema_id (ImportSchemaId),
    INDEX idx_import_schema_version_is_active (IsActive)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Insert this migration
INSERT INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('016_create_import_schema_tables', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
