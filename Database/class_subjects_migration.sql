-- ============================================================
-- class_subjects migration
--   Adds the class_subjects mapping table (which subjects a class
--   studies in a given academic year) to an EXISTING tenant DB.
--   Prerequisite for the exam / marks feature.
--   Idempotent (IF NOT EXISTS guard). Run once per tenant DB.
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'class_subjects')
BEGIN
    CREATE TABLE class_subjects (
        class_subject_id  INT          NOT NULL IDENTITY(1,1),
        academic_year_id  INT          NOT NULL,
        class_id          INT          NOT NULL,
        subject_id        INT          NOT NULL,
        display_order     INT          NULL,
        is_optional       BIT          NOT NULL DEFAULT 0,
        status            VARCHAR(10)  NOT NULL DEFAULT 'Active',
        school_id         INT          NOT NULL,
        created_by        VARCHAR(100) NULL,
        created_at        DATETIME     NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_class_subjects   PRIMARY KEY (class_subject_id),
        CONSTRAINT FK_cs_academic_year FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id),
        CONSTRAINT FK_cs_class         FOREIGN KEY (class_id)         REFERENCES classes(class_id),
        CONSTRAINT FK_cs_subject       FOREIGN KEY (subject_id)       REFERENCES subjects(subject_id),
        CONSTRAINT UQ_class_subjects   UNIQUE (school_id, academic_year_id, class_id, subject_id)
    );

    CREATE INDEX IX_class_subjects_lookup ON class_subjects (school_id, academic_year_id, class_id, status);

    PRINT 'class_subjects table created.';
END
ELSE
    PRINT 'class_subjects table already exists — skipped.';
GO
