-- Migration: 039_add_cascade_delete_to_sale_items
-- Description: Updates the foreign key constraints from Product to SaleItem and SaleItem to ProductPricePolicy to use ON DELETE CASCADE.
-- This ensures that when a Product is hard-deleted, all its SaleItems and their ProductPricePolicies are automatically deleted by the database.

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
