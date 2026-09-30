-- ============================================================
-- school_settings_homework_sms_migration.sql
-- Adds school_settings.homework_sms_enabled — per-branch toggle for whether
-- saving Daily Homework sends an SMS to the parents of every student in the
-- affected sections. Defaults OFF for every existing branch (opt-in).
--
-- school_settings lives in ascent_master (one row per branch) — run ONCE on
-- the master DB, NOT per tenant.
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('school_settings') AND name = 'homework_sms_enabled'
)
BEGIN
    ALTER TABLE school_settings ADD homework_sms_enabled BIT NOT NULL DEFAULT 0;
    PRINT 'Added school_settings.homework_sms_enabled';
END
ELSE
    PRINT 'school_settings.homework_sms_enabled already exists — skipping';
GO
