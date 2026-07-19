-- ============================================================
-- Homework "due date" retired. The column is kept (legacy rows keep their
-- values) but made NULLABLE so new homework can be saved without one.
-- Run on each existing tenant DB (ascent_group_N). Idempotent.
-- ============================================================

USE ascent_group_1;   -- change to target DB before running
GO

IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('homework') AND name = 'due_date' AND is_nullable = 0)
BEGIN
    ALTER TABLE homework ALTER COLUMN due_date DATE NULL;
    PRINT 'homework.due_date set to NULL-able.';
END
ELSE
    PRINT 'homework.due_date already nullable (or absent) — skipped.';
GO
