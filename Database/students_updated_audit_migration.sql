-- ============================================================
-- students updated_by / updated_at audit columns
-- Run on each existing tenant DB (ascent_group_N).
-- Adds updated_by + updated_at to students (set on every edit).
-- Idempotent — safe to re-run.
-- ============================================================

USE ascent_group_1;   -- change to target DB before running
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('students') AND name = 'updated_by')
BEGIN
    ALTER TABLE students ADD updated_by VARCHAR(25) NULL;
    PRINT 'students.updated_by added.';
END
ELSE
    PRINT 'students.updated_by already exists — skipped.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('students') AND name = 'updated_at')
BEGIN
    ALTER TABLE students ADD updated_at DATETIME NULL;
    PRINT 'students.updated_at added.';
END
ELSE
    PRINT 'students.updated_at already exists — skipped.';
GO
