-- Migration: 069_seed_admin_consultant_accounts
-- Description: Seed idempotent admin/consultant accounts for testing admin APIs
-- Date: 2026-03-15

-- Default login credentials (both accounts):
-- Password: MySecureP@ssw0rd123
-- BCrypt hash generated with cost factor 12
SET @default_password_hash = '$2a$12$ZXWiNvtS/vwl4JJWmso.j.pNoFbpI9gLDWpiPFaWny45pR0aIIaea';

SET @admin_role_id = '2aa3bcb2-0bf1-4cf2-8a2d-4d5c0d8e9101';
SET @consultant_role_id = '75cce410-14af-4ab4-bf9c-65cb90a9fbe3';

SET @admin_account_id_seed = 'f3f4c5c0-5eb8-4be5-9d6d-0a0ec6a7a101';
SET @admin_profile_id_seed = '4ea8a8ef-d1b5-4a53-b972-5f56a4e5a101';
SET @admin_credential_id_seed = '8f4a2e4a-0f4a-4e5a-a101-3f1d39b8b201';
SET @admin_email = 'admin@bizflow.local';
SET @admin_full_name = 'System Administrator';

SET @consultant_account_id_seed = 'f3f4c5c0-5eb8-4be5-9d6d-0a0ec6a7a102';
SET @consultant_profile_id_seed = '4ea8a8ef-d1b5-4a53-b972-5f56a4e5a102';
SET @consultant_credential_id_seed = '8f4a2e4a-0f4a-4e5a-a102-3f1d39b8b202';
SET @consultant_email = 'consultant@bizflow.local';
SET @consultant_full_name = 'Accounting Consultant';

-- 1) Ensure required roles exist
INSERT INTO Roles (RoleId, Name, Description, CreateAt, UpdateAt)
SELECT @admin_role_id, 'admin', 'System administrator', NOW(), NOW()
WHERE NOT EXISTS (
    SELECT 1
    FROM Roles
    WHERE Name COLLATE utf8mb4_unicode_ci = 'admin' COLLATE utf8mb4_unicode_ci
);

INSERT INTO Roles (RoleId, Name, Description, CreateAt, UpdateAt)
SELECT @consultant_role_id, 'consultant', 'Consultant with elevated privileges', NOW(), NOW()
WHERE NOT EXISTS (
    SELECT 1
    FROM Roles
    WHERE Name COLLATE utf8mb4_unicode_ci = 'consultant' COLLATE utf8mb4_unicode_ci
);

SET @admin_role_id = (
    SELECT RoleId
    FROM Roles
    WHERE Name COLLATE utf8mb4_unicode_ci = 'admin' COLLATE utf8mb4_unicode_ci
    LIMIT 1
);
SET @consultant_role_id = (
    SELECT RoleId
    FROM Roles
    WHERE Name COLLATE utf8mb4_unicode_ci = 'consultant' COLLATE utf8mb4_unicode_ci
    LIMIT 1
);

-- 2) Resolve admin account by existing email credential if present
SET @admin_account_id = (
    SELECT c.AccountId
    FROM Credentials c
    WHERE c.Type = 'email'
      AND c.Identifier COLLATE utf8mb4_unicode_ci = @admin_email COLLATE utf8mb4_unicode_ci
    LIMIT 1
);
SET @admin_account_id = IFNULL(@admin_account_id, @admin_account_id_seed);

INSERT INTO Accounts (AccountId, RoleId, PasswordHash, IsActive, LastLoginAt, CreatedAt, UpdatedAt, DeletedAt)
SELECT @admin_account_id, @admin_role_id, @default_password_hash, TRUE, NULL, NOW(), NOW(), NULL
WHERE NOT EXISTS (SELECT 1 FROM Accounts WHERE AccountId = @admin_account_id);

UPDATE Accounts
SET RoleId = @admin_role_id,
    PasswordHash = @default_password_hash,
    IsActive = TRUE,
    DeletedAt = NULL,
    UpdatedAt = NOW()
WHERE AccountId = @admin_account_id;

