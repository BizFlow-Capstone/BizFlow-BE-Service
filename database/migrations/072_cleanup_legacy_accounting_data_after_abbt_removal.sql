-- =============================================
-- Migration 072: Cleanup legacy accounting data after ABBT removal
-- Purpose:
-- 1) Ensure old table AccountingBookBusinessTypes is removed.
-- 2) Deduplicate legacy AccountingBooks by new key (Location + Period + TemplateVersion).
-- 3) Merge/move dependent data (exports, formula results, tax overrides) to kept books.
-- 4) Cleanup orphan records and enforce unique index for the new flow.
-- =============================================

SET SQL_SAFE_UPDATES = 0;

-- 0) Safety: drop legacy table if it still exists
DROP TABLE IF EXISTS AccountingBookBusinessTypes;

-- 1) Build duplicate book map: keep newest BookId for each (Location, Period, TemplateVersion)
DROP TEMPORARY TABLE IF EXISTS tmp_book_keep_map;
CREATE TEMPORARY TABLE tmp_book_keep_map (
    OldBookId BIGINT NOT NULL,
    KeepBookId BIGINT NOT NULL,
    PRIMARY KEY (OldBookId),
    KEY idx_tmp_book_keep_keep (KeepBookId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO tmp_book_keep_map (OldBookId, KeepBookId)
SELECT
    b.BookId AS OldBookId,
    d.KeepBookId
FROM AccountingBooks b
JOIN (
    SELECT
        BusinessLocationId,
        PeriodId,
        TemplateVersionId,
        MAX(BookId) AS KeepBookId,
        COUNT(*) AS Cnt
    FROM AccountingBooks
    GROUP BY BusinessLocationId, PeriodId, TemplateVersionId
    HAVING COUNT(*) > 1
) d
    ON d.BusinessLocationId = b.BusinessLocationId
   AND d.PeriodId = b.PeriodId
   AND d.TemplateVersionId = b.TemplateVersionId
WHERE b.BookId <> d.KeepBookId;

-- 2) Re-link exports from old books to kept books
UPDATE AccountingExports e
JOIN tmp_book_keep_map m
    ON m.OldBookId = e.BookId
SET e.BookId = m.KeepBookId;

-- 3) Merge formula results from old books into kept books
-- Keep the freshest ComputedAt when collision happens.
INSERT INTO FormulaResults
(
    BookId,
    FormulaId,
    ProductId,
    BusinessTypeId,
    SectionCode,
    ResultValue,
    ComputedAt,
    IsStale
)
SELECT
    m.KeepBookId,
    fr.FormulaId,
    fr.ProductId,
    fr.BusinessTypeId,
    fr.SectionCode,
    fr.ResultValue,
    fr.ComputedAt,
    fr.IsStale
FROM FormulaResults fr
JOIN tmp_book_keep_map m
    ON m.OldBookId = fr.BookId
ON DUPLICATE KEY UPDATE
    ResultValue = IF(VALUES(ComputedAt) >= FormulaResults.ComputedAt, VALUES(ResultValue), FormulaResults.ResultValue),
    ComputedAt = GREATEST(FormulaResults.ComputedAt, VALUES(ComputedAt)),
    IsStale = (FormulaResults.IsStale OR VALUES(IsStale));

DELETE fr
FROM FormulaResults fr
JOIN tmp_book_keep_map m
    ON m.OldBookId = fr.BookId;

-- 4) Merge tax overrides from old books into kept books
-- Keep latest UpdatedAt when collision happens.
INSERT INTO AccountingBookTaxOverrides
(
    BookId,
    BusinessTypeId,
    VatRate,
    PitRate,
    Note,
    UpdatedByUserId,
    UpdatedAt
)
SELECT
    m.KeepBookId,
    o.BusinessTypeId,
    o.VatRate,
    o.PitRate,
    o.Note,
    o.UpdatedByUserId,
    o.UpdatedAt
FROM AccountingBookTaxOverrides o
JOIN tmp_book_keep_map m
    ON m.OldBookId = o.BookId
ON DUPLICATE KEY UPDATE
    VatRate = IF(VALUES(UpdatedAt) >= AccountingBookTaxOverrides.UpdatedAt, VALUES(VatRate), AccountingBookTaxOverrides.VatRate),
    PitRate = IF(VALUES(UpdatedAt) >= AccountingBookTaxOverrides.UpdatedAt, VALUES(PitRate), AccountingBookTaxOverrides.PitRate),
    Note = IF(VALUES(UpdatedAt) >= AccountingBookTaxOverrides.UpdatedAt, VALUES(Note), AccountingBookTaxOverrides.Note),
    UpdatedByUserId = IF(VALUES(UpdatedAt) >= AccountingBookTaxOverrides.UpdatedAt, VALUES(UpdatedByUserId), AccountingBookTaxOverrides.UpdatedByUserId),
    UpdatedAt = GREATEST(AccountingBookTaxOverrides.UpdatedAt, VALUES(UpdatedAt));

DELETE o
FROM AccountingBookTaxOverrides o
JOIN tmp_book_keep_map m
    ON m.OldBookId = o.BookId;

-- 5) Delete duplicate books after moving dependent data
DELETE b
FROM AccountingBooks b
JOIN tmp_book_keep_map m
    ON m.OldBookId = b.BookId;

-- 6) Orphan cleanup (safe no-op if FK already keeps integrity)
DELETE e
FROM AccountingExports e
LEFT JOIN AccountingBooks b
    ON b.BookId = e.BookId
WHERE b.BookId IS NULL;

DELETE fr
FROM FormulaResults fr
LEFT JOIN AccountingBooks b
    ON b.BookId = fr.BookId
WHERE b.BookId IS NULL;

DELETE fr
FROM FormulaResults fr
LEFT JOIN FormulaDefinitions fd
    ON fd.FormulaId = fr.FormulaId
WHERE fd.FormulaId IS NULL;

DELETE o
FROM AccountingBookTaxOverrides o
LEFT JOIN AccountingBooks b
    ON b.BookId = o.BookId
WHERE b.BookId IS NULL;

DELETE o
FROM AccountingBookTaxOverrides o
LEFT JOIN BusinessTypes bt
    ON bt.BusinessTypeId = o.BusinessTypeId
WHERE bt.BusinessTypeId IS NULL;

-- 7) Enforce unique index for current flow if missing
SET @has_uq_book := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'AccountingBooks'
      AND INDEX_NAME = 'uq_book_location_period_template_version'
);

SET @sql_add_uq_book := IF(
    @has_uq_book = 0,
    'ALTER TABLE AccountingBooks ADD UNIQUE INDEX uq_book_location_period_template_version (BusinessLocationId, PeriodId, TemplateVersionId)',
    'SELECT 1'
);

PREPARE stmt_add_uq_book FROM @sql_add_uq_book;
EXECUTE stmt_add_uq_book;
DEALLOCATE PREPARE stmt_add_uq_book;

-- 8) Cleanup temp table
DROP TEMPORARY TABLE IF EXISTS tmp_book_keep_map;

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('072_cleanup_legacy_accounting_data_after_abbt_removal', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
