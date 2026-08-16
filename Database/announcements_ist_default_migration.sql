-- ============================================================
-- Announcements: stamp created_at in IST, not server-local time.
--
-- The production server runs US Eastern (UTC-4/-5) while schools are in India
-- (UTC+5:30). The announcements.created_at default used GETDATE() (server-local),
-- so an announcement posted this morning IST carried yesterday's Eastern date.
-- The mobile tiles-view "News" ticker filters to today's announcements, so that
-- ~10 h skew would drop today's items. This replaces the created_at default with
-- an IST conversion (SYSDATETIMEOFFSET() carries the server offset, so AT TIME ZONE
-- is correct on any server timezone). Requires SQL Server 2016+ (AT TIME ZONE).
--
-- Note: AnnouncementsRepository.CreateAnnouncement now also sets created_at
-- explicitly in IST, so this default is a safety net for any other insert path.
-- Run on each existing tenant DB (ascent_group_N). Idempotent.
-- (This migration does NOT back-correct existing rows — forward-looking only.)
-- ============================================================

USE ascent_group_1;   -- change to target DB before running
GO

  DECLARE @sql NVARCHAR(MAX) = N'';

  -- Drop the existing (auto-named) created_at default on announcements.
  SELECT @sql = @sql + N'ALTER TABLE ' + QUOTENAME(t.name) +
                N' DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';' + CHAR(10)
  FROM sys.default_constraints dc
  JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
  JOIN sys.tables  t ON t.object_id = dc.parent_object_id
  WHERE t.name = 'announcements'
    AND c.name = 'created_at';

  IF @sql <> N'' EXEC sp_executesql @sql;
  GO

  -- Re-add as an IST default (only if not already present under our name).
  IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_announcements_created_at')
      ALTER TABLE announcements ADD CONSTRAINT DF_announcements_created_at
          DEFAULT CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'India Standard Time' AS DATETIME) FOR created_at;

  PRINT 'announcements.created_at default set to IST.';
  GO
