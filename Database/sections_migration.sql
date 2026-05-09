-- ============================================================
-- sections_migration.sql
-- Run on each existing tenant DB (ascent_group_{N})
-- Adds sections table and section_id column to students
-- ============================================================

-- Change the USE statement to the target tenant DB name:
-- USE ascent_group_1;
-- GO

-- 1. Create sections table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'sections')
BEGIN
    CREATE TABLE sections (
        section_id      INT          NOT NULL IDENTITY(1,1),
        class_id        INT          NOT NULL,
        section_name    VARCHAR(10)  NOT NULL,
        description     VARCHAR(50)  NULL,
        sequence_no     INT          NULL,
        status          VARCHAR(20)   NOT NULL DEFAULT 'Active',
        school_id       INT          NOT NULL,
        created_by      VARCHAR(25)  NULL,
        created_at      DATETIME     NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_sections           PRIMARY KEY (section_id),
        CONSTRAINT FK_sections_class     FOREIGN KEY (class_id)  REFERENCES classes(class_id),
        CONSTRAINT UQ_sections_per_class UNIQUE (class_id, section_name, school_id)
    );
    PRINT 'sections table created.';
END
ELSE
    PRINT 'sections table already exists — skipping.';
GO

-- 2. Add section_id column to students
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('students') AND name = 'section_id'
)
BEGIN
    ALTER TABLE students ADD section_id INT NULL;
    PRINT 'section_id column added to students.';
END
ELSE
    PRINT 'section_id column already exists on students — skipping.';
GO

-- 3. Add FK constraint
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_students_section')
BEGIN
    ALTER TABLE students
        ADD CONSTRAINT FK_students_section FOREIGN KEY (section_id) REFERENCES sections(section_id);
    PRINT 'FK_students_section constraint added.';
END
ELSE
    PRINT 'FK_students_section already exists — skipping.';
GO
