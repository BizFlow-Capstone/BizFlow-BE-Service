-- =============================================
-- Migration  : 119_allow_decimal_quantities_for_sale_items_and_order_details
-- Description: Convert SaleItems.Quantity and OrderDetails.Quantity to DECIMAL(15,2)
--              to support fractional sale units and keep quantity types consistent
--              with decimal inventory.
-- Date       : 2026-04-23
-- =============================================

-- 1) SaleItems / SaleItem : Quantity -> DECIMAL(15,2)
SET @has_sale_items = (
    SELECT COUNT(*)
    FROM information_schema.tables
    WHERE table_schema = DATABASE()
      AND table_name = 'SaleItems'
);

SET @has_sale_item = (
    SELECT COUNT(*)
    FROM information_schema.tables
    WHERE table_schema = DATABASE()
      AND table_name = 'SaleItem'
);

SET @alter_sale_item_sql = IF(
    @has_sale_items > 0,
    'ALTER TABLE SaleItems MODIFY COLUMN Quantity DECIMAL(15,2) NOT NULL DEFAULT 1 COMMENT ''Quantity per sale unit''',
    IF(
        @has_sale_item > 0,
        'ALTER TABLE SaleItem MODIFY COLUMN Quantity DECIMAL(15,2) NOT NULL DEFAULT 1 COMMENT ''Quantity per sale unit''',
        'SELECT ''Skip: neither SaleItems nor SaleItem exists'' AS Info'
    )
);
PREPARE stmt_alter_sale_item FROM @alter_sale_item_sql;
EXECUTE stmt_alter_sale_item;
DEALLOCATE PREPARE stmt_alter_sale_item;

-- 2) OrderDetails / OrderDetail : Quantity -> DECIMAL(15,2)
SET @has_order_details = (
    SELECT COUNT(*)
    FROM information_schema.tables
    WHERE table_schema = DATABASE()
      AND table_name = 'OrderDetails'
);

SET @has_order_detail = (
    SELECT COUNT(*)
    FROM information_schema.tables
    WHERE table_schema = DATABASE()
      AND table_name = 'OrderDetail'
);

SET @alter_order_detail_sql = IF(
    @has_order_details > 0,
    'ALTER TABLE OrderDetails MODIFY COLUMN Quantity DECIMAL(15,2) NOT NULL DEFAULT 1 COMMENT ''Quantity sold''',
    IF(
        @has_order_detail > 0,
        'ALTER TABLE OrderDetail MODIFY COLUMN Quantity DECIMAL(15,2) NOT NULL DEFAULT 1 COMMENT ''Quantity sold''',
        'SELECT ''Skip: neither OrderDetails nor OrderDetail exists'' AS Info'
    )
);
PREPARE stmt_alter_order_detail FROM @alter_order_detail_sql;
EXECUTE stmt_alter_order_detail;
DEALLOCATE PREPARE stmt_alter_order_detail;

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('119_allow_decimal_quantities_for_sale_items_and_order_details', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
