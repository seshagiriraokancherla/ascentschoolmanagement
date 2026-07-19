-- ============================================================
-- Homework list performance: indexes matching the assigned_date sort (Phase 94).
-- Replaces the obsolete IX_homework_class_due (indexed the retired due_date).
--   IX_homework_school_assigned — paged web admin list (school-scoped, newest first)
--   IX_homework_class_assigned  — mobile feed (class + section + status, newest first)
-- Run on each existing tenant DB (ascent_group_N). Idempotent.
-- ============================================================

USE ascent_group_1;   -- change to target DB before running
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_homework_class_due' AND object_id = OBJECT_ID('homework'))
BEGIN
    DROP INDEX IX_homework_class_due ON homework;
    PRINT 'Dropped obsolete IX_homework_class_due (indexed the retired due_date).';
END
ELSE
    PRINT 'IX_homework_class_due not present — skipped.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_homework_school_assigned' AND object_id = OBJECT_ID('homework'))
BEGIN
    CREATE INDEX IX_homework_school_assigned ON homework (school_id, assigned_date DESC, homework_id DESC);
    PRINT 'Created IX_homework_school_assigned.';
END
ELSE
    PRINT 'IX_homework_school_assigned already exists — skipped.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_homework_class_assigned' AND object_id = OBJECT_ID('homework'))
BEGIN
    CREATE INDEX IX_homework_class_assigned ON homework (class_id, school_id, status, assigned_date DESC);
    PRINT 'Created IX_homework_class_assigned.';
END
ELSE
    PRINT 'IX_homework_class_assigned already exists — skipped.';
GO
