-- Migration 067: Shift sample data of location 6 from year 2025 to 2026
-- This migration is for environments where 063 was already executed before date alignment.

-- Align session collation with table defaults (utf8mb4_unicode_ci) to avoid ERROR 1267
-- when comparing CHAR/VARCHAR columns to literals or user variables (MySQL 8 default is utf8mb4_0900_ai_ci).
SET NAMES utf8mb4 COLLATE utf8mb4_unicode_ci;

SET @createdBy = 'ff45309c-7b0b-4012-933b-042405d75685';

-- 1) Revenues: move 2025 sample rows to 2026
UPDATE Revenues
SET RevenueDate = DATE_ADD(RevenueDate, INTERVAL 1 YEAR)
WHERE BusinessLocationId = 6
  AND RevenueDate BETWEEN '2025-01-01' AND '2025-12-31'
  AND CreatedBy = @createdBy;

-- 2) Costs: move 2025 sample rows to 2026
UPDATE Costs
SET CostDate = DATE_ADD(CostDate, INTERVAL 1 YEAR)
WHERE BusinessLocationId = 6
  AND CostDate BETWEEN '2025-01-01' AND '2025-12-31'
  AND CreatedBy = @createdBy;

-- 3) General ledger sample rows: move manual rows to 2026
UPDATE GeneralLedgerEntries
SET EntryDate = DATE_ADD(EntryDate, INTERVAL 1 YEAR)
WHERE BusinessLocationId = 6
  AND EntryDate BETWEEN '2025-01-01' AND '2025-12-31'
  AND ReferenceId IS NULL
  AND ReferenceType IN ('revenue', 'cost')
  AND TransactionType IN ('manual_revenue', 'manual_cost');

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('067_shift_location6_sample_data_2025_to_2026', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
