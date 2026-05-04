-- =============================================
-- Migration  : 121_add_revenue_cost_reversal_columns
-- Description: Append-only reversal metadata on Revenues and Costs
--              (IsReversal, self-FK to original row) aligned with GeneralLedgerEntry.
-- Date       : 2026-05-04
-- =============================================

-- ── Revenues ─────────────────────────────────

SET @has_rev_is_reversal = (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Revenues' AND COLUMN_NAME = 'IsReversal'
);
SET @sql = IF(@has_rev_is_reversal = 0,
    'ALTER TABLE Revenues ADD COLUMN IsReversal TINYINT(1) NOT NULL DEFAULT 0 COMMENT ''TRUE when this row is a reversal entry'' AFTER DocumentNumberNormalized, ADD COLUMN ReversedRevenueId BIGINT NULL COMMENT ''Original RevenueId this reversal offsets'' AFTER IsReversal',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx_revenue_reversed = (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Revenues' AND INDEX_NAME = 'idx_revenue_reversed'
);
SET @sql = IF(@idx_revenue_reversed = 0,
    'CREATE INDEX idx_revenue_reversed ON Revenues (ReversedRevenueId)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @fk_revenue_reversed = (
    SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
    WHERE CONSTRAINT_SCHEMA = DATABASE() AND TABLE_NAME = 'Revenues'
      AND CONSTRAINT_NAME = 'fk_revenue_reversed_revenue' AND CONSTRAINT_TYPE = 'FOREIGN KEY'
);
SET @sql = IF(@fk_revenue_reversed = 0,
    'ALTER TABLE Revenues ADD CONSTRAINT fk_revenue_reversed_revenue FOREIGN KEY (ReversedRevenueId) REFERENCES Revenues (RevenueId) ON DELETE RESTRICT ON UPDATE CASCADE',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- ── Costs ────────────────────────────────────

SET @has_cost_is_reversal = (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Costs' AND COLUMN_NAME = 'IsReversal'
);
SET @sql = IF(@has_cost_is_reversal = 0,
    'ALTER TABLE Costs ADD COLUMN IsReversal TINYINT(1) NOT NULL DEFAULT 0 COMMENT ''TRUE when this row is a reversal entry'' AFTER DocumentNumberNormalized, ADD COLUMN ReversedCostId BIGINT NULL COMMENT ''Original CostId this reversal offsets'' AFTER IsReversal',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx_cost_reversed = (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Costs' AND INDEX_NAME = 'idx_cost_reversed'
);
SET @sql = IF(@idx_cost_reversed = 0,
    'CREATE INDEX idx_cost_reversed ON Costs (ReversedCostId)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @fk_cost_reversed = (
    SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
    WHERE CONSTRAINT_SCHEMA = DATABASE() AND TABLE_NAME = 'Costs'
      AND CONSTRAINT_NAME = 'fk_cost_reversed_cost' AND CONSTRAINT_TYPE = 'FOREIGN KEY'
);
SET @sql = IF(@fk_cost_reversed = 0,
    'ALTER TABLE Costs ADD CONSTRAINT fk_cost_reversed_cost FOREIGN KEY (ReversedCostId) REFERENCES Costs (CostId) ON DELETE RESTRICT ON UPDATE CASCADE',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('121_add_revenue_cost_reversal_columns', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
