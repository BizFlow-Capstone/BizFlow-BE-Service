-- =============================================
-- Migration 052: Accounting Templates + Template Versions
-- Module: Accounting Book — Template Management
-- =============================================

-- AccountingTemplates (Mẫu sổ kế toán)
CREATE TABLE AccountingTemplates (
    TemplateId INT AUTO_INCREMENT PRIMARY KEY,

    -- Identity
    TemplateCode VARCHAR(20) NOT NULL COMMENT 'S1a | S2a | S2b | S2c | S2d | S2e',
    Name VARCHAR(200) NOT NULL COMMENT 'Sổ chi tiết bán hàng (Nhóm 1)',
    Description TEXT DEFAULT NULL,

    -- Classification
    ApplicableGroups JSON NOT NULL
        COMMENT 'Nhóm áp dụng: [1] hoặc [2] hoặc [2,3,4]',
    ApplicableMethods JSON DEFAULT NULL
        COMMENT 'Cách tính: ["method_1"] hoặc ["method_2"] hoặc NULL=tất cả',

    -- Status
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,

    -- Audit
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    UNIQUE INDEX idx_template_code (TemplateCode)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- AccountingTemplateVersions
CREATE TABLE AccountingTemplateVersions (
    TemplateVersionId INT AUTO_INCREMENT PRIMARY KEY,
    TemplateId INT NOT NULL,

    -- Version
    VersionLabel VARCHAR(20) NOT NULL COMMENT 'v1.0, v2.0...',
    IsActive BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'Chỉ 1 active version per template',
    EffectiveFrom DATE DEFAULT NULL,

    -- Template file cho export
    TemplateFileUrl VARCHAR(500) DEFAULT NULL COMMENT 'URL file template (xlsx/docx)',

    -- Metadata
    ChangeNotes TEXT DEFAULT NULL COMMENT 'Ghi chú thay đổi so với version trước',

    -- Audit
    CreatedByUserId CHAR(36) DEFAULT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    -- Indexes & FKs
    CONSTRAINT fk_tv_template FOREIGN KEY (TemplateId)
        REFERENCES AccountingTemplates(TemplateId),
    INDEX idx_tv_template (TemplateId),
    INDEX idx_tv_active (IsActive)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('052_create_accounting_template_tables', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
