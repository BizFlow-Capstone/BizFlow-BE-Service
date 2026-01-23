-- Migration: 003_insert_default_roles
-- Description: Insert default roles (admin, manager, user)
-- Date: 2026-01-23

-- Insert default roles
INSERT INTO Roles (Id, Name, Description, CreatedAt, UpdatedAt) VALUES
(UUID(), 'admin', 'Administrator with full system access', NOW(), NOW()),
(UUID(), 'manager', 'Manager with elevated privileges', NOW(), NOW()),
(UUID(), 'user', 'Standard user with basic access', NOW(), NOW());

-- Insert this migration
INSERT INTO __MigrationHistory (MigrationId, ProductVersion) 
VALUES ('003_insert_default_roles', '1.0.0');