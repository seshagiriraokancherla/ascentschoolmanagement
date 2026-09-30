-- ============================================================
-- ⚠ SUPERSEDED by marks_grade_scales_migration.sql (Phase 119), which drops both
--   the class-scoped marks_grade_master and exam_grade_scales created here and
--   replaces them with standalone grading scales. Kept for history — on a DB that
--   never ran this one, running it first is harmless (the next one drops its work).
-- ============================================================
-- marks card migration  (run AFTER marks_grade_master_migration.sql)
--   1. marks_grade_master gains scale_name — bands are grouped into named
--      scales (e.g. 'FA - out of 300') so one class can hold several scales for
--      exams with different totals. Existing rows become scale 'Default'.
--      The unique / lookup indexes are rebuilt to include scale_name.
--   2. New table exam_grade_scales — which scale grades the TOTAL on the marks
--      card, per exam type + class + academic year (set on Exam Master).
--   Idempotent (every step guarded). Run once per tenant DB
--   (connect to ascent_group_{N} first).
-- ============================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;   -- required for the filtered unique indexes
GO

-- ── 1. marks_grade_master.scale_name ────────────────────────────────────────
-- NOT NULL + DEFAULT back-fills existing rows with 'Default' in the same statement.
IF COL_LENGTH('marks_grade_master', 'scale_name') IS NULL
BEGIN
    ALTER TABLE marks_grade_master
        ADD scale_name VARCHAR(100) NOT NULL CONSTRAINT DF_mgm_scale_name DEFAULT 'Default';
    PRINT 'marks_grade_master.scale_name added (existing rows = ''Default'').';
END
ELSE
    PRINT 'marks_grade_master.scale_name already exists — skipped.';
GO

-- Rebuild the grade-uniqueness indexes so the same grade can exist once PER SCALE.
IF EXISTS (SELECT 1 FROM sys.indexes i
           WHERE i.name = 'UQ_mgm_section' AND i.object_id = OBJECT_ID('marks_grade_master')
             AND NOT EXISTS (SELECT 1 FROM sys.index_columns ic
                             JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                             WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND c.name = 'scale_name'))
    DROP INDEX UQ_mgm_section ON marks_grade_master;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_mgm_section' AND object_id = OBJECT_ID('marks_grade_master'))
    CREATE UNIQUE INDEX UQ_mgm_section ON marks_grade_master (school_id, academic_year_id, class_id, scale_name, section_id, grade)
        WHERE section_id IS NOT NULL AND status = 'Active';
GO

IF EXISTS (SELECT 1 FROM sys.indexes i
           WHERE i.name = 'UQ_mgm_class' AND i.object_id = OBJECT_ID('marks_grade_master')
             AND NOT EXISTS (SELECT 1 FROM sys.index_columns ic
                             JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                             WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND c.name = 'scale_name'))
    DROP INDEX UQ_mgm_class ON marks_grade_master;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_mgm_class' AND object_id = OBJECT_ID('marks_grade_master'))
    CREATE UNIQUE INDEX UQ_mgm_class ON marks_grade_master (school_id, academic_year_id, class_id, scale_name, grade)
        WHERE section_id IS NULL AND status = 'Active';
GO

IF EXISTS (SELECT 1 FROM sys.indexes i
           WHERE i.name = 'IX_mgm_lookup' AND i.object_id = OBJECT_ID('marks_grade_master')
             AND NOT EXISTS (SELECT 1 FROM sys.index_columns ic
                             JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                             WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND c.name = 'scale_name'))
    DROP INDEX IX_mgm_lookup ON marks_grade_master;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_mgm_lookup' AND object_id = OBJECT_ID('marks_grade_master'))
    CREATE INDEX IX_mgm_lookup ON marks_grade_master (school_id, academic_year_id, class_id, scale_name, section_id, status);
GO

-- ── 2. exam_grade_scales ───────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'exam_grade_scales')
BEGIN
    CREATE TABLE exam_grade_scales (
        id                INT           NOT NULL IDENTITY(1,1),
        academic_year_id  INT           NOT NULL,
        exam_type_id      INT           NOT NULL,
        class_id          INT           NOT NULL,
        scale_name        VARCHAR(100)  NOT NULL,
        school_id         INT           NOT NULL,
        created_by        VARCHAR(100)  NULL,
        created_at        DATETIME      NOT NULL DEFAULT CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'India Standard Time' AS DATETIME),  -- IST (server runs US Eastern)
        CONSTRAINT PK_exam_grade_scales   PRIMARY KEY (id),
        CONSTRAINT FK_egs_academic_year   FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id),
        CONSTRAINT FK_egs_exam_type       FOREIGN KEY (exam_type_id)     REFERENCES exam_types(exam_type_id),
        CONSTRAINT FK_egs_class           FOREIGN KEY (class_id)         REFERENCES classes(class_id),
        CONSTRAINT UQ_exam_grade_scales   UNIQUE (school_id, academic_year_id, exam_type_id, class_id)
    );
    PRINT 'exam_grade_scales table created.';
END
ELSE
    PRINT 'exam_grade_scales table already exists — skipped.';
GO

PRINT 'marks card migration complete.';
GO
