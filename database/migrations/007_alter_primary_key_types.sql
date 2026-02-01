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
    ADD COLUMN BusinessLocationIdNew INT NOT NULL AUTO_INCREMENT FIRST,
    ADD PRIMARY KEY (BusinessLocationIdNew);

-- Step 3: Add mapping for old UUID to new INT
ALTER TABLE BusinessLocation 
    ADD COLUMN BusinessLocationIdOld CHAR(36) AFTER BusinessLocationIdNew;

-- Step 4: Copy old UUID values to _old column
UPDATE BusinessLocation 
SET BusinessLocationIdOld = BusinessLocationId;

-- Step 5: Drop old UUID column and rename new column
ALTER TABLE BusinessLocation 
    DROP COLUMN BusinessLocationId,
    CHANGE COLUMN BusinessLocationIdNew BusinessLocationId INT NOT NULL AUTO_INCREMENT;

-- Step 6: Update UserLocationAssignment to use INT
ALTER TABLE UserLocationAssignment
    MODIFY COLUMN BusinessLocationId INT NOT NULL COMMENT 'Assigned location';

-- Step 7: Update Product table to use INT
ALTER TABLE Product
    MODIFY COLUMN BusinessLocationId INT NOT NULL COMMENT 'Warehouse/location of product';

-- =============================================
-- ALTER USER LOCATION ASSIGNMENT TABLE
-- Change primary key from CHAR(36) to INT
-- =============================================

-- Step 8: Change UserLocationAssignment primary key to INT AUTO_INCREMENT
ALTER TABLE UserLocationAssignment
    DROP PRIMARY KEY,
    MODIFY COLUMN UserLocationAssignmentId INT NOT NULL AUTO_INCREMENT,
    ADD PRIMARY KEY (UserLocationAssignmentId);

-- =============================================
-- RESTORE FOREIGN KEY CONSTRAINTS
-- =============================================

-- Step 9: Recreate foreign key constraints
ALTER TABLE UserLocationAssignment
    ADD CONSTRAINT fk_user_location_assignment_location FOREIGN KEY (BusinessLocationId) 
        REFERENCES BusinessLocation(BusinessLocationId) ON DELETE CASCADE ON UPDATE CASCADE;

ALTER TABLE Product
    ADD CONSTRAINT fk_product_business_location FOREIGN KEY (BusinessLocationId) 
        REFERENCES BusinessLocation(BusinessLocationId) ON DELETE CASCADE ON UPDATE CASCADE;

-- =============================================
-- CLEANUP: Remove mapping column
-- =============================================
ALTER TABLE BusinessLocation 
    DROP COLUMN BusinessLocationIdOld;

-- =============================================
-- Insert this migration
-- =============================================
INSERT INTO __MigrationHistory (MigrationId, ProductVersion) 
VALUES ('007_alter_primary_key_types', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