SET @admin_profile_id = (
    SELECT p.ProfileId
    FROM Profiles p
    WHERE p.AccountId = @admin_account_id
    LIMIT 1
);
SET @admin_profile_id = IFNULL(@admin_profile_id, @admin_profile_id_seed);

INSERT INTO Profiles (ProfileId, AccountId, FullName, AvatarUrl, TaxCode, UpdatedAt)
SELECT @admin_profile_id, @admin_account_id, @admin_full_name, NULL, NULL, NOW()
WHERE NOT EXISTS (SELECT 1 FROM Profiles WHERE AccountId = @admin_account_id);

UPDATE Profiles
SET FullName = @admin_full_name,
    UpdatedAt = NOW()
WHERE AccountId = @admin_account_id;

INSERT INTO Credentials (CredentialId, AccountId, Type, Identifier, EmailVerified, GoogleEmail, CreatedAt)
SELECT @admin_credential_id_seed, @admin_account_id, 'email', @admin_email, TRUE, NULL, NOW()
WHERE NOT EXISTS (
    SELECT 1
    FROM Credentials
    WHERE Type = 'email'
      AND Identifier COLLATE utf8mb4_unicode_ci = @admin_email COLLATE utf8mb4_unicode_ci
);

UPDATE Credentials
SET AccountId = @admin_account_id,
    EmailVerified = TRUE
WHERE Type = 'email'
    AND Identifier COLLATE utf8mb4_unicode_ci = @admin_email COLLATE utf8mb4_unicode_ci;

-- 3) Resolve consultant account by existing email credential if present
SET @consultant_account_id = (
    SELECT c.AccountId
    FROM Credentials c
    WHERE c.Type = 'email'
      AND c.Identifier COLLATE utf8mb4_unicode_ci = @consultant_email COLLATE utf8mb4_unicode_ci
    LIMIT 1
);
SET @consultant_account_id = IFNULL(@consultant_account_id, @consultant_account_id_seed);

INSERT INTO Accounts (AccountId, RoleId, PasswordHash, IsActive, LastLoginAt, CreatedAt, UpdatedAt, DeletedAt)
SELECT @consultant_account_id, @consultant_role_id, @default_password_hash, TRUE, NULL, NOW(), NOW(), NULL
WHERE NOT EXISTS (SELECT 1 FROM Accounts WHERE AccountId = @consultant_account_id);

UPDATE Accounts
SET RoleId = @consultant_role_id,
    PasswordHash = @default_password_hash,
    IsActive = TRUE,
    DeletedAt = NULL,
    UpdatedAt = NOW()
WHERE AccountId = @consultant_account_id;

SET @consultant_profile_id = (
    SELECT p.ProfileId
    FROM Profiles p
    WHERE p.AccountId = @consultant_account_id
    LIMIT 1
);
SET @consultant_profile_id = IFNULL(@consultant_profile_id, @consultant_profile_id_seed);

INSERT INTO Profiles (ProfileId, AccountId, FullName, AvatarUrl, TaxCode, UpdatedAt)
SELECT @consultant_profile_id, @consultant_account_id, @consultant_full_name, NULL, NULL, NOW()
WHERE NOT EXISTS (SELECT 1 FROM Profiles WHERE AccountId = @consultant_account_id);

UPDATE Profiles
SET FullName = @consultant_full_name,
    UpdatedAt = NOW()
WHERE AccountId = @consultant_account_id;

INSERT INTO Credentials (CredentialId, AccountId, Type, Identifier, EmailVerified, GoogleEmail, CreatedAt)
SELECT @consultant_credential_id_seed, @consultant_account_id, 'email', @consultant_email, TRUE, NULL, NOW()
WHERE NOT EXISTS (
    SELECT 1
    FROM Credentials
    WHERE Type = 'email'
      AND Identifier COLLATE utf8mb4_unicode_ci = @consultant_email COLLATE utf8mb4_unicode_ci
);

UPDATE Credentials
SET AccountId = @consultant_account_id,
    EmailVerified = TRUE
WHERE Type = 'email'
    AND Identifier COLLATE utf8mb4_unicode_ci = @consultant_email COLLATE utf8mb4_unicode_ci;

-- =============================================
-- Insert migration history
-- =============================================
INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('069_seed_admin_consultant_accounts', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);
