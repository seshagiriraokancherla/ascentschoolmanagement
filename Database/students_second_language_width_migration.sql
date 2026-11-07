-- ============================================================
-- students.second_language: VARCHAR(10) -> VARCHAR(25)
-- ------------------------------------------------------------
-- second_language was accidentally left at VARCHAR(10) while its siblings
-- first_language/third_language are VARCHAR(25). Since Phase 120 these three
-- fields are populated from a dropdown of the school's subjects
-- (subjects.subject_name, up to 30 chars), so any 2nd-language subject name
-- longer than 10 characters throws:
--   "String or binary data would be truncated ... column 'second_language'"
-- and the whole student save fails.
--
-- students may be a system-versioned TEMPORAL table (Phase 79) — altering an
-- EXISTING column (unlike adding a new one) does not auto-propagate to the
-- history table, so this must turn versioning off, alter both tables
-- identically, then turn it back on (decision #74). Schools that haven't run
-- students_temporal_migration.sql yet just get the plain ALTER.
--
-- Idempotent: safe to re-run; only alters if still the old narrower width.
-- Run once per existing tenant DB.
-- ============================================================

DECLARE @isTemporal BIT = CASE WHEN EXISTS (
    SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('dbo.students') AND temporal_type = 2
) THEN 1 ELSE 0 END;

IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'students' AND COLUMN_NAME = 'second_language'
      AND DATA_TYPE = 'varchar' AND CHARACTER_MAXIMUM_LENGTH < 25
)
BEGIN
    IF @isTemporal = 1
    BEGIN
        ALTER TABLE students SET (SYSTEM_VERSIONING = OFF);
        ALTER TABLE students         ALTER COLUMN second_language VARCHAR(25) NULL;
        ALTER TABLE students_history ALTER COLUMN second_language VARCHAR(25) NULL;
        ALTER TABLE students SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.students_history, DATA_CONSISTENCY_CHECK = ON));
        PRINT 'students.second_language widened to VARCHAR(25) (temporal versioning preserved).';
    END
    ELSE
    BEGIN
        ALTER TABLE students ALTER COLUMN second_language VARCHAR(25) NULL;
        PRINT 'students.second_language widened to VARCHAR(25).';
    END
END
ELSE
    PRINT 'students.second_language already VARCHAR(25) or wider — skipped.';
GO
