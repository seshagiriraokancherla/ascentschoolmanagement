-- ============================================================
-- student_marks + exam_master: exam_id link + activity marks
--   Adds, on an EXISTING tenant DB:
--     exam_master.activity_max_marks   (activity component config)
--     student_marks.exam_id            (FK → exam_master.id)
--     student_marks.activity_marks     (entered activity value)
--     student_marks.activity_max_marks (snapshot of the activity max)
--   All nullable — no backfill needed; existing marks entry is unaffected.
--   Idempotent. Run once per tenant DB.
-- ============================================================

-- exam_master.activity_max_marks
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'exam_master')
   AND NOT EXISTS (SELECT 1 FROM sys.columns
                   WHERE object_id = OBJECT_ID('exam_master') AND name = 'activity_max_marks')
BEGIN
    ALTER TABLE exam_master ADD activity_max_marks DECIMAL(6,2) NULL;
    PRINT 'exam_master.activity_max_marks added.';
END
ELSE
    PRINT 'exam_master.activity_max_marks already exists (or exam_master missing) — skipped.';
GO

-- student_marks.exam_id
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('student_marks') AND name = 'exam_id')
BEGIN
    ALTER TABLE student_marks ADD exam_id INT NULL;
    PRINT 'student_marks.exam_id added.';
END
ELSE
    PRINT 'student_marks.exam_id already exists — skipped.';
GO

-- student_marks.exam_id FK → exam_master(id) (separate batch: column must exist first)
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'exam_master')
   AND EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('student_marks') AND name = 'exam_id')
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_student_marks_exam_master')
BEGIN
    ALTER TABLE student_marks ADD CONSTRAINT FK_student_marks_exam_master
        FOREIGN KEY (exam_id) REFERENCES exam_master(id);
    PRINT 'FK_student_marks_exam_master added.';
END
ELSE
    PRINT 'FK_student_marks_exam_master already exists (or prerequisites missing) — skipped.';
GO

-- student_marks.activity_marks
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('student_marks') AND name = 'activity_marks')
BEGIN
    ALTER TABLE student_marks ADD activity_marks DECIMAL(6,2) NULL;
    PRINT 'student_marks.activity_marks added.';
END
ELSE
    PRINT 'student_marks.activity_marks already exists — skipped.';
GO

-- student_marks.activity_max_marks
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('student_marks') AND name = 'activity_max_marks')
BEGIN
    ALTER TABLE student_marks ADD activity_max_marks DECIMAL(6,2) NULL;
    PRINT 'student_marks.activity_max_marks added.';
END
ELSE
    PRINT 'student_marks.activity_max_marks already exists — skipped.';
GO
