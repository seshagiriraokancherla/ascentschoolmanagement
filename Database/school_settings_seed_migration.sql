-- school_settings_seed_migration.sql
-- Run against ascent_master.
-- Inserts a school_settings row for every school branch that doesn't have one yet.
-- Safe to re-run (IF NOT EXISTS guard).

INSERT INTO school_settings (school_id)
SELECT s.school_id
FROM   schools s
WHERE  NOT EXISTS (
    SELECT 1 FROM school_settings ss WHERE ss.school_id = s.school_id
);

-- Verify: every school should now have a row.
SELECT s.school_id, s.school_name, ss.integration_api_key
FROM   schools s
JOIN   school_settings ss ON ss.school_id = s.school_id
ORDER  BY s.school_id;
