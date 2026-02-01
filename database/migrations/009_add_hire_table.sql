-- Migration: 009_add_hire_table
-- Description: Add Hires table for tracking employee hiring by owners
-- Date: 2026-02-01

-- =============================================
-- HIRES TABLE
-- Tracks which owners have hired which employees
-- =============================================
CREATE TABLE IF NOT EXISTS Hires (
    HireId INT NOT NULL AUTO_INCREMENT,
    OwnerId CHAR(36) NOT NULL COMMENT 'Owner who hired the employee',
    EmployeeId CHAR(36) NOT NULL COMMENT 'Employee being hired',
    IsActive BOOLEAN NOT NULL DEFAULT TRUE COMMENT 'Hiring status',
    StartAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Start date of employment',
    EndAt DATETIME DEFAULT NULL COMMENT 'End date of employment (NULL if still active)',
    PRIMARY KEY (HireId),
    CONSTRAINT fk_hire_owner FOREIGN KEY (OwnerId) 
        REFERENCES Users(UserId) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_hire_employee FOREIGN KEY (EmployeeId) 
        REFERENCES Users(UserId) ON DELETE CASCADE ON UPDATE CASCADE,
    UNIQUE INDEX idx_hire_owner_employee (OwnerId, EmployeeId),
    INDEX idx_hire_owner (OwnerId),
    INDEX idx_hire_employee (EmployeeId),
    INDEX idx_hire_is_active (IsActive)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Insert this migration
INSERT INTO __MigrationHistory (MigrationId, ProductVersion) 
VALUES ('009_add_hire_table', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
