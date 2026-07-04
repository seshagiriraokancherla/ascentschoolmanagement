-- ============================================================
-- Users ↔ Staff link migration
-- Run on each existing tenant DB (ascent_group_N)
-- The users.employee_id column already exists; this only adds the
-- FK to staff + a filtered unique index (one login per staff).
-- Idempotent — safe to re-run.
-- ============================================================

USE ascent_group_1;   -- change to target DB before running
GO

-- FK: users.employee_id → staff.staff_id
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_users_staff')
BEGIN
    ALTER TABLE users
        ADD CONSTRAINT FK_users_staff FOREIGN KEY (employee_id) REFERENCES staff(staff_id);
    PRINT 'FK_users_staff added.';
END
ELSE
    PRINT 'FK_users_staff already exists — skipped.';
GO

-- One login per staff member (filtered so multiple NULLs are allowed, e.g. the admin)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_users_employee')
BEGIN
    CREATE UNIQUE INDEX UQ_users_employee
        ON users (employee_id)
        WHERE employee_id IS NOT NULL;
    PRINT 'UQ_users_employee index created.';
END
ELSE
    PRINT 'UQ_users_employee index already exists — skipped.';
GO
