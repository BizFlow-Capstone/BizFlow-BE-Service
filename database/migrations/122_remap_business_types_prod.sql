-- ================================================================
-- Migration: 122_remap_business_types_prod
-- Description: Remap 6 legacy BusinessTypes to 10 canonical TT152 types
--              (4 main groups + 6 subgroups with different tax rates).
--              Preserves ALL data (Products, Revenues, Costs, etc.) via FK remap.
--              Archives old data for rollback safety.
-- Date: 2026-05-03
-- WARNING: RUN ON PROD ONLY DURING MAINTENANCE WINDOW!
-- ================================================================

-- NOTE: sql_require_primary_key requires elevated privileges; do not set it here.

-- ════════════════════════════════════════════════════════════════
-- STEP 1: Archive current BusinessTypes and IndustryTaxRates
-- ════════════════════════════════════════════════════════════════

CREATE TABLE IF NOT EXISTS BusinessTypes_PreRemap_Archive (
    BusinessTypeId CHAR(36)    NOT NULL,
    Code           VARCHAR(50) NOT NULL,
    Name           VARCHAR(255) NOT NULL,
    Description    TEXT NULL,
    Status         VARCHAR(20) NOT NULL,
    CreatedBy      CHAR(36)    NULL,
    ModifiedBy     CHAR(36)    NULL,
    CreatedAt      DATETIME    NOT NULL,
    LastModifiedAt DATETIME    NOT NULL,
    ArchivedAt     DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (BusinessTypeId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Archive of BusinessTypes before remap migration 122';

INSERT IGNORE INTO BusinessTypes_PreRemap_Archive
    (BusinessTypeId, Code, Name, Description, Status, CreatedBy, ModifiedBy, CreatedAt, LastModifiedAt)
SELECT BusinessTypeId, Code, Name, Description, Status, CreatedBy, ModifiedBy, CreatedAt, LastModifiedAt
FROM BusinessTypes;

CREATE TABLE IF NOT EXISTS IndustryTaxRates_PreRemap_Archive (
    RateId         INT         NOT NULL,
    RulesetId      INT         NOT NULL,
    BusinessTypeId CHAR(36)    NOT NULL,
    TaxType        VARCHAR(20) NOT NULL,
    TaxRate        DECIMAL(5,4) NOT NULL,
    Description    TEXT        NULL,
    ArchivedAt     DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (RateId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Archive of IndustryTaxRates before remap migration 122';

INSERT IGNORE INTO IndustryTaxRates_PreRemap_Archive
    (RateId, RulesetId, BusinessTypeId, TaxType, TaxRate, Description)
SELECT RateId, RulesetId, BusinessTypeId, TaxType, TaxRate, Description
FROM IndustryTaxRates;


-- ════════════════════════════════════════════════════════════════
-- STEP 2: Update existing & Insert new 10 canonical Business Types
-- ════════════════════════════════════════════════════════════════

-- Update names for existing 3 main groups
UPDATE BusinessTypes SET Name = 'Phân phối, cung cấp hàng hóa'
WHERE BusinessTypeId = '11111111-1111-1111-1111-111111111001';

UPDATE BusinessTypes SET Name = 'Dịch vụ, xây dựng không bao thầu NVL'
WHERE BusinessTypeId = '11111111-1111-1111-1111-111111111002';

UPDATE BusinessTypes SET Name = 'Sản xuất, vận tải, DV gắn hàng hóa; xây dựng bao thầu NVL'
WHERE BusinessTypeId = '11111111-1111-1111-1111-111111111003';

-- Insert the 7 new Business Types
INSERT IGNORE INTO BusinessTypes (BusinessTypeId, Code, Name, Description, Status, CreatedAt, LastModifiedAt) VALUES
-- Main Group 4
('11111111-1111-1111-1111-111111111010', 'bt-other', 'Hoạt động kinh doanh khác', 'Nhóm 4', 'active', NOW(), NOW()),
-- Subgroup 1
('11111111-1111-1111-1111-111111111011', 'bt-retail-sub1', 'Nhóm 1: 102–106 (thưởng/hỗ trợ/bồi thường, hàng hóa 0%/không chịu GTGT)', 'Nhóm con có mức thuế khác', 'active', NOW(), NOW()),
-- Subgroup 2
('11111111-1111-1111-1111-111111111012', 'bt-service-sub1', 'Nhóm 2: 214–216 (không chịu GTGT/0%, hợp tác, bồi thường)', 'Nhóm con có mức thuế khác', 'active', NOW(), NOW()),
('11111111-1111-1111-1111-111111111013', 'bt-service-sub2', 'Nhóm 2: 217 (cho thuê tài sản)', 'Nhóm con có mức thuế khác', 'active', NOW(), NOW()),
('11111111-1111-1111-1111-111111111014', 'bt-service-sub3', 'Nhóm 2: 218–219 (đại lý xổ số/bảo hiểm/đa cấp, bồi thường)', 'Nhóm con có mức thuế khác', 'active', NOW(), NOW()),
-- Subgroup 3
('11111111-1111-1111-1111-111111111015', 'bt-fnb-sub1', 'Nhóm 3: 309–310 (không chịu GTGT/0%, hợp tác)', 'Nhóm con có mức thuế khác', 'active', NOW(), NOW()),
-- Subgroup 4
('11111111-1111-1111-1111-111111111016', 'bt-other-sub1', 'Nhóm 4: 402–403 (dịch vụ/khác thuộc VAT 5% nhưng PIT 1%)', 'Nhóm con có mức thuế khác', 'active', NOW(), NOW());


-- ════════════════════════════════════════════════════════════════
-- STEP 3: Build mapping table old_id → new_id
-- ════════════════════════════════════════════════════════════════

DROP TEMPORARY TABLE IF EXISTS tmp_bt_remap;
CREATE TEMPORARY TABLE tmp_bt_remap (
    OldId CHAR(36) NOT NULL,
    NewId CHAR(36) NOT NULL,
    PRIMARY KEY (OldId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Map legacy and outdated types to the canonical main groups
INSERT INTO tmp_bt_remap (OldId, NewId) VALUES
-- Old bt-transport -> Group 3
('11111111-1111-1111-1111-111111111004', '11111111-1111-1111-1111-111111111003'),
-- RETAIL → Group 1
('650e8400-e29b-41d4-a716-446655440001', '11111111-1111-1111-1111-111111111001'),
-- RESTAURANT → Group 3
('650e8400-e29b-41d4-a716-446655440002', '11111111-1111-1111-1111-111111111003'),
-- GROCERY → Group 1
('650e8400-e29b-41d4-a716-446655440003', '11111111-1111-1111-1111-111111111001'),
-- BEAUTY → Group 2
('650e8400-e29b-41d4-a716-446655440004', '11111111-1111-1111-1111-111111111002'),
-- HKD_RETAIL → Group 1
('87701717-9df1-4300-9386-a292c48a1cac', '11111111-1111-1111-1111-111111111001');


-- ════════════════════════════════════════════════════════════════
-- STEP 4: Remap FK columns in all dependent tables
-- ════════════════════════════════════════════════════════════════

-- 4a. Products
UPDATE Products p
JOIN   tmp_bt_remap m ON m.OldId = p.BusinessTypeId
SET    p.BusinessTypeId = m.NewId;

-- 4b. Revenues
UPDATE Revenues r
JOIN   tmp_bt_remap m ON m.OldId = r.BusinessTypeId
SET    r.BusinessTypeId = m.NewId
WHERE  r.BusinessTypeId IS NOT NULL;

-- 4c. Costs
UPDATE Costs c
JOIN   tmp_bt_remap m ON m.OldId = c.BusinessTypeId
SET    c.BusinessTypeId = m.NewId
WHERE  c.BusinessTypeId IS NOT NULL;

-- 4d. AccountingBookTaxOverrides
UPDATE AccountingBookTaxOverrides ato
JOIN   tmp_bt_remap m ON m.OldId = ato.BusinessTypeId
SET    ato.BusinessTypeId = m.NewId;

-- 4e. FormulaResults
UPDATE FormulaResults fr
JOIN   tmp_bt_remap m ON m.OldId = fr.BusinessTypeId
SET    fr.BusinessTypeId = m.NewId
WHERE  fr.BusinessTypeId != '';


-- ════════════════════════════════════════════════════════════════
-- STEP 5: Clear and Insert IndustryTaxRates for all 10 canonical types
-- ════════════════════════════════════════════════════════════════

-- 5a. Delete all IndustryTaxRates for Ruleset 1 to start fresh
DELETE FROM IndustryTaxRates
WHERE RulesetId = 1;

-- 5b. Insert exact rates for 4 main groups + 6 subgroups
INSERT INTO IndustryTaxRates (RulesetId, BusinessTypeId, TaxType, TaxRate, Description) VALUES
-- 1. Phân phối, cung cấp hàng hóa
(1, '11111111-1111-1111-1111-111111111001', 'VAT', 0.0100, 'Nhóm 1: GTGT 1%'),
(1, '11111111-1111-1111-1111-111111111001', 'PIT_METHOD_1', 0.0050, 'Nhóm 1: TNCN 0.5%'),

-- 2. Dịch vụ, xây dựng không bao thầu NVL
(1, '11111111-1111-1111-1111-111111111002', 'VAT', 0.0500, 'Nhóm 2: GTGT 5%'),
(1, '11111111-1111-1111-1111-111111111002', 'PIT_METHOD_1', 0.0200, 'Nhóm 2: TNCN 2%'),

-- 3. Sản xuất, vận tải, DV gắn hàng hóa; xây dựng bao thầu NVL
(1, '11111111-1111-1111-1111-111111111003', 'VAT', 0.0300, 'Nhóm 3: GTGT 3%'),
(1, '11111111-1111-1111-1111-111111111003', 'PIT_METHOD_1', 0.0150, 'Nhóm 3: TNCN 1.5%'),

-- 4. Hoạt động kinh doanh khác
(1, '11111111-1111-1111-1111-111111111010', 'VAT', 0.0200, 'Nhóm 4: GTGT 2%'),
(1, '11111111-1111-1111-1111-111111111010', 'PIT_METHOD_1', 0.0100, 'Nhóm 4: TNCN 1%'),

-- Sub 1: 102–106 (thưởng/hỗ trợ/bồi thường, hàng hóa 0%/không chịu GTGT)
(1, '11111111-1111-1111-1111-111111111011', 'VAT', 0.0000, 'Nhóm 1 con (102-106): GTGT 0%'),
(1, '11111111-1111-1111-1111-111111111011', 'PIT_METHOD_1', 0.0050, 'Nhóm 1 con (102-106): TNCN 0.5%'),

-- Sub 2: 214–216 (không chịu GTGT/0%, hợp tác, bồi thường)
(1, '11111111-1111-1111-1111-111111111012', 'VAT', 0.0000, 'Nhóm 2 con (214-216): GTGT 0%'),
(1, '11111111-1111-1111-1111-111111111012', 'PIT_METHOD_1', 0.0200, 'Nhóm 2 con (214-216): TNCN 2%'),

-- Sub 3: 217 (cho thuê tài sản)
(1, '11111111-1111-1111-1111-111111111013', 'VAT', 0.0500, 'Nhóm 2 con (217): GTGT 5%'),
(1, '11111111-1111-1111-1111-111111111013', 'PIT_METHOD_1', 0.0500, 'Nhóm 2 con (217): TNCN 5%'),

-- Sub 4: 218–219 (đại lý xổ số/bảo hiểm/đa cấp, bồi thường)
(1, '11111111-1111-1111-1111-111111111014', 'VAT', 0.0000, 'Nhóm 2 con (218-219): GTGT 0%'),
(1, '11111111-1111-1111-1111-111111111014', 'PIT_METHOD_1', 0.0500, 'Nhóm 2 con (218-219): TNCN 5%'),

-- Sub 5: 309–310 (không chịu GTGT/0%, hợp tác)
(1, '11111111-1111-1111-1111-111111111015', 'VAT', 0.0000, 'Nhóm 3 con (309-310): GTGT 0%'),
(1, '11111111-1111-1111-1111-111111111015', 'PIT_METHOD_1', 0.0150, 'Nhóm 3 con (309-310): TNCN 1.5%'),

-- Sub 6: 402–403 (dịch vụ/khác thuộc VAT 5% nhưng PIT 1%)
(1, '11111111-1111-1111-1111-111111111016', 'VAT', 0.0000, 'Nhóm 4 con (402-403): GTGT 0% (nhưng thuộc VAT 5%)'),
(1, '11111111-1111-1111-1111-111111111016', 'PIT_METHOD_1', 0.0100, 'Nhóm 4 con (402-403): TNCN 1%');

-- 5c. Delete old rates in other Rulesets (like Ruleset 2)
DELETE itr FROM IndustryTaxRates itr
JOIN tmp_bt_remap m ON m.OldId = itr.BusinessTypeId;


-- ════════════════════════════════════════════════════════════════
-- STEP 6: Delete old BusinessTypes (only after all FKs remapped)
-- ════════════════════════════════════════════════════════════════

DELETE bt
FROM BusinessTypes bt
JOIN tmp_bt_remap m ON m.OldId = bt.BusinessTypeId;


-- ════════════════════════════════════════════════════════════════
-- STEP 7: Cleanup
-- ════════════════════════════════════════════════════════════════

DROP TEMPORARY TABLE IF EXISTS tmp_bt_remap;


-- ════════════════════════════════════════════════════════════════
-- Track migration
-- ════════════════════════════════════════════════════════════════
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('122_remap_business_types_prod', '1.0.0');
