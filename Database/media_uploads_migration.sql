-- ============================================================
-- media_uploads migration (Phase B — R2 uploads for homework/announcements/events)
-- Run on each existing tenant DB (ascent_group_N). Idempotent.
-- Also relaxes school_events.media_url to NULL (uploads now live in media_uploads;
-- media_url keeps only the optional YouTube URL).
-- ============================================================

USE ascent_group_1;   -- change to target DB before running
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'media_uploads')
BEGIN
    CREATE TABLE media_uploads (
        upload_id    INT           NOT NULL IDENTITY(1,1),
        entity_type  VARCHAR(20)   NOT NULL,     -- homework | announcement | event
        entity_id    BIGINT        NOT NULL,
        file_name    VARCHAR(300)  NOT NULL,
        file_url     VARCHAR(1000) NOT NULL,     -- R2 public URL
        file_type    VARCHAR(20)   NULL,         -- image | doc | audio | video
        file_size_kb INT           NULL,
        school_id    INT           NOT NULL,
        created_by   VARCHAR(150)  NULL,
        created_at   DATETIME      NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_media_uploads PRIMARY KEY (upload_id)
    );
    CREATE INDEX IX_media_uploads_entity ON media_uploads (entity_type, entity_id);
    PRINT 'media_uploads table created.';
END
ELSE
    PRINT 'media_uploads table already exists — skipped.';
GO

-- Make school_events.media_url nullable (was NOT NULL).
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
           WHERE TABLE_NAME = 'school_events' AND COLUMN_NAME = 'media_url' AND IS_NULLABLE = 'NO')
BEGIN
    ALTER TABLE school_events ALTER COLUMN media_url VARCHAR(4000) NULL;
    PRINT 'school_events.media_url set to NULL-able.';
END
ELSE
    PRINT 'school_events.media_url already nullable — skipped.';
GO
