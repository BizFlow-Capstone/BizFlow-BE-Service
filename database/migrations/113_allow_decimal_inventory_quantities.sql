-- =============================================
-- Migration  : 113_allow_decimal_inventory_quantities
-- Description: Allow decimal stock and import/movement quantities
-- Date       : 2026-04-16
-- =============================================

ALTER TABLE Products
    MODIFY COLUMN Stock DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Quantity in stock';

SET @has_product_imports = (
    SELECT COUNT(*)
    FROM information_schema.tables
    WHERE table_schema = DATABASE()
      AND table_name = 'ProductImports'
);

SET @has_products_imports = (
    SELECT COUNT(*)
    FROM information_schema.tables
    WHERE table_schema = DATABASE()
      AND table_name = 'ProductsImports'
);

SET @alter_imports_sql = IF(
    @has_product_imports > 0,
    'ALTER TABLE ProductImports MODIFY COLUMN Quantity DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT ''Import quantity''',
    IF(
        @has_products_imports > 0,
        'ALTER TABLE ProductsImports MODIFY COLUMN Quantity DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT ''Import quantity''',
        'SELECT ''Skip: neither ProductImports nor ProductsImports exists'' AS Info'
    )
);
PREPARE stmt_alter_imports FROM @alter_imports_sql;
EXECUTE stmt_alter_imports;
DEALLOCATE PREPARE stmt_alter_imports;

ALTER TABLE StockMovements
    MODIFY COLUMN Quantity DECIMAL(15,2) NOT NULL COMMENT 'Quantity moved (positive for IN, negative for OUT)',
    MODIFY COLUMN BalanceAfter DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Stock balance after this movement';

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('113_allow_decimal_inventory_quantities', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
