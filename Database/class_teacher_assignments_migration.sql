-- ============================================================
-- Feature 2 prerequisite: class -> teacher assignment.
-- Adds class_teacher_assignments, which maps a class (+ optional section) to
-- its teacher(s) for an academic year. Drives parent -> teacher message routing.
--   section_id NULL = the assignment covers the whole class (all sections).
--   Several teachers may share one class+section; any of them can reply.
-- Run on each existing tenant DB (ascent_group_N). Idempotent.
-- ============================================================

USE ascent_group_1;   -- change to target DB before running
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'class_teacher_assignments')
BEGIN
    CREATE TABLE class_teacher_assignments (
        assignment_id    INT          NOT NULL IDENTITY(1,1),
        academic_year_id INT          NOT NULL,
        class_id         INT          NOT NULL,
        section_id       INT          NULL,        -- NULL = whole class (all sections)
        user_id          INT          NOT NULL,    -- teacher login (users.user_id)
        school_id        INT          NOT NULL,
        created_by       VARCHAR(150) NULL,
        created_at       DATETIME     NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_class_teacher_assignments PRIMARY KEY (assignment_id),
        CONSTRAINT FK_cta_academic_year FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id),
        CONSTRAINT FK_cta_class         FOREIGN KEY (class_id)         REFERENCES classes(class_id),
        CONSTRAINT FK_cta_section       FOREIGN KEY (section_id)       REFERENCES sections(section_id),
        CONSTRAINT FK_cta_user          FOREIGN KEY (user_id)          REFERENCES users(user_id)
    );
    PRINT 'class_teacher_assignments table created.';
END
ELSE
    PRINT 'class_teacher_assignments table already exists — skipped.';
GO

-- One row per teacher per class+section. TWO filtered indexes, not one: SQL Server
-- treats NULLs as distinct in a plain unique index, so a single index would let the
-- same teacher be assigned to the same class twice whenever section_id IS NULL.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_cta_section')
BEGIN
    CREATE UNIQUE INDEX UQ_cta_section ON class_teacher_assignments
        (school_id, academic_year_id, class_id, section_id, user_id) WHERE section_id IS NOT NULL;
    PRINT 'UQ_cta_section index created.';
END
ELSE
    PRINT 'UQ_cta_section already exists — skipped.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_cta_class')
BEGIN
    CREATE UNIQUE INDEX UQ_cta_class ON class_teacher_assignments
        (school_id, academic_year_id, class_id, user_id) WHERE section_id IS NULL;
    PRINT 'UQ_cta_class index created.';
END
ELSE
    PRINT 'UQ_cta_class already exists — skipped.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_cta_lookup')
BEGIN
    CREATE INDEX IX_cta_lookup ON class_teacher_assignments (school_id, academic_year_id, class_id, section_id);
    PRINT 'IX_cta_lookup index created.';
END
ELSE
    PRINT 'IX_cta_lookup already exists — skipped.';
GO
