-- ============================================================
-- Seed: Tenant Database Default Data
-- Run on: ascent_group_{N}  (run tenant_tables.sql first)
--
-- Steps:
--   1. Set @SchoolId to the school_id from the ascent_master.schools table
--      (the branch this seed data should apply to).
--   2. Optionally change @AdminPassword before running.
--   3. Connect to the tenant DB (e.g. ascent_group_1) in SSMS.
--   4. Run this script.
--
-- What this seeds:
--   A. Role → Permission mappings for all default roles
--   B. Payment modes (Cash, Cheque, Online)
--   C. A default school admin login user
--   D. Assigns Principal role to the admin user for @SchoolId
-- ============================================================

DECLARE @SchoolId      INT          = 1          -- ← set to actual school_id from ascent_master.schools
DECLARE @AdminPassword VARCHAR(100) = 'Admin@123' -- ← change before running

-- ── Hash the password (SHA-256 + Base64, matching .NET JwtHelper) ──────────
DECLARE @HashBytes   VARBINARY(32) = HASHBYTES('SHA2_256', CAST(@AdminPassword AS VARCHAR(100)))
DECLARE @HexStr      VARCHAR(64)   = CONVERT(VARCHAR(64), @HashBytes, 2)
DECLARE @PasswordHash VARCHAR(255)
SELECT @PasswordHash = CAST(N'' AS XML).value(
    'xs:base64Binary(xs:hexBinary(sql:variable("@HexStr")))', 'VARCHAR(255)')

PRINT 'Using SchoolId: ' + CAST(@SchoolId AS VARCHAR)
PRINT 'Admin password hash: ' + @PasswordHash


-- ============================================================
-- A. Role → Permission Mappings
--    Principal gets ALL permissions.
--    Other roles get their typical subset.
--    Idempotent — skips if already mapped.
-- ============================================================

-- Principal: all 24 permissions
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.role_id, p.permission_id
FROM roles r
CROSS JOIN permissions p
WHERE r.role_name = 'Principal'
  AND r.school_id IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM role_permissions rp
      WHERE rp.role_id = r.role_id AND rp.permission_id = p.permission_id
  )

-- Admin Clerk: student profile + fee view/collect + master data implied
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.role_id, p.permission_id
FROM roles r
CROSS JOIN permissions p
WHERE r.role_name = 'Admin Clerk'
  AND r.school_id IS NULL
  AND p.permission_code IN (
      'STUDENT_PROFILE.VIEW', 'STUDENT_PROFILE.CREATE', 'STUDENT_PROFILE.EDIT',
      'STUDENT_FEE.VIEW',
      'ATTENDANCE.VIEW',
      'MARKS.VIEW',
      'TRANSPORT.VIEW'
  )
  AND NOT EXISTS (
      SELECT 1 FROM role_permissions rp
      WHERE rp.role_id = r.role_id AND rp.permission_id = p.permission_id
  )

-- Fee Clerk: student profile view + all fee permissions
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.role_id, p.permission_id
FROM roles r
CROSS JOIN permissions p
WHERE r.role_name = 'Fee Clerk'
  AND r.school_id IS NULL
  AND p.permission_code IN (
      'STUDENT_PROFILE.VIEW',
      'STUDENT_FEE.VIEW', 'STUDENT_FEE.COLLECT', 'STUDENT_FEE.CANCEL_RECEIPT', 'STUDENT_FEE.CONCESSION'
  )
  AND NOT EXISTS (
      SELECT 1 FROM role_permissions rp
      WHERE rp.role_id = r.role_id AND rp.permission_id = p.permission_id
  )

-- Class Teacher: attendance + marks + student view
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.role_id, p.permission_id
FROM roles r
CROSS JOIN permissions p
WHERE r.role_name = 'Class Teacher'
  AND r.school_id IS NULL
  AND p.permission_code IN (
      'STUDENT_PROFILE.VIEW',
      'STUDENT_FEE.VIEW',
      'ATTENDANCE.VIEW', 'ATTENDANCE.MARK', 'ATTENDANCE.EDIT',
      'MARKS.VIEW', 'MARKS.ENTER', 'MARKS.EDIT'
  )
  AND NOT EXISTS (
      SELECT 1 FROM role_permissions rp
      WHERE rp.role_id = r.role_id AND rp.permission_id = p.permission_id
  )

