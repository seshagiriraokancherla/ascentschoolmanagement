-- ============================================================
-- sms_config_migration.sql
-- Run ONCE against each existing ascent_group_{N} tenant DB.
--
-- Adds per-school SMS gateway configuration so the SMS Center sends
-- via each school's own smslogin.mobi account + DLT templates instead
-- of a single hardcoded account.
--
--   sms_configs    — gateway account (one row per school)
--   sms_templates  — one row per school per message template (grows over time)
--
-- OTP (parent app / mobile login) is NOT affected — it keeps using the
-- hardcoded global account in SmsHelper.cs.
--
-- Seeding: existing schools (DISTINCT school_id in students) get a row
-- pre-filled with the CURRENT shared account + current message wording,
-- with template_id left blank (each school fills its own DLT ids via the
-- School App → Settings → SMS Gateway page). No behaviour change until
-- a school configures real template ids.
--
-- Idempotent — safe to re-run.
-- ============================================================

-- ── 1. sms_configs (gateway account, one per school) ──────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'sms_configs')
BEGIN
    CREATE TABLE sms_configs (
        config_id   INT IDENTITY(1,1) NOT NULL,
        school_id   INT          NOT NULL,
        provider    VARCHAR(30)  NOT NULL CONSTRAINT DF_sms_configs_provider DEFAULT 'smslogin',
        api_url     VARCHAR(200) NOT NULL,
        username    VARCHAR(100) NOT NULL,
        api_key     VARCHAR(200) NOT NULL,            -- never returned by the GET endpoint
        sender_id   VARCHAR(20)  NOT NULL,            -- school's DLT-approved header
        is_enabled  BIT          NOT NULL CONSTRAINT DF_sms_configs_enabled DEFAULT 1,
        created_by  VARCHAR(150) NULL,
        updated_at  DATETIME     NOT NULL CONSTRAINT DF_sms_configs_updated DEFAULT GETDATE(),
        CONSTRAINT PK_sms_configs        PRIMARY KEY (config_id),
        CONSTRAINT UQ_sms_configs_school UNIQUE (school_id)
    );
END
GO

-- ── 2. sms_templates (one row per school per template) ────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'sms_templates')
BEGIN
    CREATE TABLE sms_templates (
        template_row_id INT IDENTITY(1,1) NOT NULL,
        school_id       INT           NOT NULL,
        template_key    VARCHAR(40)   NOT NULL,       -- 'ABSENT','FEE_DUE','CUSTOM', future keys...
        title           VARCHAR(100)  NULL,           -- UI label
        template_id     VARCHAR(50)   NOT NULL CONSTRAINT DF_sms_templates_tid DEFAULT '',  -- DLT id ('' = not set yet)
        message_text    NVARCHAR(500) NOT NULL CONSTRAINT DF_sms_templates_msg DEFAULT '',  -- with {name},{date},{amount} placeholders
        placeholders    VARCHAR(200)  NULL,           -- UI hint, e.g. '{name},{date}'
        is_active       BIT           NOT NULL CONSTRAINT DF_sms_templates_active DEFAULT 1,
        updated_at      DATETIME      NOT NULL CONSTRAINT DF_sms_templates_updated DEFAULT GETDATE(),
        CONSTRAINT PK_sms_templates  PRIMARY KEY (template_row_id),
        CONSTRAINT UQ_sms_templates  UNIQUE (school_id, template_key)
    );
END
GO

/*-- ── 3. Seed a config row per existing school (shared account default) ─────
INSERT INTO sms_configs (school_id, provider, api_url, username, api_key, sender_id, is_enabled, created_by)
SELECT DISTINCT s.school_id, 'smslogin', 'https://smslogin.mobi/v3/api.php',
       'mushkin', 'c6cacc7b6d643993b770', 'ASNTNF', 1, 'migration'
FROM   students s
WHERE  s.school_id IS NOT NULL
  AND  NOT EXISTS (SELECT 1 FROM sms_configs c WHERE c.school_id = s.school_id);
GO

-- ── 4. Seed the 3 current template types per existing school ──────────────
--      template_id left blank — each school fills its own DLT id via the UI.
;WITH sch AS (SELECT DISTINCT school_id FROM students WHERE school_id IS NOT NULL)
INSERT INTO sms_templates (school_id, template_key, title, template_id, message_text, placeholders)
SELECT sch.school_id, t.template_key, t.title, '', t.message_text, t.placeholders
FROM   sch
CROSS JOIN (VALUES
    ('ABSENT',  'Absent Notification',
     N'Dear Parent, your ward {name} was absent on {date}. Please ensure regular attendance.', '{name},{date}'),
    ('FEE_DUE', 'Fee Due Reminder',
     N'Dear Parent, your ward {name} has an outstanding fee of Rs.{amount}. Please pay at the earliest.', '{name},{amount}'),
    ('CUSTOM',  'Custom Message',
     N'', '{name}')
) t(template_key, title, message_text, placeholders)
WHERE NOT EXISTS (
    SELECT 1 FROM sms_templates st
    WHERE st.school_id = sch.school_id AND st.template_key = t.template_key
);
GO
*/
PRINT 'sms_configs / sms_templates ready and seeded.';
GO
