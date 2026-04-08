-- ================================================================
-- Migration: 095_drop_legacy_business_type_taxes
-- Description: Xóa bảng BusinessTypeTaxes (legacy) đã được thay thế
--              hoàn toàn bởi IndustryTaxRates (gắn với TaxRuleset).
--              Dữ liệu được archive trước khi xóa.
-- Date: 2026-04-02
-- ================================================================

-- ── Step 1: Archive dữ liệu cũ trước khi xóa ──────────────────
CREATE TABLE IF NOT EXISTS BusinessTypeTaxes_Archive (
    BusinessTypeTaxId  CHAR(36)       NOT NULL,
    BusinessTypeId     CHAR(36)       NOT NULL,
    TaxType            VARCHAR(50)    NOT NULL,
    TaxRate            DECIMAL(5, 2)  NOT NULL,
    CalculationBase    VARCHAR(50)    NOT NULL DEFAULT 'price',
    EffectiveFrom      DATE           NOT NULL,
    EffectiveTo        DATE           NULL,
    CreatedBy          CHAR(36)       NULL,
    CreatedAt          DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ArchivedAt         DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (BusinessTypeTaxId)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci
  COMMENT = 'Archive của BusinessTypeTaxes trước khi drop (deprecated bởi IndustryTaxRates)';

INSERT IGNORE INTO BusinessTypeTaxes_Archive
    (BusinessTypeTaxId, BusinessTypeId, TaxType, TaxRate, CalculationBase,
     EffectiveFrom, EffectiveTo, CreatedBy, CreatedAt, ArchivedAt)
SELECT BusinessTypeTaxId, BusinessTypeId, TaxType, TaxRate, CalculationBase,
       EffectiveFrom, EffectiveTo, CreatedBy, CreatedAt, NOW()
FROM   BusinessTypeTaxes;

-- ── Step 2: Drop bảng legacy ───────────────────────────────────
DROP TABLE IF EXISTS BusinessTypeTaxes;

-- ── Track migration ────────────────────────────────────────────
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('095_drop_legacy_business_type_taxes', '1.0.0');
