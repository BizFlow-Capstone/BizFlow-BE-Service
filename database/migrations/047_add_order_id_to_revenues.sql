-- =============================================
-- Migration  : 047_add_order_id_to_revenues
-- Description: Add OrderId soft reference to Revenues table
-- Date       : 2026-03-21
-- =============================================

ALTER TABLE Revenues
ADD COLUMN OrderId BIGINT NULL COMMENT 'Soft reference to Order, nullable because some revenues are manual'
AFTER BusinessLocationId;
-- Note: Intentionally NOT adding a FOREIGN KEY constraint to decouple Accounting from Sales module.

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('047_add_order_id_to_revenues', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
