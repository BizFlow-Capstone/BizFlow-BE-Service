-- Migration 068: Fix revenue/cost sample date shift for location 6
-- 067 used a CreatedBy filter that may not match seeded rows in some environments.

SET NAMES utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 1) Revenues: shift manual/sale sample rows in 2025 to 2026
UPDATE Revenues
SET RevenueDate = DATE_ADD(RevenueDate, INTERVAL 1 YEAR)
WHERE BusinessLocationId = 6
  AND RevenueDate BETWEEN '2025-01-01' AND '2025-12-31'
  AND RevenueType IN ('sale', 'manual');

-- 2) Costs: shift sample cost rows in 2025 to 2026
UPDATE Costs
SET CostDate = DATE_ADD(CostDate, INTERVAL 1 YEAR)
WHERE BusinessLocationId = 6
  AND CostDate BETWEEN '2025-01-01' AND '2025-12-31'
  AND CostType IN ('manual', 'utilities', 'transport', 'maintenance', 'marketing', 'salary');

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('068_shift_location6_sample_revenue_cost_2025_to_2026_fix', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
