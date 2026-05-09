-- ============================================================
-- Migration: Add db_name column to school_groups
-- Run on: ascent_master
--
-- After running this, update each group's db_name manually:
--   UPDATE school_groups SET db_name = 'ascent_group_1' WHERE group_id = 1
-- ============================================================

USE ascent_master;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('school_groups') AND name = 'db_name'
)
BEGIN
    ALTER TABLE school_groups
        ADD db_name VARCHAR(100) NULL;

    PRINT 'Column db_name added to school_groups.'
END
ELSE
    PRINT 'Column db_name already exists — skipping.'
GO

-- ── Set db_name for existing groups ──────────────────────────
-- The default is ascent_group_{group_id}. Run this to auto-fill
-- any groups that don't have a custom db_name yet.
UPDATE school_groups
SET db_name = 'ascent_group_' + CAST(group_id AS VARCHAR)
WHERE db_name IS NULL;

PRINT 'db_name set for ' + CAST(@@ROWCOUNT AS VARCHAR) + ' group(s).'
GO
