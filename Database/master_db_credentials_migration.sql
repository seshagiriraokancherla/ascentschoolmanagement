-- ============================================================
-- Migration: Add db_username and db_password to school_groups
-- Run on: ascent_master
-- ============================================================

ALTER TABLE school_groups
ADD db_username VARCHAR(100) NULL,
    db_password VARCHAR(200) NULL;
GO

-- Verify
SELECT group_id, group_name, db_name, db_username,
       CASE WHEN db_password IS NOT NULL THEN '(set)' ELSE NULL END AS db_password_status
FROM school_groups
ORDER BY group_id;
GO
