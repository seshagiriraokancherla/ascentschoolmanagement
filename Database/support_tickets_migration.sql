-- ============================================================
-- support_tickets — school staff raise Issue/Change tickets from
-- the school app; the Ascent control app manages status across every
-- school group. Lives in ascent_master (master table 18), NOT a
-- tenant table, so the internal team sees everything in one place.
--
-- Idempotent — safe to re-run. Run ONCE on ascent_master (NOT per
-- tenant DB — same rule as device_push_tokens, Phase 91).
-- ============================================================

-- USE ascent_master;
-- GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'support_tickets')
BEGIN
    CREATE TABLE support_tickets (
        ticket_id                    INT             NOT NULL IDENTITY(1,1),
        group_id                     INT             NOT NULL,
        school_id                    INT             NULL,
        db_name                      VARCHAR(100)    NOT NULL,
        raised_by_user_id            INT             NOT NULL,
        raised_by_name               VARCHAR(100)    NOT NULL,
        raised_by_username           VARCHAR(50)     NULL,
        ticket_type                  VARCHAR(20)     NOT NULL DEFAULT 'Issue',
        priority                     VARCHAR(10)     NOT NULL DEFAULT 'Medium',
        subject                      VARCHAR(200)    NOT NULL,
        description                  VARCHAR(MAX)    NOT NULL,
        module                       VARCHAR(50)     NULL,
        status                       VARCHAR(20)     NOT NULL DEFAULT 'Open',
        assigned_to_control_user_id  INT             NULL,
        resolution_notes             VARCHAR(MAX)    NULL,
        created_at                   DATETIME        NOT NULL DEFAULT (CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'India Standard Time' AS DATETIME)),
        updated_at                   DATETIME        NULL,
        resolved_at                  DATETIME        NULL,
        CONSTRAINT PK_support_tickets           PRIMARY KEY (ticket_id),
        CONSTRAINT FK_support_tickets_group     FOREIGN KEY (group_id)  REFERENCES school_groups(group_id),
        CONSTRAINT FK_support_tickets_school    FOREIGN KEY (school_id) REFERENCES schools(school_id),
        CONSTRAINT FK_support_tickets_assigned  FOREIGN KEY (assigned_to_control_user_id) REFERENCES control_users(user_id)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_support_tickets_group')
BEGIN
    CREATE INDEX IX_support_tickets_group ON support_tickets (group_id, status);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_support_tickets_status')
BEGIN
    CREATE INDEX IX_support_tickets_status ON support_tickets (status, created_at DESC);
END
GO

PRINT 'support_tickets_migration complete.';
GO
