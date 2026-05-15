-- ============================================================
-- fee_collection_redesign_migration.sql
-- Adds student_unique_id to fee_receipts for cross-year tracking.
-- join_type already exists on students (New / Transfer).
-- ============================================================

ALTER TABLE fee_receipts
    ADD student_unique_id INT NULL;
GO

PRINT 'fee_collection_redesign_migration.sql: student_unique_id added to fee_receipts.';
