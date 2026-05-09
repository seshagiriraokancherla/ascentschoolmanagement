-- ============================================================
-- Migration: Mobile App — Tenant DB tables
-- Run on: each ascent_group_{N} database (e.g. EducareDemoDb)
-- Safe to run multiple times (IF NOT EXISTS guards).
--
-- Adds:
--   1. student_mobile_accounts  — student PIN-based login
--   2. student_refresh_tokens   — sliding-expiry refresh tokens
--   3. student_attendance       — daily attendance records
--   4. exam_types               — Unit Test / Midterm / Final etc.
--   5. student_marks            — marks per student / subject / exam
--   6. homework                 — subject-wise homework per class
--   7. homework_attachments     — file attachments for homework
--   8. announcements            — school-wide or class-specific notices
-- ============================================================


-- ============================================================
-- 1. student_mobile_accounts
--    One row per student who registers in the mobile app.
--    PIN is SHA-256 + Base64 hashed (same as JwtHelper.HashRefreshToken).
--    Student registers with admission_no + school_code + PIN.
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'student_mobile_accounts')
BEGIN
    CREATE TABLE student_mobile_accounts (
        account_id      INT             NOT NULL IDENTITY(1,1),
        student_id      BIGINT          NOT NULL,
        pin_hash        VARCHAR(255)    NOT NULL,
        mobile          VARCHAR(20)     NULL,
        email           VARCHAR(100)    NULL,
        otp_code        VARCHAR(10)     NULL,        -- for PIN reset
        otp_expires_at  DATETIME        NULL,
        is_active       BIT             NOT NULL DEFAULT 1,
        last_login_at   DATETIME        NULL,
        school_id       INT             NOT NULL,
        created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_student_mobile_accounts           PRIMARY KEY (account_id),
        CONSTRAINT UQ_student_mobile_accounts_student   UNIQUE (student_id),
        CONSTRAINT FK_student_mobile_accounts_student   FOREIGN KEY (student_id) REFERENCES students(student_id)
    );
    PRINT 'Created student_mobile_accounts.'
END
ELSE
    PRINT 'student_mobile_accounts already exists — skipping.'
GO


-- ============================================================
-- 2. student_refresh_tokens
--    Sliding-expiry refresh tokens for student mobile sessions.
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'student_refresh_tokens')
BEGIN
    CREATE TABLE student_refresh_tokens (
        token_id        INT             NOT NULL IDENTITY(1,1),
        account_id      INT             NOT NULL,
        token_hash      VARCHAR(255)    NOT NULL,
        expires_at      DATETIME        NOT NULL,
        created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
        revoked_at      DATETIME        NULL,
        replaced_by     VARCHAR(255)    NULL,
        CONSTRAINT PK_student_refresh_tokens            PRIMARY KEY (token_id),
        CONSTRAINT FK_student_refresh_tokens_account    FOREIGN KEY (account_id) REFERENCES student_mobile_accounts(account_id)
    );
    PRINT 'Created student_refresh_tokens.'
END
ELSE
    PRINT 'student_refresh_tokens already exists — skipping.'
GO


-- ============================================================
-- 3. student_attendance
--    One row per student per date.
--    Marked by school staff via web UI; read by mobile app.
--    status: Present / Absent / Late / Holiday
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'student_attendance')
BEGIN
    CREATE TABLE student_attendance (
        attendance_id   INT             NOT NULL IDENTITY(1,1),
        student_id      BIGINT          NOT NULL,
        attendance_date DATE            NOT NULL,
        status          VARCHAR(10)     NOT NULL DEFAULT 'Present',  -- Present/Absent/Late/Holiday
        remarks         VARCHAR(100)    NULL,
        school_id       INT             NOT NULL,
        marked_by       VARCHAR(100)    NOT NULL,
        marked_at       DATETIME        NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_student_attendance            PRIMARY KEY (attendance_id),
        CONSTRAINT UQ_student_attendance_date       UNIQUE (student_id, attendance_date, school_id),
        CONSTRAINT FK_student_attendance_student    FOREIGN KEY (student_id) REFERENCES students(student_id)
    );

    CREATE INDEX IX_student_attendance_student_date
        ON student_attendance (student_id, attendance_date);

    PRINT 'Created student_attendance.'
END
ELSE
    PRINT 'student_attendance already exists — skipping.'
GO


-- ============================================================
-- 4. exam_types
--    Defines exam categories per school.
--    e.g. Unit Test 1, Midterm, Final, Slip Test, Annual
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'exam_types')
BEGIN
    CREATE TABLE exam_types (
        exam_type_id    INT             NOT NULL IDENTITY(1,1),
        exam_type_name  VARCHAR(50)     NOT NULL,
        academic_year_id INT            NULL,
        display_order   INT             NULL,
        school_id       INT             NOT NULL,
        status          VARCHAR(10)     NOT NULL DEFAULT 'Active',
        created_by      VARCHAR(100)    NULL,
        created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_exam_types    PRIMARY KEY (exam_type_id)
    );

    -- Seed common defaults
    PRINT 'Created exam_types.'
END
ELSE
    PRINT 'exam_types already exists — skipping.'
GO


