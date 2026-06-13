-- ============================================================
-- eod_sync_migration.sql
-- Run against each ascent_group_{N} tenant DB.
-- Adds source column to fee_receipts for EOD sync tracking.
-- ============================================================

-- Add source column to fee_receipts
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('fee_receipts') AND name = 'source'
)
BEGIN
    ALTER TABLE fee_receipts
        ADD source VARCHAR(10) NULL;   -- 'legacy' | 'webapp'

    PRINT 'Added source column to fee_receipts.';
END
ELSE
    PRINT 'source column already exists on fee_receipts — skipped.';
GO

-- Tag existing receipts created before this migration as 'webapp'
-- (all historical receipts in the new system were created via webapp or migration)
UPDATE fee_receipts
SET source = 'webapp'
WHERE source IS NULL;

PRINT CAST(@@ROWCOUNT AS VARCHAR) + ' existing receipts tagged as webapp.';
GO
