-- ============================================================
-- app_config_migration.sql
-- Run ONCE against ascent_master.
--
-- Adds the app_config table that drives the mobile force/soft
-- update gate (Option 3). The Android app sends its applicationId
-- + versionCode to GET /mobile/app/config on launch; the server
-- compares against the row below and tells the app whether an
-- update is REQUIRED (hard block) or AVAILABLE (soft nudge).
--
-- TO FORCE ALL USERS onto a newer build later (no API redeploy):
--   UPDATE app_config
--   SET min_supported_version_code = <safe versionCode>,
--       update_message = 'Please update to continue.'
--   WHERE application_id = '<flavor app id>';   -- or '*' for all
--
-- TO SOFT-NUDGE only: raise latest_version_code, leave min alone.
--
-- Idempotent — safe to re-run.
-- ============================================================

USE ascent_master;
GO

-- ── 1. Create the table if it doesn't exist ───────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'app_config')
BEGIN
    CREATE TABLE app_config (
        config_id                   INT             NOT NULL IDENTITY(1,1),
        application_id              VARCHAR(100)    NOT NULL,   -- e.g. 'in.educare.app'; '*' = default fallback
        platform                   VARCHAR(10)     NOT NULL DEFAULT 'android',
        min_supported_version_code  INT             NOT NULL DEFAULT 1,   -- app below this  -> FORCE update
        latest_version_code         INT             NOT NULL DEFAULT 1,   -- app below this (>= min) -> SOFT update
        update_message             NVARCHAR(500)   NULL,
        store_url                  VARCHAR(300)    NULL,                  -- optional override; else app builds market:// link
        updated_at                 DATETIME        NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_app_config          PRIMARY KEY (config_id),
        CONSTRAINT UQ_app_config_app_plat UNIQUE (application_id, platform)
    );
END
GO

-- ── 2. Seed one row per published applicationId + a '*' fallback ───────────
--      Initial values arm the gate but force nobody (min = 1).
--      latest = the current shipping versionCode (12) so no false soft-nudge.
INSERT INTO app_config (application_id, platform, min_supported_version_code, latest_version_code, update_message)
SELECT v.application_id, 'android', 1, 12, NULL
FROM (VALUES
    ('*'),                       -- default fallback for any unknown app id
    ('in.educare.app'),          -- CHAK IN (generic single-app build)
    ('in.educare.stannsasf'),
    ('in.educare.holyspiritjm'),
    ('in.educare.depaulemyv'),
    ('in.educare.demo')
) AS v(application_id)
WHERE NOT EXISTS (
    SELECT 1 FROM app_config c
    WHERE c.application_id = v.application_id AND c.platform = 'android'
);
GO

PRINT 'app_config table ready and seeded.';
GO
