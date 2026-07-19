-- ============================================================
-- Feature 3 (Phase 91): push notifications (FCM).
-- Adds device_push_tokens to the MASTER DB (ascent_master) — run ONCE
-- on the master database, NOT per tenant.
--   One row per device install; a parent OR teacher owns each token.
-- Idempotent: safe to re-run.
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'device_push_tokens')
BEGIN
    CREATE TABLE device_push_tokens (
        token_id        INT             NOT NULL IDENTITY(1,1),
        fcm_token       VARCHAR(4000)   NOT NULL,
        user_type       VARCHAR(10)     NOT NULL,            -- 'parent' | 'teacher'
        parent_id       INT             NULL,
        teacher_user_id INT             NULL,
        group_id        INT             NULL,
        school_id       INT             NULL,
        db_name         VARCHAR(100)    NULL,
        application_id  VARCHAR(100)    NULL,
        platform        VARCHAR(10)     NOT NULL DEFAULT 'android',
        created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
        updated_at      DATETIME        NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_device_push_tokens PRIMARY KEY (token_id)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_device_push_tokens_token')
    CREATE UNIQUE INDEX UQ_device_push_tokens_token ON device_push_tokens (fcm_token);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_device_push_tokens_parent')
    CREATE INDEX IX_device_push_tokens_parent ON device_push_tokens (parent_id) WHERE parent_id IS NOT NULL;
GO
