-- ============================================================
-- class_subjects.language_slot TINYINT NULL
-- ------------------------------------------------------------
-- Links a placeholder subject (e.g. "Second Language") to the student's own
-- first / second / third language (students.first_language / second_language /
-- third_language). Values 1, 2, 3 — NULL = not a language subject.
-- The marks card then prints "Second Language (Hindi)" for each student.
-- Set it in Exams → Class Subjects → "Student Language".
--
-- Load-bearing: the API selects this column — run BEFORE deploying the API.
-- Idempotent. Run once per existing tenant DB.
-- ============================================================

IF COL_LENGTH('class_subjects', 'language_slot') IS NULL
BEGIN
    ALTER TABLE class_subjects ADD language_slot TINYINT NULL;
    PRINT 'class_subjects.language_slot added.';
END
ELSE
    PRINT 'class_subjects.language_slot already exists — skipped.';
GO
