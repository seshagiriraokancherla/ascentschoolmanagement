-- ============================================================
-- School Calendar Migration
-- Run on each existing tenant DB (ascent_group_N)
-- Adds: calendar_events table (holidays / exams / celebrations / events;
--        school-wide, date-range) used by the web Calendar page and the
--        parent mobile Calendar tab.
-- Idempotent — safe to re-run (guarded by IF NOT EXISTS).
-- ============================================================

USE ascent_group_1;   -- change to target DB before running
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'calendar_events')
BEGIN
    CREATE TABLE calendar_events (
        calendar_event_id INT           NOT NULL IDENTITY(1,1),
        title             VARCHAR(200)  NOT NULL,
        description       NVARCHAR(MAX) NULL,
        category          VARCHAR(20)   NOT NULL DEFAULT 'Event',  -- Holiday | Exam | Celebration | Event
        start_date        DATE          NOT NULL,
        end_date          DATE          NOT NULL,
        academic_year_id  INT           NULL,
        school_id         INT           NOT NULL,
        status            VARCHAR(10)   NOT NULL DEFAULT 'Active',
        created_by        VARCHAR(100)  NOT NULL,
        -- IST default (server runs US Eastern) — Phase 98/103/105.
        created_at        DATETIME      NOT NULL DEFAULT (CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'India Standard Time' AS DATETIME)),
        updated_by        VARCHAR(100)  NULL,
        updated_at        DATETIME      NULL,
        CONSTRAINT PK_calendar_events               PRIMARY KEY (calendar_event_id),
        CONSTRAINT FK_calendar_events_academic_year FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id)
    );

    CREATE INDEX IX_calendar_events_school_date ON calendar_events (school_id, start_date);

    PRINT 'calendar_events table created.';
END
ELSE
    PRINT 'calendar_events table already exists — skipped.';
GO
