-- subjects_status_migration.sql
-- Widens subjects.status from VARCHAR(5) to VARCHAR(10) so 'Active'/'Inactive' fit.
-- Also normalises legacy 'Y'/'N' values to 'Active'/'Inactive'.

USE [ascent_group_1]  -- ← change to target tenant DB before running
GO

ALTER TABLE subjects
    ALTER COLUMN status VARCHAR(10) NULL
GO

UPDATE subjects SET status = 'Active'   WHERE status = 'Y'
UPDATE subjects SET status = 'Inactive' WHERE status = 'N'
GO
