-- Migration: 027_fix_fk_references_to_profiles
-- Description: Fix FK references - all tables should reference Profiles (not Accounts)
--              Accounts only connects to Profiles; Profiles connects to all other tables
-- Date: 2026-03-11

-- =============================================
-- 1. HIRES: OwnerId, EmployeeId → Profiles(ProfileId)
-- =============================================
ALTER TABLE Hires
    DROP FOREIGN KEY fk_hire_owner,
    DROP FOREIGN KEY fk_hire_employee;

ALTER TABLE Hires
    ADD CONSTRAINT fk_hire_owner FOREIGN KEY (OwnerId)
        REFERENCES Profiles(ProfileId) ON DELETE CASCADE ON UPDATE CASCADE,
    ADD CONSTRAINT fk_hire_employee FOREIGN KEY (EmployeeId)
        REFERENCES Profiles(ProfileId) ON DELETE CASCADE ON UPDATE CASCADE;

-- =============================================
-- 2. USERLOCATIONASSIGNMENTS: UserId → Profiles(ProfileId)
-- =============================================
-- Drop whichever FK name exists (could be old or new name)
SET @fk_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'UserLocationAssignments'
    AND CONSTRAINT_NAME = 'fk_user_location_assignment_account' AND CONSTRAINT_TYPE = 'FOREIGN KEY');
SET @sql = IF(@fk_exists > 0,
    'ALTER TABLE UserLocationAssignments DROP FOREIGN KEY fk_user_location_assignment_account',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @fk_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'UserLocationAssignments'
    AND CONSTRAINT_NAME = 'fk_user_location_assignment_profile' AND CONSTRAINT_TYPE = 'FOREIGN KEY');
SET @sql = IF(@fk_exists > 0,
    'SELECT 1',
    'ALTER TABLE UserLocationAssignments ADD CONSTRAINT fk_user_location_assignment_profile FOREIGN KEY (UserId) REFERENCES Profiles(ProfileId) ON DELETE CASCADE ON UPDATE CASCADE');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- =============================================
-- 3. BUSINESSTYPES: CreatedBy, ModifiedBy → Profiles(ProfileId)
-- =============================================
ALTER TABLE BusinessTypes
    DROP FOREIGN KEY fk_business_type_created_by,
    DROP FOREIGN KEY fk_business_type_modified_by;

ALTER TABLE BusinessTypes
    ADD CONSTRAINT fk_business_type_created_by FOREIGN KEY (CreatedBy)
        REFERENCES Profiles(ProfileId) ON DELETE SET NULL ON UPDATE CASCADE,
    ADD CONSTRAINT fk_business_type_modified_by FOREIGN KEY (ModifiedBy)
        REFERENCES Profiles(ProfileId) ON DELETE SET NULL ON UPDATE CASCADE;

-- =============================================
-- 4. BUSINESSTYPETAXES: CreatedBy → Profiles(ProfileId)
-- =============================================
ALTER TABLE BusinessTypeTaxes
    DROP FOREIGN KEY fk_business_type_tax_created_by;

ALTER TABLE BusinessTypeTaxes
    ADD CONSTRAINT fk_business_type_tax_created_by FOREIGN KEY (CreatedBy)
        REFERENCES Profiles(ProfileId) ON DELETE SET NULL ON UPDATE CASCADE;

-- =============================================
-- 5. IMPORTSCHEMAVERSIONS: CreatedBy → Profiles(ProfileId)
-- =============================================
SET @fk_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ImportSchemaVersions'
    AND CONSTRAINT_NAME = 'fk_import_schema_version_created_by' AND CONSTRAINT_TYPE = 'FOREIGN KEY');
SET @sql = IF(@fk_exists > 0,
    'ALTER TABLE ImportSchemaVersions DROP FOREIGN KEY fk_import_schema_version_created_by',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

ALTER TABLE ImportSchemaVersions
    ADD CONSTRAINT fk_import_schema_version_created_by FOREIGN KEY (CreatedBy)
        REFERENCES Profiles(ProfileId) ON DELETE SET NULL ON UPDATE CASCADE;

-- =============================================
-- 6. SYSTEMCONFIG: UpdatedBy → Profiles(ProfileId)
-- =============================================
SET @fk_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SystemConfig'
    AND CONSTRAINT_NAME = 'fk_system_config_updated_by' AND CONSTRAINT_TYPE = 'FOREIGN KEY');
SET @sql = IF(@fk_exists > 0,
    'ALTER TABLE SystemConfig DROP FOREIGN KEY fk_system_config_updated_by',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

ALTER TABLE SystemConfig
    ADD CONSTRAINT fk_system_config_updated_by FOREIGN KEY (UpdatedBy)
        REFERENCES Profiles(ProfileId) ON DELETE SET NULL ON UPDATE CASCADE;

-- =============================================
-- Insert this migration
-- =============================================
INSERT INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('027_fix_fk_references_to_profiles', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
