-- ============================================================
-- Migration: Mobile App — Master DB tables
-- Run on: ascent_master
-- Safe to run multiple times (IF NOT EXISTS guards).
--
-- Adds:
--   1. parent_accounts          — parent login credentials (PIN-based)
--   2. parent_children          — links parent → student across any tenant DB
--   3. parent_refresh_tokens    — sliding-expiry refresh tokens for parents
-- ============================================================

USE ascent_master;
GO


-- ============================================================
-- 1. parent_accounts
--    One row per parent/guardian.
--    A parent can have children in different school groups.
--    PIN is SHA-256 + Base64 hashed (same as JwtHelper.HashRefreshToken).
--    Either mobile or email must be provided (used as login identifier).
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'parent_accounts')
BEGIN
    CREATE TABLE parent_accounts (
        parent_id       INT             NOT NULL IDENTITY(1,1),
        full_name       VARCHAR(100)    NOT NULL,
        mobile          VARCHAR(20)     NULL,
        email           VARCHAR(100)    NULL,
        pin_hash        VARCHAR(255)    NOT NULL,
        otp_code        VARCHAR(10)     NULL,        -- for PIN reset
        otp_expires_at  DATETIME        NULL,
        is_active       BIT             NOT NULL DEFAULT 1,
        last_login_at   DATETIME        NULL,
        created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_parent_accounts           PRIMARY KEY (parent_id),
        CONSTRAINT UQ_parent_accounts_mobile    UNIQUE (mobile)
    );
    -- Filtered unique index: allows multiple NULLs, enforces uniqueness only for non-NULL emails
    CREATE UNIQUE INDEX UX_parent_accounts_email ON parent_accounts(email) WHERE email IS NOT NULL;
    PRINT 'Created parent_accounts.'
END
ELSE
BEGIN
    -- Fix existing table if it still has the bad UNIQUE constraint on email
    IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_parent_accounts_email')
    BEGIN
        ALTER TABLE parent_accounts DROP CONSTRAINT UQ_parent_accounts_email;
        PRINT 'Dropped UQ_parent_accounts_email constraint.'
    END
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_parent_accounts_email')
    BEGIN
        CREATE UNIQUE INDEX UX_parent_accounts_email ON parent_accounts(email) WHERE email IS NOT NULL;
        PRINT 'Created filtered index UX_parent_accounts_email.'
    END
    PRINT 'parent_accounts already exists — constraint fixed.'
END
GO


-- ============================================================
-- 2. parent_children
--    Links a parent account to one or more students.
--    A student can belong to any tenant DB (cross-group supported).
--    db_name + school_id stored here so parent requests can route
--    to the correct tenant DB without a lookup.
--    student_name / class_name cached for child-selector screen.
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'parent_children')
BEGIN
    CREATE TABLE parent_children (
        link_id         INT             NOT NULL IDENTITY(1,1),
        parent_id       INT             NOT NULL,
        student_id      BIGINT          NOT NULL,   -- PK in tenant DB students table
        group_id        INT             NOT NULL,   -- FK → school_groups.group_id
        db_name         VARCHAR(100)    NOT NULL,   -- e.g. ascent_group_1 / EducareDemoDb
        school_id       INT             NOT NULL,
        student_name    VARCHAR(105)    NULL,        -- cached — update on profile change
        class_name      VARCHAR(50)     NULL,        -- cached — update on class change
        admission_no    VARCHAR(20)     NULL,        -- cached for display
        is_active       BIT             NOT NULL DEFAULT 1,
        linked_at       DATETIME        NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_parent_children           PRIMARY KEY (link_id),
        CONSTRAINT UQ_parent_children_pair      UNIQUE (parent_id, student_id, group_id),
        CONSTRAINT FK_parent_children_parent    FOREIGN KEY (parent_id) REFERENCES parent_accounts(parent_id),
        CONSTRAINT FK_parent_children_group     FOREIGN KEY (group_id)  REFERENCES school_groups(group_id)
    );
    PRINT 'Created parent_children.'
END
ELSE
    PRINT 'parent_children already exists — skipping.'
GO


-- ============================================================
-- 3. parent_refresh_tokens
--    Sliding-expiry refresh tokens for parent mobile sessions.
--    Same pattern as control_users refresh_tokens.
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'parent_refresh_tokens')
BEGIN
    CREATE TABLE parent_refresh_tokens (
        token_id        INT             NOT NULL IDENTITY(1,1),
        parent_id       INT             NOT NULL,
        token_hash      VARCHAR(255)    NOT NULL,
        expires_at      DATETIME        NOT NULL,
        created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
        revoked_at      DATETIME        NULL,
        replaced_by     VARCHAR(255)    NULL,
        CONSTRAINT PK_parent_refresh_tokens         PRIMARY KEY (token_id),
        CONSTRAINT FK_parent_refresh_tokens_parent  FOREIGN KEY (parent_id) REFERENCES parent_accounts(parent_id)
    );
    PRINT 'Created parent_refresh_tokens.'
END
ELSE
    PRINT 'parent_refresh_tokens already exists — skipping.'
GO

PRINT '--- mobile_master_migration complete ---'
GO
