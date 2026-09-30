-- ============================================================
-- exam_master_time_migration.sql
-- Adds exam_master.exam_time — a per-exam-row start time ("HH:mm", 24-hour),
-- alongside the existing exam_date. Lets Exam Master carry a full schedule
-- (date + time), which the Exam Timetable feature (mobile student + teacher)
-- and the Exam Hall Ticket print now also read.
--
-- Run once per existing tenant DB. Idempotent (checked via sys.columns).
-- No backfill — existing rows simply have no time until edited/re-imported.
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('exam_master') AND name = 'exam_time'
)
BEGIN
    ALTER TABLE exam_master ADD exam_time VARCHAR(5) NULL;
    PRINT 'Added exam_master.exam_time';
END
ELSE
    PRINT 'exam_master.exam_time already exists — skipping';
GO
