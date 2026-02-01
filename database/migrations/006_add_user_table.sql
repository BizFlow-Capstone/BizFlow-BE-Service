-- Migration: 006_add_user_table
-- Description: Add Users table and relationships with Roles and UserLocationAssignments
-- Date: 2026-01-31

-- =============================================
-- USERS TABLE (Application users)
-- Medium data volume => UUID
-- =============================================
CREATE TABLE IF NOT EXISTS Users (
    UserId CHAR(36) NOT NULL PRIMARY KEY,
    Email VARCHAR(255) NOT NULL UNIQUE COMMENT 'User email (login)',
    PasswordHash VARCHAR(255) NOT NULL COMMENT 'Hashed password',
    FullName VARCHAR(255) NOT NULL COMMENT 'Full name',
    Phone VARCHAR(20) DEFAULT NULL COMMENT 'Phone number',
    AvatarUrl VARCHAR(500) DEFAULT NULL COMMENT 'Profile avatar URL',
    TaxCode VARCHAR(50) DEFAULT NULL COMMENT 'Personal tax identification number',
    RoleId CHAR(36) NOT NULL COMMENT 'User role',
    IsActive BOOLEAN NOT NULL COMMENT 'Account status',
    IsDeleted BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'Soft delete flag',
    EmailVerified BOOLEAN NOT NULL COMMENT 'Email verification status',
    LastLoginAt DATETIME DEFAULT NULL COMMENT 'Last login timestamp',
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_user_role FOREIGN KEY (RoleId) 
        REFERENCES Roles(Id) ON DELETE RESTRICT ON UPDATE CASCADE,
    INDEX idx_user_email (Email),
    INDEX idx_user_full_name (FullName),
    INDEX idx_user_phone (Phone),
    INDEX idx_user_role (RoleId),
    INDEX idx_user_is_active (IsActive)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- ADD FOREIGN KEY: UserLocationAssignments -> Users
-- Link user assignments to the Users table
-- =============================================
ALTER TABLE UserLocationAssignments
    ADD CONSTRAINT fk_user_location_assignment_user FOREIGN KEY (UserId) 
        REFERENCES Users(UserId) ON DELETE CASCADE ON UPDATE CASCADE;

-- =============================================
-- ADD FOREIGN KEY: BusinessTypes -> Users (CreatedById)
-- =============================================
ALTER TABLE BusinessTypes
    ADD CONSTRAINT fk_business_type_created_by FOREIGN KEY (CreatedById) 
        REFERENCES Users(UserId) ON DELETE SET NULL ON UPDATE CASCADE,
    ADD CONSTRAINT fk_business_type_modified_by FOREIGN KEY (ModifiedById) 
        REFERENCES Users(UserId) ON DELETE SET NULL ON UPDATE CASCADE;

-- =============================================
-- ADD FOREIGN KEY: BusinessTypeTaxes -> Users (CreatedById)
-- =============================================
ALTER TABLE BusinessTypeTaxes
    ADD CONSTRAINT fk_business_type_tax_created_by FOREIGN KEY (CreatedById) 
        REFERENCES Users(UserId) ON DELETE SET NULL ON UPDATE CASCADE;

-- Insert this migration
INSERT INTO __MigrationHistory (MigrationId, ProductVersion) 
VALUES ('006_add_user_table', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
