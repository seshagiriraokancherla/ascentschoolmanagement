-- ============================================================
-- Migration: Staff Attendance — Tenant DB tables
-- Run on: each ascent_group_{N} database
-- Safe to run multiple times (IF NOT EXISTS guards).
--
-- Adds:
--   1. staff            — employee master per school branch
--   2. staff_attendance — daily attendance per staff member
-- ============================================================

-- ============================================================
-- 1. staff
--    Employee master.  designation and department are free-text.
--    employee_code is optional but unique within a school when set.
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'staff')
BEGIN
    CREATE TABLE staff (
        staff_id        INT             NOT NULL IDENTITY(1,1),
        staff_name      VARCHAR(100)    NOT NULL,
        employee_code   VARCHAR(20)     NULL,
        designation     VARCHAR(50)     NULL,
        department      VARCHAR(50)     NULL,
        mobile          VARCHAR(20)     NULL,
        email           VARCHAR(100)    NULL,
        join_date       DATE            NULL,
        school_id       INT             NOT NULL,
        status          VARCHAR(10)     NOT NULL DEFAULT 'Active',   -- Active / Inactive
        created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
        updated_at      DATETIME        NULL,
        CONSTRAINT PK_staff PRIMARY KEY (staff_id)
    );

    CREATE INDEX IX_staff_school_status
        ON staff (school_id, status);

    PRINT 'Created staff.'
END
ELSE
    PRINT 'staff already exists — skipping.'
GO


-- ============================================================
-- 2. staff_attendance
--    One row per staff member per date.
--    status: Present / Absent / Late / HalfDay / OnLeave
--    Unique constraint prevents duplicate entries per day.
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'staff_attendance')
BEGIN
    CREATE TABLE staff_attendance (
        attendance_id   INT             NOT NULL IDENTITY(1,1),
        staff_id        INT             NOT NULL,
        attendance_date DATE            NOT NULL,
        status          VARCHAR(10)     NOT NULL DEFAULT 'Present',
        remarks         VARCHAR(100)    NULL,
        school_id       INT             NOT NULL,
        marked_by       VARCHAR(100)    NOT NULL,
        marked_at       DATETIME        NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_staff_attendance              PRIMARY KEY (attendance_id),
        CONSTRAINT UQ_staff_attendance_date         UNIQUE (staff_id, attendance_date, school_id),
        CONSTRAINT FK_staff_attendance_staff        FOREIGN KEY (staff_id) REFERENCES staff(staff_id)
    );

    CREATE INDEX IX_staff_attendance_school_date
        ON staff_attendance (school_id, attendance_date);

    PRINT 'Created staff_attendance.'
END
ELSE
    PRINT 'staff_attendance already exists — skipping.'
GO

-- ============================================================
-- 3. staff_advances
--    Records each advance / loan given to a staff member.
--    status: Active / Cancelled
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'staff_advances')
BEGIN
    CREATE TABLE staff_advances (
        advance_id      INT             NOT NULL IDENTITY(1,1),
        staff_id        INT             NOT NULL,
        advance_date    DATE            NOT NULL,
        amount          DECIMAL(12,2)   NOT NULL,
        purpose         VARCHAR(200)    NULL,
        remarks         VARCHAR(200)    NULL,
        school_id       INT             NOT NULL,
        status          VARCHAR(10)     NOT NULL DEFAULT 'Active',   -- Active / Cancelled
        created_by      VARCHAR(100)    NOT NULL,
        created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_staff_advances            PRIMARY KEY (advance_id),
        CONSTRAINT FK_staff_advances_staff      FOREIGN KEY (staff_id) REFERENCES staff(staff_id)
    );

    CREATE INDEX IX_staff_advances_staff
        ON staff_advances (staff_id, school_id, status);

    PRINT 'Created staff_advances.'
END
ELSE
    PRINT 'staff_advances already exists — skipping.'
GO


-- ============================================================
-- 4. staff_advance_repayments
--    Records each repayment against an advance.
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'staff_advance_repayments')
BEGIN
    CREATE TABLE staff_advance_repayments (
        repayment_id    INT             NOT NULL IDENTITY(1,1),
        advance_id      INT             NOT NULL,
        staff_id        INT             NOT NULL,
        repayment_date  DATE            NOT NULL,
        amount          DECIMAL(12,2)   NOT NULL,
        remarks         VARCHAR(200)    NULL,
        school_id       INT             NOT NULL,
        created_by      VARCHAR(100)    NOT NULL,
        created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_staff_advance_repayments          PRIMARY KEY (repayment_id),
        CONSTRAINT FK_staff_advance_repayments_advance  FOREIGN KEY (advance_id) REFERENCES staff_advances(advance_id),
        CONSTRAINT FK_staff_advance_repayments_staff    FOREIGN KEY (staff_id)   REFERENCES staff(staff_id)
    );

    CREATE INDEX IX_staff_advance_repayments_advance
        ON staff_advance_repayments (advance_id);

    PRINT 'Created staff_advance_repayments.'
