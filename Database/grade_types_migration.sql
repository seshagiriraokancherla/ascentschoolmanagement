-- ============================================================
-- Grade Types Migration
-- Run on each existing tenant DB (ascent_group_N)
-- Adds: grade_types table (marks-to-grade bands per subject)
-- Migrated from legacy SAS_MarksGrade by AscentMigration's GradeTypesMigrator.
-- Idempotent — safe to re-run (guarded by IF NOT EXISTS).
-- ============================================================

USE ascent_group_1;   -- change to target DB before running
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'grade_types')
BEGIN
    CREATE TABLE grade_types (
        id            INT          NOT NULL IDENTITY(1,1),
        grade_name    VARCHAR(200) NULL,
        subject_id    INT          NULL,        -- FK → subjects; NULL when legacy subject not matched
        max_marks     FLOAT        NULL,
        min_marks     FLOAT        NULL,
        grade         VARCHAR(100) NULL,
        remarks       VARCHAR(200) NULL,
        status        VARCHAR(10)  NOT NULL DEFAULT 'Active',
        created_date  DATETIME     NOT NULL DEFAULT GETDATE(),
        created_by    VARCHAR(200) NULL,
        school_id     INT          NOT NULL,
        CONSTRAINT PK_grade_types         PRIMARY KEY (id),
        CONSTRAINT FK_grade_types_subject FOREIGN KEY (subject_id) REFERENCES subjects(subject_id)
    );

    PRINT 'grade_types table created.';
END
ELSE
    PRINT 'grade_types table already exists — skipped.';
GO