-- ============================================================
-- 5. student_marks
--    Marks per student / subject / exam type / academic year.
--    Entered via web UI (teacher/admin); displayed in mobile app.
--    Unique constraint prevents duplicate entries.
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'student_marks')
BEGIN
    CREATE TABLE student_marks (
        mark_id             INT             NOT NULL IDENTITY(1,1),
        student_id          BIGINT          NOT NULL,
        subject_id          INT             NOT NULL,
        exam_type_id        INT             NOT NULL,
        academic_year_id    INT             NOT NULL,
        marks_obtained      DECIMAL(6,2)    NOT NULL,
        max_marks           DECIMAL(6,2)    NOT NULL DEFAULT 100,
        is_absent           BIT             NOT NULL DEFAULT 0,
        school_id           INT             NOT NULL,
        entered_by          VARCHAR(100)    NOT NULL,
        entered_at          DATETIME        NOT NULL DEFAULT GETDATE(),
        updated_by          VARCHAR(100)    NULL,
        updated_at          DATETIME        NULL,
        CONSTRAINT PK_student_marks         PRIMARY KEY (mark_id),
        CONSTRAINT UQ_student_marks         UNIQUE (student_id, subject_id, exam_type_id, academic_year_id, school_id),
        CONSTRAINT FK_student_marks_student FOREIGN KEY (student_id)       REFERENCES students(student_id),
        CONSTRAINT FK_student_marks_subject FOREIGN KEY (subject_id)       REFERENCES subjects(subject_id),
        CONSTRAINT FK_student_marks_exam    FOREIGN KEY (exam_type_id)     REFERENCES exam_types(exam_type_id),
        CONSTRAINT FK_student_marks_year    FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id)
    );

    CREATE INDEX IX_student_marks_student
        ON student_marks (student_id, academic_year_id);

    PRINT 'Created student_marks.'
END
ELSE
    PRINT 'student_marks already exists — skipping.'
GO


-- ============================================================
-- 6. homework
--    Homework assignments created by teachers via web UI.
--    Scoped to a class + subject.  Displayed in mobile app.
--    status: Active / Cancelled
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'homework')
BEGIN
    CREATE TABLE homework (
        homework_id     INT             NOT NULL IDENTITY(1,1),
        title           VARCHAR(200)    NOT NULL,
        description     NVARCHAR(MAX)   NULL,
        subject_id      INT             NULL,
        class_id        INT             NULL,
        assigned_date   DATE            NOT NULL DEFAULT CAST(GETDATE() AS DATE),
        due_date        DATE            NOT NULL,
        school_id       INT             NOT NULL,
        status          VARCHAR(10)     NOT NULL DEFAULT 'Active',
        created_by      VARCHAR(100)    NOT NULL,
        created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
        updated_by      VARCHAR(100)    NULL,
        updated_at      DATETIME        NULL,
        CONSTRAINT PK_homework              PRIMARY KEY (homework_id),
        CONSTRAINT FK_homework_subject      FOREIGN KEY (subject_id) REFERENCES subjects(subject_id),
        CONSTRAINT FK_homework_class        FOREIGN KEY (class_id)   REFERENCES classes(class_id)
    );

    CREATE INDEX IX_homework_class_due
        ON homework (class_id, due_date, school_id);

    PRINT 'Created homework.'
END
ELSE
    PRINT 'homework already exists — skipping.'
GO


-- ============================================================
-- 7. homework_attachments
--    Optional file attachments (PDF / images) for homework.
--    file_path is relative path served by API (same as branding).
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'homework_attachments')
BEGIN
    CREATE TABLE homework_attachments (
        attachment_id   INT             NOT NULL IDENTITY(1,1),
        homework_id     INT             NOT NULL,
        file_name       VARCHAR(200)    NOT NULL,
        file_path       VARCHAR(500)    NOT NULL,   -- /Uploads/homework/{file}
        file_size_kb    INT             NULL,
        CONSTRAINT PK_homework_attachments          PRIMARY KEY (attachment_id),
        CONSTRAINT FK_homework_attachments_hw       FOREIGN KEY (homework_id) REFERENCES homework(homework_id)
    );
    PRINT 'Created homework_attachments.'
END
ELSE
    PRINT 'homework_attachments already exists — skipping.'
GO


-- ============================================================
-- 8. announcements
--    School-wide or class-specific notices.
--    scope: 'School' (all students) or 'Class' (specific class_id).
--    Pinned announcements float to top in mobile app.
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'announcements')
BEGIN
    CREATE TABLE announcements (
        announcement_id INT             NOT NULL IDENTITY(1,1),
        title           VARCHAR(200)    NOT NULL,
        description     NVARCHAR(MAX)   NULL,
        scope           VARCHAR(10)     NOT NULL DEFAULT 'School',  -- School / Class
        class_id        INT             NULL,       -- NULL when scope = School
        is_pinned       BIT             NOT NULL DEFAULT 0,
        school_id       INT             NOT NULL,
        status          VARCHAR(10)     NOT NULL DEFAULT 'Active',
        created_by      VARCHAR(100)    NOT NULL,
        created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
        updated_by      VARCHAR(100)    NULL,
        updated_at      DATETIME        NULL,
        CONSTRAINT PK_announcements         PRIMARY KEY (announcement_id),
        CONSTRAINT FK_announcements_class   FOREIGN KEY (class_id) REFERENCES classes(class_id)
    );

    CREATE INDEX IX_announcements_school_date
        ON announcements (school_id, created_at DESC);

    PRINT 'Created announcements.'
END
ELSE
    PRINT 'announcements already exists — skipping.'
GO

PRINT '--- mobile_tenant_migration complete ---'
GO
