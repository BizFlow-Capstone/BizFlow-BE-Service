-- Migration: 039_add_cascade_delete_to_sale_items
-- Description: Updates the foreign key constraints from Product and SaleItem to all their dependents to use ON DELETE CASCADE.
-- This ensures that when a Product is hard-deleted, all its dependent entities are automatically deleted by the database.

-- 1. Update SaleItem -> Product constraint
ALTER TABLE `SaleItems` DROP FOREIGN KEY `fk_sale_item_product`;
ALTER TABLE `SaleItems`
ADD CONSTRAINT `fk_sale_item_product`
FOREIGN KEY (`ProductId`) REFERENCES `Products` (`ProductId`) ON DELETE CASCADE;

-- 2. Update ProductPricePolicy -> SaleItem constraint 
ALTER TABLE `ProductPricePolicies` DROP FOREIGN KEY `fk_product_price_policy_sale_item`;
ALTER TABLE `ProductPricePolicies`
ADD CONSTRAINT `fk_product_price_policy_sale_item`
FOREIGN KEY (`SaleItemId`) REFERENCES `SaleItems` (`SaleItemId`) ON DELETE CASCADE;

-- 3. Update StockMovement -> Product constraint
ALTER TABLE `StockMovements` DROP FOREIGN KEY `fk_stock_movement_product`;
ALTER TABLE `StockMovements`
ADD CONSTRAINT `fk_stock_movement_product`
FOREIGN KEY (`ProductId`) REFERENCES `Products` (`ProductId`) ON DELETE CASCADE;

-- 4. Update ProductImport -> Product constraint
ALTER TABLE `ProductsImports` DROP FOREIGN KEY `fk_product_import_new_product`;
ALTER TABLE `ProductsImports`
ADD CONSTRAINT `fk_product_import_new_product`
FOREIGN KEY (`ProductId`) REFERENCES `Products` (`ProductId`) ON DELETE CASCADE;

-- 5. Update OrderDetail -> SaleItem constraint
ALTER TABLE `OrderDetails` DROP FOREIGN KEY `fk_order_detail_sale_item`;
ALTER TABLE `OrderDetails`
ADD CONSTRAINT `fk_order_detail_sale_item`
FOREIGN KEY (`SaleItemId`) REFERENCES `SaleItems` (`SaleItemId`) ON DELETE CASCADE;

