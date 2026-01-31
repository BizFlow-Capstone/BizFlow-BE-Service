-- Migration: 006_add_user_table
-- Description: Add User table and relationships with Roles and UserLocationAssignment
-- Date: 2026-01-31

-- =============================================
-- USER TABLE (Application users)
-- Medium data volume => UUID
-- =============================================
CREATE TABLE IF NOT EXISTS User (
    user_id CHAR(36) NOT NULL PRIMARY KEY,
    email VARCHAR(255) NOT NULL UNIQUE COMMENT 'User email (login)',
    password_hash VARCHAR(255) NOT NULL COMMENT 'Hashed password',
    full_name VARCHAR(255) NOT NULL COMMENT 'Full name',
    phone VARCHAR(20) DEFAULT NULL COMMENT 'Phone number',
    avatar_url VARCHAR(500) DEFAULT NULL COMMENT 'Profile avatar URL',
    tax_code VARCHAR(50) DEFAULT NULL COMMENT 'Personal tax identification number',
    role_id CHAR(36) NOT NULL COMMENT 'User role',
    is_active BOOLEAN NOT NULL DEFAULT TRUE COMMENT 'Account status',
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'Soft delete flag',
    email_verified BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'Email verification status',
    last_login_at DATETIME DEFAULT NULL COMMENT 'Last login timestamp',
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_user_role FOREIGN KEY (role_id) 
        REFERENCES Roles(Id) ON DELETE RESTRICT ON UPDATE CASCADE,
    INDEX idx_user_email (email),
    INDEX idx_user_full_name (full_name),
    INDEX idx_user_phone (phone),
    INDEX idx_user_role (role_id),
    INDEX idx_user_is_active (is_active)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- ADD FOREIGN KEY: UserLocationAssignment -> User
-- Link user assignments to the User table
-- =============================================
ALTER TABLE UserLocationAssignment
    ADD CONSTRAINT fk_user_location_assignment_user FOREIGN KEY (user_id) 
        REFERENCES User(user_id) ON DELETE CASCADE ON UPDATE CASCADE;

-- =============================================
-- ADD FOREIGN KEY: BusinessType -> User (CreatedById)
-- =============================================
ALTER TABLE BusinessType
    ADD CONSTRAINT fk_business_type_created_by FOREIGN KEY (CreatedById) 
        REFERENCES User(user_id) ON DELETE SET NULL ON UPDATE CASCADE,
    ADD CONSTRAINT fk_business_type_modified_by FOREIGN KEY (ModifiedById) 
        REFERENCES User(user_id) ON DELETE SET NULL ON UPDATE CASCADE;

-- =============================================
-- ADD FOREIGN KEY: BusinessTypeTax -> User (CreatedById)
-- =============================================
ALTER TABLE BusinessTypeTax
    ADD CONSTRAINT fk_business_type_tax_created_by FOREIGN KEY (CreatedById) 
        REFERENCES User(user_id) ON DELETE SET NULL ON UPDATE CASCADE;

-- Insert this migration
INSERT INTO __MigrationHistory (MigrationId, ProductVersion) 
VALUES ('006_add_user_table', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
