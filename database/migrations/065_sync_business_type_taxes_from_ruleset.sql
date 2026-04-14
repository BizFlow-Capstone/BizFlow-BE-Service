-- =============================================
-- Migration 065: Sync BusinessTypeTaxes from active ruleset mappings
-- Purpose:
-- 1) Sync legacy BusinessTypeTaxes from active ruleset rates.
-- 2) Close outdated open-ended rows to keep history consistent.
-- =============================================

-- Aiven: disable primary key requirement for temp tables (session-scoped)
SET SESSION sql_require_primary_key = 0;

-- Active ruleset and effective date baseline
SET @activeRulesetId := (
    SELECT RulesetId
    FROM TaxRulesets
    WHERE IsActive = TRUE
    ORDER BY EffectiveFrom DESC, RulesetId DESC
    LIMIT 1
);

SET @effectiveFrom := COALESCE(
    (SELECT EffectiveFrom FROM TaxRulesets WHERE RulesetId = @activeRulesetId),
    CURRENT_DATE()
);

-- Distinct business types that are actually used by non-deleted products
CREATE TEMPORARY TABLE tmp_used_business_types AS
SELECT DISTINCT p.BusinessTypeId
FROM Products p
WHERE p.DeletedAt IS NULL;

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
CREATE TEMPORARY TABLE tmp_reconciled_rates AS
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

-- Prepare BusinessTypeTaxes sync payload
CREATE TEMPORARY TABLE tmp_business_type_tax_target AS
SELECT
    rr.TargetBusinessTypeId AS BusinessTypeId,
    CAST((CASE WHEN rr.TaxType = 'PIT_METHOD_1' THEN 'PIT' ELSE rr.TaxType END) AS CHAR CHARACTER SET utf8mb4) COLLATE utf8mb4_unicode_ci AS TaxType,
    ROUND(rr.TaxRate * 100, 2) AS TaxRatePercent,
    CAST((CASE WHEN rr.TaxType = 'PIT_METHOD_1' THEN 'revenue' ELSE 'price' END) AS CHAR CHARACTER SET utf8mb4) COLLATE utf8mb4_unicode_ci AS CalculationBase
FROM tmp_reconciled_rates rr;

-- Close open rows with outdated rates
UPDATE BusinessTypeTaxes btt
JOIN tmp_business_type_tax_target t
    ON t.BusinessTypeId = btt.BusinessTypeId
   AND t.TaxType = btt.TaxType
SET btt.EffectiveTo = DATE_SUB(@effectiveFrom, INTERVAL 1 DAY)
WHERE btt.EffectiveTo IS NULL
  AND btt.EffectiveFrom < @effectiveFrom
  AND ABS(btt.TaxRate - t.TaxRatePercent) > 0.001;

-- Insert missing BusinessTypeTaxes rows at active ruleset effective date
INSERT INTO BusinessTypeTaxes
(
    BusinessTypeTaxId,
    BusinessTypeId,
    TaxType,
    TaxRate,
    CalculationBase,
    EffectiveFrom,
    EffectiveTo,
    CreatedBy,
    CreatedAt
)
SELECT
    UUID(),
    t.BusinessTypeId,
    t.TaxType,
    t.TaxRatePercent,
    t.CalculationBase,
    @effectiveFrom,
    NULL,
    NULL,
    NOW()
FROM tmp_business_type_tax_target t
WHERE NOT EXISTS (
    SELECT 1
    FROM BusinessTypeTaxes btt
    WHERE btt.BusinessTypeId = t.BusinessTypeId
      AND btt.TaxType = t.TaxType
      AND btt.EffectiveFrom = @effectiveFrom
      AND ABS(btt.TaxRate - t.TaxRatePercent) <= 0.001
);

-- Cleanup temp tables
DROP TEMPORARY TABLE IF EXISTS tmp_business_type_tax_target;
DROP TEMPORARY TABLE IF EXISTS tmp_reconciled_rates;
DROP TEMPORARY TABLE IF EXISTS tmp_business_type_mapping;
DROP TEMPORARY TABLE IF EXISTS tmp_used_business_types;

-- Restore primary key requirement
SET SESSION sql_require_primary_key = 1;

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('065_sync_business_type_taxes_from_ruleset', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
