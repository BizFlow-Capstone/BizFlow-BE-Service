-- =============================================
-- Migration 053: Metadata Registry — MappableEntities + MappableFields
-- Module: Accounting Book — Template Management (v2)
-- =============================================

-- MappableEntities — Whitelist nguồn dữ liệu cho field mapping
CREATE TABLE MappableEntities (
    EntityId INT AUTO_INCREMENT PRIMARY KEY,

    -- Identity
    EntityCode VARCHAR(50) NOT NULL
        COMMENT 'Mã kỹ thuật: orders, order_details, gl_entries, costs, tax_payments',
    DisplayName VARCHAR(200) NOT NULL
        COMMENT 'Tên hiển thị trên UI: "Đơn hàng", "Chi tiết đơn hàng"',
    Description TEXT DEFAULT NULL
        COMMENT 'Mô tả tóm tắt entity này chứa data gì',

    -- Context
    Category VARCHAR(50) NOT NULL DEFAULT 'revenue'
        COMMENT 'Phân loại: revenue | cost | tax | cashflow | general',

    -- Status
    IsActive BOOLEAN NOT NULL DEFAULT TRUE
        COMMENT 'FALSE = ẩn khỏi dropdown, mapping cũ vẫn giữ nguyên',

    -- Audit
    CreatedByUserId CHAR(36) DEFAULT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,

    UNIQUE INDEX idx_me_code (EntityCode)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- MappableFields — Whitelist field cho từng entity
CREATE TABLE MappableFields (
    FieldId INT AUTO_INCREMENT PRIMARY KEY,
    EntityId INT NOT NULL,

    -- Field Identity
    FieldCode VARCHAR(100) NOT NULL
        COMMENT 'Tên field kỹ thuật: TotalAmount, CompletedAt',
    DisplayName VARCHAR(200) NOT NULL
        COMMENT 'Tên hiển thị trên UI: "Tổng tiền", "Ngày hoàn tất"',
    Description TEXT DEFAULT NULL
        COMMENT 'Giải thích: "Tổng tiền đơn hàng sau giảm giá"',

    -- Data Type
    DataType VARCHAR(20) NOT NULL
        COMMENT 'decimal | date | text | integer | boolean',

    -- Allowed Operations
    AllowedAggregations JSON NOT NULL DEFAULT ('["none"]')
        COMMENT 'Aggregation cho phép: ["sum","avg","count","none"]',

    -- Status
    IsActive BOOLEAN NOT NULL DEFAULT TRUE
        COMMENT 'FALSE = ẩn khỏi dropdown, không ảnh hưởng mapping cũ',

    -- Audit
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,

    -- FKs & Indexes
    CONSTRAINT fk_mf_entity FOREIGN KEY (EntityId)
        REFERENCES MappableEntities(EntityId),
    INDEX idx_mf_entity (EntityId),
    UNIQUE INDEX idx_mf_entity_field (EntityId, FieldCode)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('053_create_metadata_registry_tables', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
