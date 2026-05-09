-- ============================================================
-- Migration: SMS Logs — Tenant DB table
-- Run on: each ascent_group_{N} database
-- Safe to run multiple times (IF NOT EXISTS guard).
--
-- Adds:
--   1. sms_logs  — audit trail of every SMS sent from the school app
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'sms_logs')
BEGIN
    CREATE TABLE sms_logs (
        log_id          INT             NOT NULL IDENTITY(1,1),
        school_id       INT             NOT NULL,
        sms_type        VARCHAR(20)     NOT NULL,   -- Absent / FeeDue / Custom
        student_id      BIGINT          NULL,
        student_name    VARCHAR(200)    NULL,
        mobile          VARCHAR(20)     NOT NULL,
        message         NVARCHAR(1000)  NOT NULL,
        status          VARCHAR(10)     NOT NULL,   -- Sent / Failed
        error_message   VARCHAR(500)    NULL,
        sent_by         VARCHAR(200)    NOT NULL,
        sent_at         DATETIME        NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_sms_logs PRIMARY KEY (log_id)
    );

    CREATE INDEX IX_sms_logs_school_type_date
        ON sms_logs (school_id, sms_type, sent_at DESC);

    PRINT 'Created sms_logs.'
END
ELSE
    PRINT 'sms_logs already exists — skipping.'
GO

PRINT '--- sms_migration complete ---'
GO
