-- ============================================================
-- Marks Grade Master → standalone grading scales
--   Replaces the class/section/year-scoped grade bands with a reusable library:
--     marks_grade_master        = the scale header (name only, school-wide)
--     marks_grade_master_bands  = that scale's bands
--   Each exam row now points at a scale (exam_master.marks_grade_master_id), the
--   same way it already points at a grade type for the SUBJECT grade.
--   Retires exam_grade_scales (the per exam+class scale choice).
--
--   Run AFTER marks_grade_master_migration.sql + marks_card_migration.sql.
--   ⚠ The old class-scoped marks_grade_master is DROPPED — confirmed with the
--     user that no school had entered bands yet. Check before running:
--        SELECT (SELECT COUNT(*) FROM marks_grade_master) AS grade_rows,
--               (SELECT COUNT(*) FROM exam_grade_scales)  AS exam_choices;
--   Idempotent (every step guarded). Run once per tenant DB
--   (connect to ascent_group_{N} first).
-- ============================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ── 1. Retire the old tables ───────────────────────────────────────────────
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'exam_grade_scales')
BEGIN
    DROP TABLE exam_grade_scales;
    PRINT 'exam_grade_scales dropped (exam now points at a scale directly).';
END
GO

-- The old marks_grade_master is class/section/year-scoped — recognise it by the
-- class_id column and drop it so the new header table can take the name.
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'marks_grade_master')
   AND COL_LENGTH('marks_grade_master', 'class_id') IS NOT NULL
BEGIN
    DROP TABLE marks_grade_master;
    PRINT 'old class-scoped marks_grade_master dropped.';
END
GO

-- ── 2. Scale header ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'marks_grade_master')
BEGIN
    CREATE TABLE marks_grade_master (
        id          INT           NOT NULL IDENTITY(1,1),
        scale_name  VARCHAR(100)  NOT NULL,      -- e.g. 'Out of 300'
        description VARCHAR(200)  NULL,
        status      VARCHAR(10)   NOT NULL DEFAULT 'Active',
        school_id   INT           NOT NULL,
        created_by  VARCHAR(100)  NULL,
        created_at  DATETIME      NOT NULL DEFAULT CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'India Standard Time' AS DATETIME),  -- IST (server runs US Eastern)
        CONSTRAINT PK_marks_grade_master PRIMARY KEY (id),
        CONSTRAINT UQ_marks_grade_master UNIQUE (school_id, scale_name)
    );
    PRINT 'marks_grade_master (scale header) created.';
END
ELSE
    PRINT 'marks_grade_master (scale header) already exists — skipped.';
GO

-- ── 3. Scale bands ─────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'marks_grade_master_bands')
BEGIN
    CREATE TABLE marks_grade_master_bands (
        id          INT           NOT NULL IDENTITY(1,1),
        scale_id    INT           NOT NULL,
        min_marks   DECIMAL(8,2)  NOT NULL,      -- raw marks, not %
        max_marks   DECIMAL(8,2)  NOT NULL,
        grade       VARCHAR(10)   NOT NULL,
        grade_point VARCHAR(10)   NULL,          -- text, e.g. A / B
        description VARCHAR(100)  NULL,
        school_id   INT           NOT NULL,
        created_by  VARCHAR(100)  NULL,
        created_at  DATETIME      NOT NULL DEFAULT CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'India Standard Time' AS DATETIME),  -- IST
        CONSTRAINT PK_mgm_bands        PRIMARY KEY (id),
        CONSTRAINT FK_mgm_bands_scale  FOREIGN KEY (scale_id) REFERENCES marks_grade_master(id),
        CONSTRAINT UQ_mgm_bands_grade  UNIQUE (scale_id, grade),
        CONSTRAINT CK_mgm_bands_range  CHECK (min_marks >= 0 AND min_marks <= max_marks)
    );
    CREATE INDEX IX_mgm_bands_scale ON marks_grade_master_bands (scale_id);
    PRINT 'marks_grade_master_bands created.';
END
ELSE
    PRINT 'marks_grade_master_bands already exists — skipped.';
GO

-- ── 4. exam_master → scale ─────────────────────────────────────────────────
IF COL_LENGTH('exam_master', 'marks_grade_master_id') IS NULL
BEGIN
    ALTER TABLE exam_master ADD marks_grade_master_id INT NULL;
    PRINT 'exam_master.marks_grade_master_id added.';
END
ELSE
    PRINT 'exam_master.marks_grade_master_id already exists — skipped.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_exam_master_grade_scale')
BEGIN
    ALTER TABLE exam_master WITH CHECK
        ADD CONSTRAINT FK_exam_master_grade_scale
        FOREIGN KEY (marks_grade_master_id) REFERENCES marks_grade_master(id);
    PRINT 'FK_exam_master_grade_scale added.';
END
GO

PRINT 'marks grade scales migration complete.';
GO
