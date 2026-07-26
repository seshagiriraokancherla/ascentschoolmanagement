-- ============================================================
-- students → system-versioned temporal table (audit trail)
-- Turns the students table into a SQL Server temporal table so EVERY
-- change (from any path: web form, bulk import, sync tool, promote,
-- block/detain, change-section, transport/hostel update) is snapshotted
-- automatically into dbo.students_history — no application code.
--
-- WHO/WHEN: each history row carries students.updated_by / updated_at
-- (Phase 78) plus the system period (valid_from/valid_to).
--
-- REQUIRES SQL Server 2016+ (any edition, incl. Web/Express).
-- Idempotent — safe to re-run.
--
-- To view history:
--   SELECT * FROM students FOR SYSTEM_TIME ALL
--   WHERE student_id = @id ORDER BY valid_from;
--
-- To later turn it OFF (e.g. before a schema change):
--   ALTER TABLE students SET (SYSTEM_VERSIONING = OFF);
--   -- make changes to students AND students_history, then re-enable.
-- ============================================================

USE ascent_group_1;   -- change to target DB before running
GO

-- 1. Add the system-time PERIOD columns (hidden so SELECT * / existing
--    queries are unaffected). Existing rows get valid_from = now.
IF NOT EXISTS (SELECT 1 FROM sys.periods WHERE object_id = OBJECT_ID('dbo.students'))
BEGIN
    ALTER TABLE students ADD
        valid_from DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL
            CONSTRAINT DF_students_valid_from DEFAULT SYSUTCDATETIME(),
        valid_to   DATETIME2 GENERATED ALWAYS AS ROW END   HIDDEN NOT NULL
            CONSTRAINT DF_students_valid_to   DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
        PERIOD FOR SYSTEM_TIME (valid_from, valid_to);
    PRINT 'students: PERIOD columns added.';
END
ELSE
    PRINT 'students: PERIOD columns already exist — skipped.';
GO

-- 2. Turn on system versioning (auto-creates dbo.students_history).
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('dbo.students') AND temporal_type = 2)
BEGIN
    ALTER TABLE students SET
        (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.students_history, DATA_CONSISTENCY_CHECK = ON));
    PRINT 'students: SYSTEM_VERSIONING ON (history = dbo.students_history).';
END
ELSE
    PRINT 'students: SYSTEM_VERSIONING already ON — skipped.';
GO
