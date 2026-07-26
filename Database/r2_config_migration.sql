-- ============================================================
-- r2_configs migration
-- Run on each existing tenant DB (ascent_group_N).
-- Per-school Cloudflare R2 (S3-compatible) storage credentials.
-- Idempotent — safe to re-run.
-- ============================================================

USE ascent_group_1;   -- change to target DB before running
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'r2_configs')
BEGIN
    CREATE TABLE r2_configs (
        config_id         INT           NOT NULL IDENTITY(1,1),
        school_id         INT           NOT NULL,
        account_id        VARCHAR(100)  NOT NULL,
        access_key_id     VARCHAR(200)  NOT NULL,
        secret_access_key VARCHAR(500)  NOT NULL,   -- never returned by GET
        bucket_name       VARCHAR(100)  NOT NULL,
        public_base_url   VARCHAR(300)  NOT NULL,   -- e.g. https://pub-xxx.r2.dev (no trailing slash)
        is_enabled        BIT           NOT NULL DEFAULT 1,
        created_by        VARCHAR(150)  NULL,
        updated_at        DATETIME      NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_r2_configs        PRIMARY KEY (config_id),
        CONSTRAINT UQ_r2_configs_school UNIQUE (school_id)
    );
    PRINT 'r2_configs table created.';
END
ELSE
    PRINT 'r2_configs table already exists — skipped.';
GO