-- Librarian: student view + library all
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.role_id, p.permission_id
FROM roles r
CROSS JOIN permissions p
WHERE r.role_name = 'Librarian'
  AND r.school_id IS NULL
  AND p.permission_code IN (
      'STUDENT_PROFILE.VIEW',
      'LIBRARY.VIEW', 'LIBRARY.ISSUE', 'LIBRARY.RETURN', 'LIBRARY.MANAGE'
  )
  AND NOT EXISTS (
      SELECT 1 FROM role_permissions rp
      WHERE rp.role_id = r.role_id AND rp.permission_id = p.permission_id
  )

PRINT 'Role-permission mappings seeded.'
GO


-- ============================================================
-- B. Payment Modes
--    Cash, Cheque — offline.  Online — gateway (is_online = 1).
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM payment_modes WHERE mode_name = 'Cash')
    INSERT INTO payment_modes (mode_name, school_id, is_online) VALUES ('Cash', NULL, 0)

IF NOT EXISTS (SELECT 1 FROM payment_modes WHERE mode_name = 'Cheque')
    INSERT INTO payment_modes (mode_name, school_id, is_online) VALUES ('Cheque', NULL, 0)

IF NOT EXISTS (SELECT 1 FROM payment_modes WHERE mode_name = 'Online')
    INSERT INTO payment_modes (mode_name, school_id, is_online) VALUES ('Online', NULL, 1)

PRINT 'Payment modes seeded.'
GO


-- ============================================================
-- C. Default Admin User  (school app login)
--    Uses the same @SchoolId and @PasswordHash from top of script.
--    Skips if username already exists.
-- ============================================================

-- NOTE: Variables declared above are batch-scoped.
-- Re-declare here since GO ends the previous batch.

DECLARE @SchoolId2     INT          = 1          -- ← must match the value at top of script
DECLARE @AdminPassword2 VARCHAR(100) = 'Admin@123' -- ← must match the value at top of script

DECLARE @HashBytes2    VARBINARY(32) = HASHBYTES('SHA2_256', CAST(@AdminPassword2 AS VARCHAR(100)))
DECLARE @HexStr2       VARCHAR(64)   = CONVERT(VARCHAR(64), @HashBytes2, 2)
DECLARE @PasswordHash2 VARCHAR(255)
SELECT @PasswordHash2 = CAST(N'' AS XML).value(
    'xs:base64Binary(xs:hexBinary(sql:variable("@HexStr2")))', 'VARCHAR(255)')

IF EXISTS (SELECT 1 FROM users WHERE username = 'admin')
BEGIN
    PRINT 'User "admin" already exists — skipping user insert.'
END
ELSE
BEGIN
    DECLARE @NewUserId INT

    INSERT INTO users (school_id, username, password_hash, full_name, status, created_by)
    VALUES (NULL, 'admin', @PasswordHash2, 'School Admin', 'Active', 'system')

    SET @NewUserId = CAST(SCOPE_IDENTITY() AS INT)

    -- Assign Principal role to this user for the specified school
    DECLARE @PrincipalRoleId INT
    SELECT @PrincipalRoleId = role_id FROM roles WHERE role_name = 'Principal' AND school_id IS NULL

    IF @PrincipalRoleId IS NOT NULL AND @SchoolId2 IS NOT NULL
        INSERT INTO user_roles (user_id, role_id, school_id)
        VALUES (@NewUserId, @PrincipalRoleId, @SchoolId2)

    PRINT 'Created school admin user: admin (Principal)'
    PRINT 'Password hash: ' + @PasswordHash2
END
GO


-- ============================================================
-- Done.
-- Login to the school app with:
--   Username: admin
--   Password: Admin@123  (or whatever you set above)
--   School:   the school whose school_id you set in @SchoolId
-- ============================================================
PRINT '--- Tenant seed complete ---'
GO
