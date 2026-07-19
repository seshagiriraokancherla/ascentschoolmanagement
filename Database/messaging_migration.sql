-- ============================================================
-- Feature 2: parent <-> teacher messaging.
-- Adds message_threads, messages, message_reports.
--   * Threads key on student_unique_id (STABLE across promotions), not student_id.
--   * parent_id / sender_id point at ascent_master.parent_accounts — cross-DB, no FK.
--   * Which teachers see a thread is resolved live from the student's current
--     class+section via class_teacher_assignments (run its migration first).
--   * message_reports + thread blocking exist for the Play Store UGC requirement.
-- Run on each existing tenant DB (ascent_group_N), AFTER
-- class_teacher_assignments_migration.sql. Idempotent.
-- ============================================================

USE ascent_group_1;   -- change to target DB before running
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'message_threads')
BEGIN
    CREATE TABLE message_threads (
        thread_id         INT          NOT NULL IDENTITY(1,1),
        student_unique_id INT          NOT NULL,   -- stable cross-year student id
        parent_id         INT          NOT NULL,   -- master DB parent_accounts.parent_id (no FK: other DB)
        status            VARCHAR(10)  NOT NULL DEFAULT 'Active',  -- Active | Blocked
        blocked_by_type   VARCHAR(10)  NULL,       -- parent | teacher
        blocked_by_id     INT          NULL,
        blocked_at        DATETIME     NULL,
        last_message_at   DATETIME     NULL,
        school_id         INT          NOT NULL,
        created_at        DATETIME     NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_message_threads PRIMARY KEY (thread_id),
        CONSTRAINT UQ_message_threads_student_parent UNIQUE (school_id, student_unique_id, parent_id)
    );
    CREATE INDEX IX_message_threads_recent ON message_threads (school_id, last_message_at DESC);
    PRINT 'message_threads table created.';
END
ELSE
    PRINT 'message_threads table already exists — skipped.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'messages')
BEGIN
    CREATE TABLE messages (
        message_id  INT           NOT NULL IDENTITY(1,1),
        thread_id   INT           NOT NULL,
        sender_type VARCHAR(10)   NOT NULL,   -- parent | teacher
        sender_id   INT           NOT NULL,   -- parent_id or users.user_id (per sender_type)
        sender_name VARCHAR(150)  NULL,       -- display snapshot at send time
        body        VARCHAR(2000) NOT NULL,
        status      VARCHAR(10)   NOT NULL DEFAULT 'Active',  -- Active | Removed
        read_at     DATETIME      NULL,       -- when the OTHER side first read it
        school_id   INT           NOT NULL,
        created_at  DATETIME      NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_messages        PRIMARY KEY (message_id),
        CONSTRAINT FK_messages_thread FOREIGN KEY (thread_id) REFERENCES message_threads(thread_id)
    );
    CREATE INDEX IX_messages_thread ON messages (thread_id, created_at);
    PRINT 'messages table created.';
END
ELSE
    PRINT 'messages table already exists — skipped.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'message_reports')
BEGIN
    CREATE TABLE message_reports (
        report_id        INT          NOT NULL IDENTITY(1,1),
        message_id       INT          NOT NULL,
        thread_id        INT          NOT NULL,
        reported_by_type VARCHAR(10)  NOT NULL,   -- parent | teacher
        reported_by_id   INT          NOT NULL,
        reason           VARCHAR(500) NULL,
        status           VARCHAR(10)  NOT NULL DEFAULT 'Open',  -- Open | Reviewed | Removed
        reviewed_by      VARCHAR(150) NULL,
        reviewed_at      DATETIME     NULL,
        school_id        INT          NOT NULL,
        created_at       DATETIME     NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_message_reports         PRIMARY KEY (report_id),
        CONSTRAINT FK_message_reports_message FOREIGN KEY (message_id) REFERENCES messages(message_id),
        CONSTRAINT FK_message_reports_thread  FOREIGN KEY (thread_id)  REFERENCES message_threads(thread_id)
    );
    CREATE INDEX IX_message_reports_open ON message_reports (school_id, status, created_at DESC);
    PRINT 'message_reports table created.';
END
ELSE
    PRINT 'message_reports table already exists — skipped.';
GO
