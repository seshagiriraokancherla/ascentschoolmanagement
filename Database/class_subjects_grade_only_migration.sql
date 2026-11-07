-- ============================================================
-- class_subjects.grade_only BIT NOT NULL DEFAULT 0
-- ------------------------------------------------------------
-- Marks a class's subject as "grade only" (e.g. GK): on the marks card it is
-- printed in a separate Subject | Grade table instead of with marks, and it is
-- EXCLUDED from the card's total, total grade and rank, and from the Toppers /
-- Failed Students reports. Marks are still entered as usual — the grade comes
-- from the Subject Grade Master chosen on the subject's Exam Master row.
--
-- Load-bearing: the API selects this column — run BEFORE deploying the API.
-- Idempotent. Run once per existing tenant DB.
-- ============================================================

IF COL_LENGTH('class_subjects', 'grade_only') IS NULL
BEGIN
    ALTER TABLE class_subjects ADD grade_only BIT NOT NULL
        CONSTRAINT DF_class_subjects_grade_only DEFAULT 0;
    PRINT 'class_subjects.grade_only added.';
END
ELSE
    PRINT 'class_subjects.grade_only already exists — skipped.';
GO
