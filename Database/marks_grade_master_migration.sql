-- ============================================================
-- marks_grade_master migration
--   Adds the marks_grade_master table (grade bands for a student's TOTAL
--   marks, per class + optional section per academic year — legacy
--   SAS_MarksGradeMaster) to an EXISTING tenant DB.
--   section_id NULL = whole class; a section's own set overrides it.
--   Idempotent (IF NOT EXISTS guards). Run once per tenant DB
--   (connect to ascent_group_{N} first).
-- ============================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;   -- required for the filtered unique indexes
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'marks_grade_master')
BEGIN
    CREATE TABLE marks_grade_master (
        id                INT           NOT NULL IDENTITY(1,1),
        academic_year_id  INT           NOT NULL,
        class_id          INT           NOT NULL,
        section_id        INT           NULL,        -- NULL = whole class (all sections)
        min_marks         DECIMAL(8,2)  NOT NULL,
        max_marks         DECIMAL(8,2)  NOT NULL,
        grade             VARCHAR(10)   NOT NULL,
        grade_point       VARCHAR(10)   NULL,
        description       VARCHAR(100)  NULL,
        status            VARCHAR(10)   NOT NULL DEFAULT 'Active',
        school_id         INT           NOT NULL,
        created_by        VARCHAR(100)  NULL,
        created_at        DATETIME      NOT NULL DEFAULT CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'India Standard Time' AS DATETIME),  -- IST (server runs US Eastern)
        CONSTRAINT PK_marks_grade_master PRIMARY KEY (id),
        CONSTRAINT FK_mgm_academic_year  FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id),
        CONSTRAINT FK_mgm_class          FOREIGN KEY (class_id)         REFERENCES classes(class_id),
        CONSTRAINT FK_mgm_section        FOREIGN KEY (section_id)       REFERENCES sections(section_id),
        CONSTRAINT CK_mgm_range          CHECK (min_marks >= 0 AND min_marks <= max_marks)
    );
    PRINT 'marks_grade_master table created.';
END
ELSE
    PRINT 'marks_grade_master table already exists — skipped.';
GO

-- Grade must be unique within one band set. Two filtered indexes because SQL Server
-- treats NULL section_ids as distinct (a single index would not guard the class-wide set).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_mgm_section' AND object_id = OBJECT_ID('marks_grade_master'))
    CREATE UNIQUE INDEX UQ_mgm_section ON marks_grade_master (school_id, academic_year_id, class_id, section_id, grade)
        WHERE section_id IS NOT NULL AND status = 'Active';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_mgm_class' AND object_id = OBJECT_ID('marks_grade_master'))
    CREATE UNIQUE INDEX UQ_mgm_class ON marks_grade_master (school_id, academic_year_id, class_id, grade)
        WHERE section_id IS NULL AND status = 'Active';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_mgm_lookup' AND object_id = OBJECT_ID('marks_grade_master'))
    CREATE INDEX IX_mgm_lookup ON marks_grade_master (school_id, academic_year_id, class_id, section_id, status);
GO

PRINT 'marks_grade_master migration complete.';
GO
