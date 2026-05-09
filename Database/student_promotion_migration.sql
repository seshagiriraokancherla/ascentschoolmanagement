-- ============================================================
-- student_promotion_migration.sql
-- Run on each existing tenant DB (ascent_group_{N})
-- Enables multi-year student records (one row per student per academic year)
-- ============================================================

-- Change the USE statement to the target tenant DB name:
-- USE ascent_group_1;
-- GO

-- 1. Add UNIQUE constraint for (admission_no, school_id, academic_year_id)
--    This replaces the implicit single-year uniqueness assumption.
--    NULL academic_year_id rows are excluded from uniqueness by SQL Server (NULLs not equal).
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UQ_students_admno_school_year'
      AND object_id = OBJECT_ID('students')
)
BEGIN
    CREATE UNIQUE INDEX UQ_students_admno_school_year
        ON students (admission_no, school_id, academic_year_id)
        WHERE admission_no IS NOT NULL AND academic_year_id IS NOT NULL;
    PRINT 'UQ_students_admno_school_year index created.';
END
ELSE
    PRINT 'UQ_students_admno_school_year already exists — skipping.';
GO
