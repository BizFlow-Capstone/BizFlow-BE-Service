-- Migration: 030_add_selling_price_to_products
-- Description: Add SellingPrice column to Products table.
--   Per product-flow.md, SellingPrice (giá bán) is separate from CostPrice (giá vốn).
--   CostPrice is auto-updated from the latest confirmed import.
--   SellingPrice is user-entered selling price per base unit.
--   Default SaleItem price should use SellingPrice, not CostPrice.
-- Date: 2026-03-12

-- =============================================
-- 1. Add SellingPrice column (after CostPrice)
-- =============================================
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'SellingPrice');
SET @sql = IF(@col_exists = 0,
    'ALTER TABLE Products ADD COLUMN SellingPrice DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT ''Giá bán theo base unit'' AFTER CostPrice',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- =============================================
-- 2. Initialize SellingPrice from default SaleItem price
--    For existing products, copy the current default SaleItem price → SellingPrice
-- =============================================
UPDATE Products p
    JOIN SaleItems si ON si.ProductId = p.ProductId
        AND si.Unit = p.Unit
        AND si.Quantity = 1
        AND si.DeletedAt IS NULL
    JOIN ProductPricePolicies pp ON pp.SaleItemId = si.SaleItemId
        AND pp.IsDefault = TRUE
SET p.SellingPrice = pp.Price
WHERE p.SellingPrice = 0;

-- =============================================
-- Track migration
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('031_add_selling_price_to_products', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '1.0.0';
