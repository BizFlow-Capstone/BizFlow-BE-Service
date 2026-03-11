-- Migration: 026_update_schema_to_match_erd_v2
-- Description: Align database schema with Bizflow-ERD_Updated.drawio.svg
--   - Roles: Rename PK Id → RoleId, rename timestamps
--   - Users → Accounts + Profiles (split table)
--   - Accounts only connects to Profiles; Profiles connects to all other tables
--   - Hires: Update FKs to reference Profiles
--   - SystemConfig: New table
--   - BusinessTypes: Rename audit columns, FKs → Profiles
--   - BusinessTypeTaxes: Rename columns, FKs → Profiles
--   - BusinessLocations: Restructure columns
--   - UserLocationAssignments: Remove IsOwner/IsActive, FK → Profiles
--   - Imports: Add HasInvoice, SchemaVersionId FK, SchemaDataJson, ConfirmedAt, CancelledAt
--   - ImportSchemaVersions: Add MappingJson, TemplateFileUrl, VersionLabel, EffectiveFrom, CreatedBy FK → Profiles
--   - ProductsImports → ProductImports: Rename table, add auto-increment PK
--   - StockMovements: New table
-- Date: 2026-03-11

-- =============================================
-- 1. ROLES TABLE: Rename PK and timestamp columns
-- =============================================
ALTER TABLE Roles
    CHANGE COLUMN Id RoleId CHAR(36) NOT NULL,
    CHANGE COLUMN CreatedAt CreateAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CHANGE COLUMN UpdatedAt UpdateAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP;

