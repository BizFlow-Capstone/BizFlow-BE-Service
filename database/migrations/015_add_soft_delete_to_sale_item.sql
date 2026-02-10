-- Migration: Add soft delete to SaleItems table
-- Author: System
-- Date: 2026-02-09
-- Description: Add DeletedAt column to SaleItems table for soft deletion instead of physical removal

-- Add DeletedAt column
ALTER TABLE SaleItems
ADD COLUMN DeletedAt DATETIME NULL;

-- Add index for query performance
CREATE INDEX idx_saleitems_deletedat ON SaleItems(DeletedAt);

-- Insert migration record
INSERT INTO __MigrationHistory (MigrationId, ProductVersion) 
VALUES ('015_add_soft_delete_to_sale_item', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
