-- VB Integration — Add API key column to school_settings (master DB)
-- Run this against ascent_master

ALTER TABLE school_settings
    ADD integration_api_key VARCHAR(50) NULL;
GO

-- Unique index: two schools cannot share the same key.
-- Filtered so NULLs don't collide with each other.
CREATE UNIQUE INDEX UQ_school_settings_api_key
    ON school_settings (integration_api_key)
    WHERE integration_api_key IS NOT NULL;
GO

-- ── Generate and assign a key for a school ───────────────────────────────────
-- Run the SELECT first to confirm your school_id, then run the UPDATE.

-- 1. List all schools and their current key status:
SELECT s.school_id, s.school_name, ss.integration_api_key
FROM   schools s
LEFT   JOIN school_settings ss ON ss.school_id = s.school_id
ORDER  BY s.school_id;

-- 2. Assign a random key to a specific school (replace 1 with your school_id).
--    Re-run to rotate the key at any time.
-- UPDATE school_settings
-- SET    integration_api_key = LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
-- WHERE  school_id = 1;

-- 3. View the generated key (copy this into your VB app's API_KEY constant):
-- SELECT integration_api_key
-- FROM   school_settings
-- WHERE  school_id = 1;
