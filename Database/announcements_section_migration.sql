-- ============================================================
-- Feature 1 (Phase 90): teacher announcements target class + optional section.
-- Adds a nullable section_id FK to announcements.
--   NULL      = whole class (or School scope) — reaches every section.
--   set value = only that section's parents see it.
-- Idempotent: safe to re-run.
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('announcements') AND name = 'section_id')
BEGIN
    ALTER TABLE announcements ADD section_id INT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_announcements_section')
BEGIN
    ALTER TABLE announcements
        ADD CONSTRAINT FK_announcements_section
        FOREIGN KEY (section_id) REFERENCES sections(section_id);
END
GO
