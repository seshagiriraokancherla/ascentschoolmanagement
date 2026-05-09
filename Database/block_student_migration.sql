-- ============================================================
-- Migration: Block Student — adds blocked_reason to students
-- Run on: each ascent_group_{N} database
-- Safe to run multiple times (column existence check).
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('students') AND name = 'blocked_reason'
)
BEGIN
    ALTER TABLE students ADD blocked_reason VARCHAR(200) NULL;
    PRINT 'Added blocked_reason column to students.'
END
ELSE
    PRINT 'blocked_reason already exists — skipping.'
GO

PRINT '--- block_student_migration complete ---'
GO
