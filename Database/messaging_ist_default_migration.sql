-- ============================================================
-- Messaging: stamp created_at in IST, not server-local time.
--
-- The production server runs US Eastern (UTC-4/-5) while schools are in India
-- (UTC+5:30). The message tables' created_at defaults used GETDATE() (server-local),
-- so chat/report timestamps were ~9.5-10.5 h off IST. This replaces the created_at
-- default on message_threads, messages and message_reports with an IST conversion
-- (SYSDATETIMEOFFSET() carries the server offset, so AT TIME ZONE is correct on any
-- server timezone). Requires SQL Server 2016+ (AT TIME ZONE).
--
-- Note: MessagingRepository now also sets created_at / last_message_at / read_at /
-- blocked_at / reviewed_at explicitly in IST, so this default is a safety net for any
-- other insert path. Run on each existing tenant DB (ascent_group_N). Idempotent.
-- (This migration does NOT back-correct existing rows — forward-looking only.)
-- ============================================================

USE ascent_group_1;   -- change to target DB before running
GO

DECLARE @sql NVARCHAR(MAX) = N'';

-- Drop the existing (auto-named) created_at defaults on the three tables.
SELECT @sql = @sql + N'ALTER TABLE ' + QUOTENAME(t.name) +
              N' DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';' + CHAR(10)
FROM sys.default_constraints dc
JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
JOIN sys.tables  t ON t.object_id = dc.parent_object_id
WHERE t.name IN ('message_threads', 'messages', 'message_reports')
  AND c.name = 'created_at';

IF @sql <> N'' EXEC sp_executesql @sql;
GO

-- Re-add each as an IST default (only if not already present under our name).
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_message_threads_created_at')
    ALTER TABLE message_threads ADD CONSTRAINT DF_message_threads_created_at
        DEFAULT CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'India Standard Time' AS DATETIME) FOR created_at;

IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_messages_created_at')
    ALTER TABLE messages ADD CONSTRAINT DF_messages_created_at
        DEFAULT CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'India Standard Time' AS DATETIME) FOR created_at;

IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_message_reports_created_at')
    ALTER TABLE message_reports ADD CONSTRAINT DF_message_reports_created_at
        DEFAULT CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'India Standard Time' AS DATETIME) FOR created_at;

PRINT 'Messaging created_at defaults set to IST (message_threads, messages, message_reports).';
GO
