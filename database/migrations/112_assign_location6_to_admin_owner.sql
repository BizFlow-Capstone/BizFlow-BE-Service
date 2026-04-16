-- =============================================
-- Migration  : 112_assign_location6_to_admin_owner
-- Description: Reassign BusinessLocationId = 6 owner to admin account seeded in migration 069
-- Date       : 2026-04-16
-- =============================================

SET @target_location_id = 6;
SET @admin_email = 'admin@bizflow.local';

-- Resolve admin profile from credential (migration 069 seed)
SET @admin_profile_id = (
    SELECT p.ProfileId
    FROM Profiles p
    INNER JOIN Accounts a ON a.AccountId = p.AccountId
    INNER JOIN Credentials c ON c.AccountId = a.AccountId
    WHERE c.Type = 'email'
      AND c.Identifier COLLATE utf8mb4_unicode_ci = @admin_email COLLATE utf8mb4_unicode_ci
    LIMIT 1
);

  INSERT INTO BusinessLocations (
    BusinessLocationId,
    LocationName,
    Address,
    Status,
    IsActive,
    TaxCode,
    CreatedAt,
    UpdatedAt,
    DeletedAt
  )
  SELECT
    @target_location_id,
    'BizFlow Sample Location 6',
    'Sample address for location 6',
    'active',
    TRUE,
    'TAX-LOC6-SEED',
    NOW(),
    NOW(),
    NULL
  WHERE NOT EXISTS (
    SELECT 1
    FROM BusinessLocations
    WHERE BusinessLocationId = @target_location_id
  );

SET @location_exists = (
    SELECT COUNT(*)
    FROM BusinessLocations
    WHERE BusinessLocationId = @target_location_id
);

SET @can_apply = IF(@admin_profile_id IS NOT NULL AND @location_exists > 0, 1, 0);

-- Deactivate current active owner rows at location 6 that are not admin.
UPDATE UserLocationAssignments
SET IsActive = FALSE,
    UnassignedAt = COALESCE(UnassignedAt, NOW())
WHERE @can_apply = 1
  AND BusinessLocationId = @target_location_id
  AND IsOwner = TRUE
  AND IsActive = TRUE
  AND UserId <> @admin_profile_id;

-- Deactivate admin's active non-owner rows for the same location (if any).
UPDATE UserLocationAssignments
SET IsActive = FALSE,
    UnassignedAt = COALESCE(UnassignedAt, NOW())
WHERE @can_apply = 1
  AND BusinessLocationId = @target_location_id
  AND UserId = @admin_profile_id
  AND IsOwner = FALSE
  AND IsActive = TRUE;

-- Ensure one active owner row for admin at location 6.
INSERT INTO UserLocationAssignments (UserId, BusinessLocationId, IsOwner, IsActive, AssignedAt, UnassignedAt)
SELECT @admin_profile_id, @target_location_id, TRUE, TRUE, NOW(), NULL
WHERE @can_apply = 1
  AND NOT EXISTS (
      SELECT 1
      FROM UserLocationAssignments
      WHERE UserId = @admin_profile_id
        AND BusinessLocationId = @target_location_id
        AND IsOwner = TRUE
        AND IsActive = TRUE
  );

SELECT IF(
    @can_apply = 1,
    CONCAT('Location ', @target_location_id, ' ownership assigned to admin profile ', @admin_profile_id),
    'Skip ownership assignment: location 6 or admin credential not found'
) AS Info;

INSERT IGNORE INTO __MigrationHistory (MigrationId, ProductVersion)
VALUES ('112_assign_location6_to_admin_owner', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = VALUES(ProductVersion);