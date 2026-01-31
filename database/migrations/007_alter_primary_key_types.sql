-- Migration: 007_alter_primary_key_types
-- Description: Change BusinessLocation and UserLocationAssignment primary keys from UUID to INT
-- Date: 2026-01-31

-- =============================================
-- ALTER BUSINESS LOCATION TABLE
-- Change primary key from CHAR(36) to INT
-- =============================================

-- Step 1: Drop foreign key constraints that reference BusinessLocation
ALTER TABLE UserLocationAssignment DROP FOREIGN KEY fk_user_location_assignment_location;
ALTER TABLE Product DROP FOREIGN KEY fk_product_business_location;

-- Step 2: Drop primary key and add new AUTO_INCREMENT primary key
ALTER TABLE BusinessLocation 
    DROP PRIMARY KEY,
    ADD COLUMN business_location_id_new INT NOT NULL AUTO_INCREMENT FIRST,
    ADD PRIMARY KEY (business_location_id_new);

-- Step 3: Add mapping for old UUID to new INT
ALTER TABLE BusinessLocation 
    ADD COLUMN business_location_id_old CHAR(36) AFTER business_location_id_new;

-- Step 4: Copy old UUID values to _old column
UPDATE BusinessLocation 
SET business_location_id_old = business_location_id;

-- Step 5: Drop old UUID column and rename new column
ALTER TABLE BusinessLocation 
    DROP COLUMN business_location_id,
    CHANGE COLUMN business_location_id_new business_location_id INT NOT NULL AUTO_INCREMENT;

-- Step 6: Update UserLocationAssignment to use INT
ALTER TABLE UserLocationAssignment
    MODIFY COLUMN business_location_id INT NOT NULL COMMENT 'Assigned location';

-- Step 7: Update Product table to use INT
ALTER TABLE Product
    MODIFY COLUMN business_location_id INT NOT NULL COMMENT 'Warehouse/location of product';

-- =============================================
-- ALTER USER LOCATION ASSIGNMENT TABLE
-- Change primary key from CHAR(36) to INT
-- =============================================

-- Step 8: Change UserLocationAssignment primary key to INT AUTO_INCREMENT
ALTER TABLE UserLocationAssignment
    DROP PRIMARY KEY,
    MODIFY COLUMN user_location_assignment_id INT NOT NULL AUTO_INCREMENT,
    ADD PRIMARY KEY (user_location_assignment_id);

-- =============================================
-- RESTORE FOREIGN KEY CONSTRAINTS
-- =============================================

-- Step 9: Recreate foreign key constraints
ALTER TABLE UserLocationAssignment
    ADD CONSTRAINT fk_user_location_assignment_location FOREIGN KEY (business_location_id) 
        REFERENCES BusinessLocation(business_location_id) ON DELETE CASCADE ON UPDATE CASCADE;

ALTER TABLE Product
    ADD CONSTRAINT fk_product_business_location FOREIGN KEY (business_location_id) 
        REFERENCES BusinessLocation(business_location_id) ON DELETE CASCADE ON UPDATE CASCADE;

-- =============================================
-- CLEANUP: Remove mapping column
-- =============================================
ALTER TABLE BusinessLocation 
    DROP COLUMN business_location_id_old;

-- =============================================
-- Insert this migration
-- =============================================
INSERT INTO __MigrationHistory (MigrationId, ProductVersion) 
VALUES ('007_alter_primary_key_types', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
