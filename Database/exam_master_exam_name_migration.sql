-- ============================================================
-- exam_master.exam_name migration
--   Adds the exam_name column to the existing exam_master table
--   (exam label shown on the Exam Master form / report cards).
--   Idempotent. Run once per tenant DB.
-- ============================================================
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'exam_master')
   AND NOT EXISTS (SELECT 1 FROM sys.columns
                   WHERE object_id = OBJECT_ID('exam_master') AND name = 'exam_name')
BEGIN
    ALTER TABLE exam_master ADD exam_name VARCHAR(200) NULL;
    PRINT 'exam_master.exam_name column added.';
END
ELSE
    PRINT 'exam_master.exam_name already exists (or exam_master missing) — skipped.';
GO
