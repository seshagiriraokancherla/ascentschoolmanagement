-- ============================================================
-- fee_types.fee_type_name: VARCHAR(25) -> VARCHAR(100)
-- (+ fee_structures.fee_type_name, the nullable snapshot column, to match)
-- ------------------------------------------------------------
-- 25 chars was too short for real fee type names, e.g.
-- "Extra Circular Activity Fee" (27) failed with
--   "String or binary data would be truncated ... column 'fee_type_name'".
--
-- Idempotent: only alters a column still narrower than 100.
-- Run once per existing tenant DB.
-- ============================================================

IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'fee_types' AND COLUMN_NAME = 'fee_type_name'
      AND DATA_TYPE = 'varchar' AND CHARACTER_MAXIMUM_LENGTH < 100
)
BEGIN
    ALTER TABLE fee_types ALTER COLUMN fee_type_name VARCHAR(100) NOT NULL;
    PRINT 'fee_types.fee_type_name widened to VARCHAR(100).';
END
ELSE
    PRINT 'fee_types.fee_type_name already VARCHAR(100) or wider — skipped.';
GO

IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'fee_structures' AND COLUMN_NAME = 'fee_type_name'
      AND DATA_TYPE = 'varchar' AND CHARACTER_MAXIMUM_LENGTH < 100
)
BEGIN
    ALTER TABLE fee_structures ALTER COLUMN fee_type_name VARCHAR(100) NULL;
    PRINT 'fee_structures.fee_type_name widened to VARCHAR(100).';
END
ELSE
    PRINT 'fee_structures.fee_type_name already VARCHAR(100) or wider — skipped.';
GO
