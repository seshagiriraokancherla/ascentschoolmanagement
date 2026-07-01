-- ============================================================
-- Exam Master Migration
-- Run on each existing tenant DB (ascent_group_N)
-- Adds: exam_master table (per-class/subject exam definitions)
-- Migrated from legacy SAS_ExamNam by AscentMigration's ExamMasterMigrator.
-- Depends on: exam_types, classes, academic_years, subjects, grade_types.
-- Idempotent — safe to re-run (guarded by IF NOT EXISTS).
-- ============================================================

USE ascent_group_1;   -- change to target DB before running
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'exam_master')
BEGIN
    CREATE TABLE exam_master (
        id                INT          NOT NULL IDENTITY(1,1),
        exam_type_id      INT          NULL,        -- FK → exam_types (by name)
        class_id          INT          NULL,        -- FK → classes
        exam_total_marks  INT          NULL,
        exam_min_marks    INT          NULL,
        subject_min_marks INT          NULL,
        sub_max_marks     INT          NULL,
        exam_remarks      VARCHAR(300) NULL,
        academic_year_id  INT          NULL,        -- FK → academic_years
        subject_id        INT          NULL,        -- FK → subjects
        exam_status       VARCHAR(10)  NOT NULL DEFAULT 'Active',
        created_by        VARCHAR(200) NULL,
        created_date      DATETIME     NOT NULL DEFAULT GETDATE(),
        school_id         INT          NOT NULL,
        exam_category     VARCHAR(200) NULL,
        exam_date         DATE         NULL,
        grade_type_id     INT          NULL,        -- FK → grade_types
        CONSTRAINT PK_exam_master            PRIMARY KEY (id),
        CONSTRAINT FK_exam_master_exam_type  FOREIGN KEY (exam_type_id)     REFERENCES exam_types(exam_type_id),
        CONSTRAINT FK_exam_master_class      FOREIGN KEY (class_id)         REFERENCES classes(class_id),
        CONSTRAINT FK_exam_master_year       FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id),
        CONSTRAINT FK_exam_master_subject    FOREIGN KEY (subject_id)       REFERENCES subjects(subject_id),
        CONSTRAINT FK_exam_master_grade_type FOREIGN KEY (grade_type_id)    REFERENCES grade_types(id)
    );

    PRINT 'exam_master table created.';
END
ELSE
    PRINT 'exam_master table already exists — skipped.';
GO
