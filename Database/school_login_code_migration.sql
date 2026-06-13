-- ============================================================
-- school_login_code_migration.sql
-- Run against ascent_master.
-- Adds login_code to school_groups — the 4-digit code parents enter
-- in the single (multi-school) Android app. Set per group via the control app.
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('school_groups') AND name = 'login_code'
)
BEGIN
    ALTER TABLE school_groups ADD login_code VARCHAR(10) NULL;
    PRINT 'Added login_code column to school_groups.';
END
ELSE
    PRINT 'login_code column already exists — skipped.';
GO

-- Unique among groups that have a code (NULLs allowed)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'UQ_school_groups_login_code'
)
BEGIN
    CREATE UNIQUE INDEX UQ_school_groups_login_code
        ON school_groups (login_code) WHERE login_code IS NOT NULL;
    PRINT 'Created unique filtered index UQ_school_groups_login_code.';
END
GO

-- Seed the demo group's code = 1000 for the Play/App Store reviewer path
-- (reviewer enters 1000 → demo school → mobile 9999999999 → OTP 123456).
UPDATE school_groups SET login_code = '1000'
WHERE subdomain = 'demo' AND (login_code IS NULL OR login_code = '');
GO