-- =============================================
-- 2. CREATE ACCOUNTS TABLE
--    ERD: Accounts(AccountId PK, RoleId FK→Roles, Email, Phone, PasswordHash, IsActive, EmailVerified, LastLoginAt, CreatedAt, UpdatedAt, DeletedAt)
--    Accounts ONLY connects to Roles and Profiles
-- =============================================
CREATE TABLE IF NOT EXISTS Accounts (
    AccountId CHAR(36) NOT NULL PRIMARY KEY,
    RoleId CHAR(36) NOT NULL COMMENT 'FK to Roles',
    Email VARCHAR(255) NOT NULL UNIQUE COMMENT 'User email (login)',
    Phone VARCHAR(20) DEFAULT NULL COMMENT 'Phone number',
    PasswordHash VARCHAR(255) NOT NULL COMMENT 'Hashed password',
    IsActive BOOLEAN NOT NULL DEFAULT TRUE COMMENT 'Account status',
    EmailVerified BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'Email verification status',
    LastLoginAt DATETIME DEFAULT NULL COMMENT 'Last login timestamp',
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    DeletedAt DATETIME DEFAULT NULL COMMENT 'Soft delete timestamp',
    CONSTRAINT fk_account_role FOREIGN KEY (RoleId)
        REFERENCES Roles(RoleId) ON DELETE RESTRICT ON UPDATE CASCADE,
    INDEX idx_account_email (Email),
    INDEX idx_account_phone (Phone),
    INDEX idx_account_role (RoleId),
    INDEX idx_account_is_active (IsActive),
    INDEX idx_account_deleted_at (DeletedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- 3. CREATE PROFILES TABLE
--    ERD: Profiles(ProfileId PK, AccountId FK→Accounts, FullName, AvatarUrl, TaxCode, UpdatedAt)
--    Profiles is the hub that connects to all other tables
-- =============================================
CREATE TABLE IF NOT EXISTS Profiles (
    ProfileId CHAR(36) NOT NULL PRIMARY KEY,
    AccountId CHAR(36) NOT NULL COMMENT 'FK to Accounts',
    FullName VARCHAR(255) NOT NULL COMMENT 'Full name',
    AvatarUrl VARCHAR(500) DEFAULT NULL COMMENT 'Profile avatar URL',
    TaxCode VARCHAR(50) DEFAULT NULL COMMENT 'Personal tax identification number',
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_profile_account FOREIGN KEY (AccountId)
        REFERENCES Accounts(AccountId) ON DELETE CASCADE ON UPDATE CASCADE,
    UNIQUE INDEX idx_profile_account (AccountId),
    INDEX idx_profile_full_name (FullName)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- 4. MIGRATE DATA: Users → Accounts + Profiles
--    ProfileId = UserId so existing FK values in other tables still work
-- =============================================
INSERT INTO Accounts (AccountId, RoleId, Email, Phone, PasswordHash, IsActive, EmailVerified, LastLoginAt, CreatedAt, UpdatedAt, DeletedAt)
SELECT UserId, RoleId, Email, Phone, PasswordHash, IsActive, EmailVerified, LastLoginAt, CreatedAt, UpdatedAt, DeletedAt
FROM Users;

INSERT INTO Profiles (ProfileId, AccountId, FullName, AvatarUrl, TaxCode, UpdatedAt)
SELECT UserId, UserId, FullName, AvatarUrl, TaxCode, UpdatedAt
FROM Users;

-- =============================================
-- 5. UPDATE ALL FK REFERENCES: Users → Profiles
--    In the ERD, Profiles (not Accounts) connects to all other tables
-- =============================================

-- 5a. Hires: Drop old FKs, re-create pointing to Profiles
ALTER TABLE Hires
    DROP FOREIGN KEY fk_hire_owner,
    DROP FOREIGN KEY fk_hire_employee;

ALTER TABLE Hires
    ADD CONSTRAINT fk_hire_owner FOREIGN KEY (OwnerId)
        REFERENCES Profiles(ProfileId) ON DELETE CASCADE ON UPDATE CASCADE,
    ADD CONSTRAINT fk_hire_employee FOREIGN KEY (EmployeeId)
        REFERENCES Profiles(ProfileId) ON DELETE CASCADE ON UPDATE CASCADE;

-- 5b. UserLocationAssignments: Drop old FK, re-create pointing to Profiles
ALTER TABLE UserLocationAssignments
    DROP FOREIGN KEY fk_user_location_assignment_user;

ALTER TABLE UserLocationAssignments
    ADD CONSTRAINT fk_user_location_assignment_profile FOREIGN KEY (UserId)
        REFERENCES Profiles(ProfileId) ON DELETE CASCADE ON UPDATE CASCADE;

-- 5c. BusinessTypes: Drop old FKs, re-create pointing to Profiles
ALTER TABLE BusinessTypes
    DROP FOREIGN KEY fk_business_type_created_by,
    DROP FOREIGN KEY fk_business_type_modified_by;

ALTER TABLE BusinessTypes
    ADD CONSTRAINT fk_business_type_created_by FOREIGN KEY (CreatedById)
        REFERENCES Profiles(ProfileId) ON DELETE SET NULL ON UPDATE CASCADE,
    ADD CONSTRAINT fk_business_type_modified_by FOREIGN KEY (ModifiedById)
        REFERENCES Profiles(ProfileId) ON DELETE SET NULL ON UPDATE CASCADE;

-- 5d. BusinessTypeTaxes: Drop old FK, re-create pointing to Profiles
ALTER TABLE BusinessTypeTaxes
    DROP FOREIGN KEY fk_business_type_tax_created_by;

ALTER TABLE BusinessTypeTaxes
    ADD CONSTRAINT fk_business_type_tax_created_by FOREIGN KEY (CreatedById)
        REFERENCES Profiles(ProfileId) ON DELETE SET NULL ON UPDATE CASCADE;

-- =============================================
-- 6. DROP Users TABLE (replaced by Accounts + Profiles)
-- =============================================
DROP TABLE IF EXISTS Users;

-- =============================================
-- 7. BUSINESSTYPES: Rename audit columns to match ERD
--    CreatedById → CreatedBy, ModifiedById → ModifiedBy
--    CreatedDate → CreatedAt, LastModifiedDate → LastModifiedAt
-- =============================================
ALTER TABLE BusinessTypes
    DROP FOREIGN KEY fk_business_type_created_by,
    DROP FOREIGN KEY fk_business_type_modified_by;

ALTER TABLE BusinessTypes
    CHANGE COLUMN CreatedById CreatedBy CHAR(36) DEFAULT NULL,
    CHANGE COLUMN ModifiedById ModifiedBy CHAR(36) DEFAULT NULL,
    CHANGE COLUMN CreatedDate CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CHANGE COLUMN LastModifiedDate LastModifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP;

ALTER TABLE BusinessTypes
    ADD CONSTRAINT fk_business_type_created_by FOREIGN KEY (CreatedBy)
        REFERENCES Profiles(ProfileId) ON DELETE SET NULL ON UPDATE CASCADE,
    ADD CONSTRAINT fk_business_type_modified_by FOREIGN KEY (ModifiedBy)
        REFERENCES Profiles(ProfileId) ON DELETE SET NULL ON UPDATE CASCADE;

-- =============================================
-- 8. BUSINESSTYPETAXES: Rename columns to match ERD
--    CalculateOnPrice → CalculationBase, CreatedById → CreatedBy
-- =============================================
ALTER TABLE BusinessTypeTaxes
    DROP FOREIGN KEY fk_business_type_tax_created_by;

ALTER TABLE BusinessTypeTaxes
    CHANGE COLUMN CalculateOnPrice CalculationBase VARCHAR(50) NOT NULL DEFAULT 'price' COMMENT 'Calculation base: price, revenue',
    CHANGE COLUMN CreatedById CreatedBy CHAR(36) DEFAULT NULL,
    CHANGE COLUMN CreatedDate CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP;

ALTER TABLE BusinessTypeTaxes
    ADD CONSTRAINT fk_business_type_tax_created_by FOREIGN KEY (CreatedBy)
        REFERENCES Profiles(ProfileId) ON DELETE SET NULL ON UPDATE CASCADE;

-- =============================================
-- 9. BUSINESSLOCATIONS: Restructure to match ERD
--    ERD: BusinessLocationId PK, LocationName, TaxCode, Address, Phone, Email, Status, CreatedAt, UpdatedAt
--    Remove: District, City, IsActive, DeletedAt
--    Rename: Name → LocationName
--    Add: Email, Status, CreatedAt, UpdatedAt
-- =============================================
ALTER TABLE BusinessLocations
    CHANGE COLUMN Name LocationName VARCHAR(255) NOT NULL COMMENT 'Location/store name',
    DROP COLUMN District,
    DROP COLUMN City,
    DROP COLUMN IsActive,
    DROP COLUMN DeletedAt,
    ADD COLUMN Email VARCHAR(255) DEFAULT NULL COMMENT 'Location email' AFTER Phone,
    ADD COLUMN Status VARCHAR(20) NOT NULL DEFAULT 'active' COMMENT 'active, inactive' AFTER Email,
    ADD COLUMN CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP AFTER Status,
    ADD COLUMN UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP AFTER CreatedAt;

-- Drop stale indexes
SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'BusinessLocations' AND INDEX_NAME = 'idx_business_location_city');
SET @sql = IF(@idx_exists > 0,
    'ALTER TABLE BusinessLocations DROP INDEX idx_business_location_city',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'BusinessLocations' AND INDEX_NAME = 'idx_business_location_is_active');
SET @sql = IF(@idx_exists > 0,
    'ALTER TABLE BusinessLocations DROP INDEX idx_business_location_is_active',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'BusinessLocations' AND INDEX_NAME = 'idx_business_location_is_deleted');
SET @sql = IF(@idx_exists > 0,
    'ALTER TABLE BusinessLocations DROP INDEX idx_business_location_is_deleted',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Add new indexes
ALTER TABLE BusinessLocations
    ADD INDEX idx_business_location_status (Status);

SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'BusinessLocations' AND INDEX_NAME = 'idx_business_location_name');
SET @sql = IF(@idx_exists > 0,
    'ALTER TABLE BusinessLocations DROP INDEX idx_business_location_name',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

ALTER TABLE BusinessLocations
    ADD INDEX idx_business_location_name (LocationName);

-- =============================================
-- 10. USERLOCATIONASSIGNMENTS: Remove IsOwner, IsActive (not in ERD)
-- =============================================
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'UserLocationAssignments' AND COLUMN_NAME = 'IsOwner');
SET @sql = IF(@col_exists > 0,
    'ALTER TABLE UserLocationAssignments DROP COLUMN IsOwner',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'UserLocationAssignments' AND COLUMN_NAME = 'IsActive');
SET @sql = IF(@col_exists > 0,
    'ALTER TABLE UserLocationAssignments DROP COLUMN IsActive',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- =============================================
-- 11. IMPORTS: Add missing columns from ERD
--     HasInvoice, SchemaVersionId FK, SchemaDataJson, ConfirmedAt, CancelledAt
-- =============================================
ALTER TABLE Imports
    ADD COLUMN HasInvoice BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'Whether the import has an invoice attached' AFTER Supplier,
    ADD COLUMN SchemaVersionId INT DEFAULT NULL COMMENT 'FK to ImportSchemaVersions' AFTER ImagePublicId,
    ADD COLUMN SchemaDataJson LONGTEXT DEFAULT NULL COMMENT 'Stored data captured from schema form' AFTER SchemaVersionId,
    ADD COLUMN ConfirmedAt DATETIME DEFAULT NULL COMMENT 'When the import was confirmed' AFTER UpdatedAt,
    ADD COLUMN CancelledAt DATETIME DEFAULT NULL COMMENT 'When the import was cancelled' AFTER ConfirmedAt;

-- Drop old SchemaJson column if it exists (replaced by SchemaDataJson + SchemaVersionId)
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Imports' AND COLUMN_NAME = 'SchemaJson');
SET @sql = IF(@col_exists > 0,
    'ALTER TABLE Imports DROP COLUMN SchemaJson',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Add FK for SchemaVersionId
ALTER TABLE Imports
    ADD CONSTRAINT fk_import_schema_version FOREIGN KEY (SchemaVersionId)
        REFERENCES ImportSchemaVersions(ImportSchemaVersionId) ON DELETE SET NULL ON UPDATE CASCADE,
    ADD INDEX idx_import_schema_version (SchemaVersionId);

-- =============================================
-- 12. IMPORTSCHEMAVERSIONS: Add missing columns from ERD
--     MappingJson, TemplateFileUrl, VersionLabel, EffectiveFrom, CreatedBy FK → Profiles
-- =============================================
ALTER TABLE ImportSchemaVersions
    ADD COLUMN MappingJson LONGTEXT DEFAULT NULL COMMENT 'JSON mapping definition for data transformation' AFTER SchemaJson,
    ADD COLUMN TemplateFileUrl VARCHAR(500) DEFAULT NULL COMMENT 'URL of the template file for this version' AFTER MappingJson,
    ADD COLUMN VersionLabel VARCHAR(50) DEFAULT NULL COMMENT 'Human-readable version label' AFTER TemplateFileUrl,
    ADD COLUMN EffectiveFrom DATETIME DEFAULT NULL COMMENT 'When this version becomes effective' AFTER IsActive,
    ADD COLUMN CreatedBy CHAR(36) DEFAULT NULL COMMENT 'FK to Profiles - who created this version' AFTER CreatedAt;

-- Add FK for CreatedBy → Profiles
ALTER TABLE ImportSchemaVersions
    ADD CONSTRAINT fk_import_schema_version_created_by FOREIGN KEY (CreatedBy)
        REFERENCES Profiles(ProfileId) ON DELETE SET NULL ON UPDATE CASCADE;

-- =============================================
-- 13. PRODUCTSIMPORTS → PRODUCTIMPORTS: Rename table, add auto-increment PK
--     ERD: ProductImportId PK, ImportId FK, ProductId FK, Quantity, CostPrice, TotalPrice, BaseUnit, CreatedAt
-- =============================================
CREATE TABLE IF NOT EXISTS ProductImports (
    ProductImportId BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    ImportId BIGINT NOT NULL COMMENT 'FK to Imports',
    ProductId BIGINT NOT NULL COMMENT 'FK to Products',
    Quantity INT NOT NULL DEFAULT 0 COMMENT 'Import quantity',
    CostPrice DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Cost price per import unit',
    TotalPrice DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Total price',
    BaseUnit VARCHAR(50) NOT NULL DEFAULT 'Unit' COMMENT 'Base/smallest inventory unit',
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_product_import_new_import FOREIGN KEY (ImportId)
        REFERENCES Imports(ImportId) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_product_import_new_product FOREIGN KEY (ProductId)
        REFERENCES Products(ProductId) ON DELETE RESTRICT ON UPDATE CASCADE,
    INDEX idx_product_import_import (ImportId),
    INDEX idx_product_import_product (ProductId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Migrate data from old table
INSERT INTO ProductImports (ImportId, ProductId, Quantity, CostPrice, TotalPrice, BaseUnit)
SELECT ImportId, ProductId, Quantity, CostPrice, TotalPrice, BaseUnit
FROM ProductsImports;

-- Drop old table
DROP TABLE IF EXISTS ProductsImports;

-- =============================================
-- 14. STOCKMOVEMENTS: New table from ERD
--     StockMovementId PK, ProductId FK, MovementType, Quantity, ReferenceType, ReferenceId, BalanceAfter, CreatedAt
-- =============================================
CREATE TABLE IF NOT EXISTS StockMovements (
    StockMovementId BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    ProductId BIGINT NOT NULL COMMENT 'FK to Products',
    MovementType VARCHAR(50) NOT NULL COMMENT 'IN, OUT, ADJUSTMENT',
    Quantity INT NOT NULL COMMENT 'Quantity moved (positive for IN, negative for OUT)',
    ReferenceType VARCHAR(50) DEFAULT NULL COMMENT 'IMPORT, ORDER, ADJUSTMENT',
    ReferenceId BIGINT DEFAULT NULL COMMENT 'ID of the reference entity (ImportId, OrderId, etc.)',
    BalanceAfter INT NOT NULL DEFAULT 0 COMMENT 'Stock balance after this movement',
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_stock_movement_product FOREIGN KEY (ProductId)
        REFERENCES Products(ProductId) ON DELETE RESTRICT ON UPDATE CASCADE,
    INDEX idx_stock_movement_product (ProductId),
    INDEX idx_stock_movement_type (MovementType),
    INDEX idx_stock_movement_reference (ReferenceType, ReferenceId),
    INDEX idx_stock_movement_created_at (CreatedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- 15. SYSTEMCONFIG: New table from ERD
--     SystemConfigId PK, Name, Value, Description, UpdatedAt, UpdatedBy FK → Profiles
-- =============================================
CREATE TABLE IF NOT EXISTS SystemConfig (
    SystemConfigId CHAR(36) NOT NULL PRIMARY KEY,
    Name VARCHAR(255) NOT NULL COMMENT 'Configuration key name',
    Value TEXT DEFAULT NULL COMMENT 'Configuration value',
    Description TEXT DEFAULT NULL COMMENT 'Description of this config entry',
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UpdatedBy CHAR(36) DEFAULT NULL COMMENT 'FK to Profiles - who last updated',
    UNIQUE INDEX idx_system_config_name (Name),
    CONSTRAINT fk_system_config_updated_by FOREIGN KEY (UpdatedBy)
        REFERENCES Profiles(ProfileId) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- Insert this migration
-- =============================================
INSERT INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('026_update_schema_to_match_erd_v2', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
