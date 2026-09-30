-- ============================================================
-- exam_master.exam_remarks: VARCHAR -> NVARCHAR (Unicode) + widen to 1000 chars
-- ------------------------------------------------------------
-- Root cause: exam_remarks was VARCHAR(300) — a non-Unicode (codepage) type.
-- Staff pasting local-language (Telugu/Hindi) syllabus text from a PDF had
-- every non-ASCII character silently collapsed to a literal '?' by SQL Server
-- on INSERT/UPDATE (the API's Unicode parameter gets implicitly converted
-- down to the column's non-Unicode codepage — Dapper/SqlClient default
-- string parameters to Unicode, so this happens even through parameterized
-- queries, with no error raised).
--
-- This migration only fixes the COLUMN TYPE so future saves store Telugu/
-- Hindi/etc. correctly. It CANNOT recover text that was already saved as
-- literal '?' characters — that data is gone at the byte level. Any exam row
-- whose Remarks already shows "??? ??" must be re-typed/re-pasted by staff
-- AFTER this migration runs (and after the API is redeployed).
--
-- Idempotent: safe to re-run; only alters if still the old VARCHAR type.
-- Run once per existing tenant DB.
-- ============================================================

IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'exam_master' AND COLUMN_NAME = 'exam_remarks' AND DATA_TYPE = 'varchar'
)
BEGIN
    ALTER TABLE exam_master ALTER COLUMN exam_remarks NVARCHAR(1000) NULL;
    PRINT 'exam_master.exam_remarks converted to NVARCHAR(1000).';
END
ELSE IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'exam_master' AND COLUMN_NAME = 'exam_remarks' AND DATA_TYPE = 'nvarchar' AND CHARACTER_MAXIMUM_LENGTH < 1000
)
BEGIN
    ALTER TABLE exam_master ALTER COLUMN exam_remarks NVARCHAR(1000) NULL;
    PRINT 'exam_master.exam_remarks widened to NVARCHAR(1000).';
END
ELSE
    PRINT 'exam_master.exam_remarks already NVARCHAR(1000) or wider — skipped.';
GO
