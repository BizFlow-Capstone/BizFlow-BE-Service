-- =============================================
-- Migration 064: Reconcile BusinessType tax mappings
-- Purpose:
-- 1) Align IndustryTaxRates with BusinessTypes that are actually used by Products.
-- 2) Keep this migration focused on ruleset tax mapping only.
-- =============================================

-- Active ruleset and effective date baseline
SET @activeRulesetId := (
    SELECT RulesetId
    FROM TaxRulesets
    WHERE IsActive = TRUE
    ORDER BY EffectiveFrom DESC, RulesetId DESC
    LIMIT 1
);

-- Distinct business types that are actually used by non-deleted products
DROP TEMPORARY TABLE IF EXISTS tmp_used_business_types;
CREATE TEMPORARY TABLE tmp_used_business_types (
        BusinessTypeId CHAR(36) NOT NULL,
        PRIMARY KEY (BusinessTypeId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO tmp_used_business_types (BusinessTypeId)
SELECT DISTINCT p.BusinessTypeId
FROM Products p
WHERE p.DeletedAt IS NULL
    AND p.BusinessTypeId IS NOT NULL;

-- Map legacy business type codes to canonical TT152 categories
CREATE TEMPORARY TABLE tmp_business_type_mapping (
    LegacyCode VARCHAR(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    CanonicalCode VARCHAR(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    PRIMARY KEY (LegacyCode)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO tmp_business_type_mapping (LegacyCode, CanonicalCode) VALUES
('RETAIL', 'bt-retail'),
('GROCERY', 'bt-retail'),
('RESTAURANT', 'bt-fnb'),
('BEAUTY', 'bt-service');

-- Build reconciled rates for product-used business types from canonical TT152 rates
DROP TEMPORARY TABLE IF EXISTS tmp_reconciled_rates;
CREATE TEMPORARY TABLE tmp_reconciled_rates (
    TargetBusinessTypeId CHAR(36) NOT NULL,
    TaxType VARCHAR(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    TaxRate DECIMAL(5,4) NOT NULL,
    PRIMARY KEY (TargetBusinessTypeId, TaxType)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO tmp_reconciled_rates (TargetBusinessTypeId, TaxType, TaxRate)
SELECT
    btLegacy.BusinessTypeId AS TargetBusinessTypeId,
    (itr.TaxType COLLATE utf8mb4_unicode_ci) AS TaxType,
    itr.TaxRate
FROM tmp_used_business_types ubt
JOIN BusinessTypes btLegacy
    ON btLegacy.BusinessTypeId = ubt.BusinessTypeId
JOIN tmp_business_type_mapping map
    ON map.LegacyCode COLLATE utf8mb4_unicode_ci = btLegacy.Code COLLATE utf8mb4_unicode_ci
JOIN BusinessTypes btCanonical
    ON btCanonical.Code COLLATE utf8mb4_unicode_ci = map.CanonicalCode COLLATE utf8mb4_unicode_ci
JOIN IndustryTaxRates itr
    ON itr.BusinessTypeId = btCanonical.BusinessTypeId
   AND itr.RulesetId = @activeRulesetId
WHERE @activeRulesetId IS NOT NULL;

-- Update existing IndustryTaxRates for used business types
UPDATE IndustryTaxRates itr
JOIN tmp_reconciled_rates rr
    ON rr.TargetBusinessTypeId = itr.BusinessTypeId
   AND rr.TaxType = itr.TaxType
SET itr.TaxRate = rr.TaxRate
WHERE itr.RulesetId = @activeRulesetId;

-- Insert missing IndustryTaxRates for used business types
INSERT INTO IndustryTaxRates (RulesetId, BusinessTypeId, TaxType, TaxRate, Description)
SELECT
    @activeRulesetId,
    rr.TargetBusinessTypeId,
    rr.TaxType,
    rr.TaxRate,
    CONCAT('Synced from canonical TT152 mapping (migration 064)')
FROM tmp_reconciled_rates rr
LEFT JOIN IndustryTaxRates existing
    ON existing.RulesetId = @activeRulesetId
   AND existing.BusinessTypeId = rr.TargetBusinessTypeId
   AND existing.TaxType = rr.TaxType
WHERE @activeRulesetId IS NOT NULL
    AND existing.RateId IS NULL;

-- Cleanup temp tables
DROP TEMPORARY TABLE IF EXISTS tmp_reconciled_rates;
DROP TEMPORARY TABLE IF EXISTS tmp_business_type_mapping;
DROP TEMPORARY TABLE IF EXISTS tmp_used_business_types;

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('064_reconcile_business_type_tax_mappings', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
