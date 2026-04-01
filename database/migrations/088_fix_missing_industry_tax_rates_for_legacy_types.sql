-- =============================================
-- Migration 088: Fix missing IndustryTaxRates for legacy business types
-- Purpose:
--   Migration 064 only synced IndustryTaxRates for business types referenced by
--   non-deleted Products. This left GROCERY (and potentially other legacy types)
--   without tax rates if they were used in revenues but had no products at migration time.
--   This migration ensures ALL legacy business types get IndustryTaxRates from their
--   canonical TT152 counterpart, regardless of Product existence.
-- =============================================

SET @activeRulesetId := (
    SELECT RulesetId
    FROM TaxRulesets
    WHERE IsActive = TRUE
    ORDER BY EffectiveFrom DESC, RulesetId DESC
    LIMIT 1
);

-- Map ALL legacy business type codes to canonical TT152 categories
CREATE TEMPORARY TABLE tmp_legacy_to_canonical (
    LegacyCode VARCHAR(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    CanonicalCode VARCHAR(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    PRIMARY KEY (LegacyCode)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO tmp_legacy_to_canonical (LegacyCode, CanonicalCode) VALUES
('RETAIL', 'bt-retail'),
('GROCERY', 'bt-retail'),
('RESTAURANT', 'bt-fnb'),
('BEAUTY', 'bt-service');

-- Build rates for ALL legacy business types (not just those with Products)
CREATE TEMPORARY TABLE tmp_missing_rates AS
SELECT
    btLegacy.BusinessTypeId AS TargetBusinessTypeId,
    (itr.TaxType COLLATE utf8mb4_unicode_ci) AS TaxType,
    itr.TaxRate,
    itr.Description
FROM tmp_legacy_to_canonical map
JOIN BusinessTypes btLegacy
    ON btLegacy.Code COLLATE utf8mb4_unicode_ci = map.LegacyCode COLLATE utf8mb4_unicode_ci
JOIN BusinessTypes btCanonical
    ON btCanonical.Code COLLATE utf8mb4_unicode_ci = map.CanonicalCode COLLATE utf8mb4_unicode_ci
JOIN IndustryTaxRates itr
    ON itr.BusinessTypeId = btCanonical.BusinessTypeId
   AND itr.RulesetId = @activeRulesetId
WHERE @activeRulesetId IS NOT NULL
  AND NOT EXISTS (
    SELECT 1
    FROM IndustryTaxRates existing
    WHERE existing.RulesetId = @activeRulesetId
      AND existing.BusinessTypeId = btLegacy.BusinessTypeId
      AND existing.TaxType COLLATE utf8mb4_unicode_ci = itr.TaxType COLLATE utf8mb4_unicode_ci
  );

-- Insert missing rates
INSERT INTO IndustryTaxRates (RulesetId, BusinessTypeId, TaxType, TaxRate, Description)
SELECT
    @activeRulesetId,
    mr.TargetBusinessTypeId,
    mr.TaxType,
    mr.TaxRate,
    CONCAT('Synced from canonical TT152 mapping (migration 088)')
FROM tmp_missing_rates mr
WHERE @activeRulesetId IS NOT NULL;

-- Cleanup
DROP TEMPORARY TABLE IF EXISTS tmp_missing_rates;
DROP TEMPORARY TABLE IF EXISTS tmp_legacy_to_canonical;

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('088_fix_missing_industry_tax_rates_for_legacy_types', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
