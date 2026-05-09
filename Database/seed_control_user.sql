-- ============================================================
-- Seed: Control App User
-- Run on: ascent_master
-- Purpose: Creates the first login for the Ascent control panel.
--
-- Change @Password before running.
-- Roles: super_admin | support | billing
-- ============================================================

USE ascent_master;
GO

DECLARE @FullName  VARCHAR(100) = 'Super Admin'
DECLARE @Username  VARCHAR(50)  = 'admin'
DECLARE @Password  VARCHAR(100) = 'Admin@123'      -- ← change this
DECLARE @Role      VARCHAR(20)  = 'super_admin'

-- Hash the password using SHA-256 + Base64, matching .NET:
--   SHA256.ComputeHash(Encoding.UTF8.GetBytes(password))  →  Convert.ToBase64String(bytes)
-- NOTE: Using VARCHAR (not NVARCHAR) so SQL Server encodes as single-byte (UTF-8 compatible
--       for ASCII passwords). For passwords with non-ASCII chars, hash in .NET instead.
DECLARE @HashBytes  VARBINARY(32) = HASHBYTES('SHA2_256', CAST(@Password AS VARCHAR(100)))
DECLARE @HexStr     VARCHAR(64)   = CONVERT(VARCHAR(64), @HashBytes, 2)  -- hex, no 0x prefix
DECLARE @PasswordHash VARCHAR(255)
SELECT  @PasswordHash = CAST(N'' AS XML).value(
    'xs:base64Binary(xs:hexBinary(sql:variable("@HexStr")))', 'VARCHAR(255)')

-- Prevent duplicate username
IF EXISTS (SELECT 1 FROM control_users WHERE username = @Username)
BEGIN
    PRINT 'User "' + @Username + '" already exists — skipping insert.'
END
ELSE
BEGIN
    INSERT INTO control_users (full_name, username, password_hash, role, status, created_by)
    VALUES (@FullName, @Username, @PasswordHash, @Role, 'Active', 'system')

    PRINT 'Created control user: ' + @Username + ' (' + @Role + ')'
    PRINT 'Password hash: ' + @PasswordHash
END
GO
