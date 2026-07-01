-- ============================================================
-- app_config_auto_update_migration.sql
-- Run ONCE against ascent_master.
--
-- Adds the auto_update_enabled flag to app_config. The mobile app
-- only auto-prompts for updates (force/soft) when this flag is 1 for
-- the matching applicationId row; otherwise the server returns
-- updateRequired = updateAvailable = false and the app shows only a
-- manual "Update App" button on the login screen.
--
-- Defaults to 0 (OFF) for every existing row, so auto-prompts stop
-- immediately after this migration. Turn it back on per app with:
--   UPDATE app_config SET auto_update_enabled = 1
--   WHERE application_id = '<flavor app id>' AND platform = 'android';
--
-- Idempotent — safe to re-run.
-- ============================================================

USE ascent_master;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('app_config') AND name = 'auto_update_enabled')
BEGIN
    ALTER TABLE app_config
        ADD auto_update_enabled BIT NOT NULL CONSTRAINT DF_app_config_auto_update DEFAULT 0;
    PRINT 'Added app_config.auto_update_enabled (default 0).';
END
ELSE
    PRINT 'app_config.auto_update_enabled already exists.';
GO
