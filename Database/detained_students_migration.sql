-- ============================================================
-- Migration: Detained Students
-- Run on: each ascent_group_{N} database
-- Safe to run multiple times (column existence checks).
--
-- Adds:
--   is_detained     BIT         — 1 = student will not be promoted
--   detained_reason VARCHAR(200) — mandatory when is_detained = 1
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('students') AND name = 'is_detained'
)
BEGIN
    ALTER TABLE students ADD is_detained BIT NOT NULL DEFAULT 0;
    PRINT 'Added is_detained column to students.'
END
ELSE
    PRINT 'is_detained already exists — skipping.'
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('students') AND name = 'detained_reason'
)
BEGIN
    ALTER TABLE students ADD detained_reason VARCHAR(200) NULL;
    PRINT 'Added detained_reason column to students.'
END
ELSE
    PRINT 'detained_reason already exists — skipping.'
GO

PRINT '--- detained_students_migration complete ---'
GO