END
ELSE
    PRINT 'staff_advance_repayments already exists — skipping.'
GO

-- ============================================================
-- 5. staff_salary_components  (salary template per staff member)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'staff_salary_components')
BEGIN
    CREATE TABLE staff_salary_components (
        component_id    INT             NOT NULL IDENTITY(1,1),
        staff_id        INT             NOT NULL,
        component_name  VARCHAR(80)     NOT NULL,
        component_type  VARCHAR(10)     NOT NULL,   -- 'Earning' or 'Deduction'
        amount          DECIMAL(12,2)   NOT NULL,
        display_order   INT             NOT NULL DEFAULT 0,
        school_id       INT             NOT NULL,
        is_active       BIT             NOT NULL DEFAULT 1,
        CONSTRAINT PK_staff_salary_components           PRIMARY KEY (component_id),
        CONSTRAINT FK_staff_salary_components_staff     FOREIGN KEY (staff_id) REFERENCES staff(staff_id),
        CONSTRAINT UQ_staff_salary_components           UNIQUE (staff_id, component_name, school_id)
    );

    CREATE INDEX IX_staff_salary_components_staff
        ON staff_salary_components (staff_id, school_id);

    PRINT 'Created staff_salary_components.'
END
ELSE
    PRINT 'staff_salary_components already exists — skipping.'
GO


-- ============================================================
-- 6. staff_salaries  (monthly salary header per staff)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'staff_salaries')
BEGIN
    CREATE TABLE staff_salaries (
        salary_id           INT             NOT NULL IDENTITY(1,1),
        staff_id            INT             NOT NULL,
        month               INT             NOT NULL,
        year                INT             NOT NULL,
        gross_earnings      DECIMAL(12,2)   NOT NULL DEFAULT 0,
        total_deductions    DECIMAL(12,2)   NOT NULL DEFAULT 0,
        advance_deducted    DECIMAL(12,2)   NOT NULL DEFAULT 0,
        net_salary          DECIMAL(12,2)   NOT NULL DEFAULT 0,
        school_id           INT             NOT NULL,
        status              VARCHAR(10)     NOT NULL DEFAULT 'Draft',  -- Draft/Paid/Cancelled
        remarks             VARCHAR(200)    NULL,
        processed_by        VARCHAR(100)    NOT NULL,
        processed_at        DATETIME        NOT NULL DEFAULT GETDATE(),
        paid_date           DATE            NULL,
        CONSTRAINT PK_staff_salaries        PRIMARY KEY (salary_id),
        CONSTRAINT FK_staff_salaries_staff  FOREIGN KEY (staff_id) REFERENCES staff(staff_id),
        CONSTRAINT UQ_staff_salaries        UNIQUE (staff_id, month, year, school_id)
    );

    CREATE INDEX IX_staff_salaries_month_year
        ON staff_salaries (month, year, school_id, status);

    PRINT 'Created staff_salaries.'
END
ELSE
    PRINT 'staff_salaries already exists — skipping.'
GO


-- ============================================================
-- 7. staff_salary_items  (snapshot of components at processing time)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'staff_salary_items')
BEGIN
    CREATE TABLE staff_salary_items (
        item_id         INT             NOT NULL IDENTITY(1,1),
        salary_id       INT             NOT NULL,
        component_name  VARCHAR(80)     NOT NULL,
        component_type  VARCHAR(10)     NOT NULL,
        amount          DECIMAL(12,2)   NOT NULL,
        CONSTRAINT PK_staff_salary_items            PRIMARY KEY (item_id),
        CONSTRAINT FK_staff_salary_items_salary     FOREIGN KEY (salary_id) REFERENCES staff_salaries(salary_id)
    );

    CREATE INDEX IX_staff_salary_items_salary
        ON staff_salary_items (salary_id);

    PRINT 'Created staff_salary_items.'
END
ELSE
    PRINT 'staff_salary_items already exists — skipping.'
GO

PRINT '--- staff_tables_migration complete ---'
GO
