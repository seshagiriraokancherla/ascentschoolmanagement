-- ============================================================
-- announcements.title: VARCHAR -> NVARCHAR (Unicode)
-- ------------------------------------------------------------
-- Same root cause as exam_master.exam_remarks (see
-- exam_master_remarks_unicode_migration.sql): title was VARCHAR(200), a
-- non-Unicode (codepage) type. Typing Telugu/Hindi into it (now possible via
-- the Announcements page's live transliteration) would silently collapse
-- every non-ASCII character to '?' on save, with no error raised.
--
-- description is already NVARCHAR(MAX) and needs no change.
--
-- This migration only fixes the COLUMN TYPE for future saves. It cannot
-- recover any title already corrupted to '?' before this runs — any affected
-- announcement would need to be retyped after the migration.
--
-- Idempotent: safe to re-run; only alters if still the old VARCHAR type.
-- Run once per existing tenant DB.
-- ============================================================

IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'announcements' AND COLUMN_NAME = 'title' AND DATA_TYPE = 'varchar'
)
BEGIN
    ALTER TABLE announcements ALTER COLUMN title NVARCHAR(200) NOT NULL;
    PRINT 'announcements.title converted to NVARCHAR(200).';
END
ELSE
    PRINT 'announcements.title already NVARCHAR — skipped.';
GO
