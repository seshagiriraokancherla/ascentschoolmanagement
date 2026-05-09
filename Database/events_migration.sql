-- events_migration.sql
-- Run on each tenant DB (ascent_group_N) to add school_events table.

USE [ascent_group_1]  -- ← change to target tenant DB before running
GO

CREATE TABLE school_events (
    event_id      INT           IDENTITY(1,1) PRIMARY KEY,
    school_id     INT           NOT NULL,
    title         VARCHAR(200)  NOT NULL,
    description   VARCHAR(1000) NULL,
    event_date    DATE          NOT NULL,
    media_type    VARCHAR(10)   NOT NULL DEFAULT 'image',  -- 'image' | 'video'
    media_url     VARCHAR(4000)  NOT NULL,                  -- Cloudinary URL or YouTube URL
    thumbnail_url  VARCHAR(4000)  NULL,                      -- optional override thumbnail
    attachment_url VARCHAR(4000)  NULL,                      -- optional PDF/doc link (Google Drive, Cloudinary)
    scope         VARCHAR(10)   NOT NULL DEFAULT 'School', -- 'School' | 'Class'
    class_id      INT           NULL,
    is_pinned     BIT           NOT NULL DEFAULT 0,
    status        VARCHAR(20)   NOT NULL DEFAULT 'Active', -- 'Active' | 'Inactive'
    created_by    VARCHAR(100)  NULL,
    created_at    DATETIME      NOT NULL DEFAULT GETDATE(),
    updated_by    VARCHAR(100)  NULL,
    updated_at    DATETIME      NULL,

    CONSTRAINT FK_school_events_class FOREIGN KEY (class_id) REFERENCES classes(class_id)
)
GO

CREATE INDEX IX_school_events_school_id ON school_events (school_id)
GO
CREATE INDEX IX_school_events_event_date ON school_events (event_date DESC)
GO
