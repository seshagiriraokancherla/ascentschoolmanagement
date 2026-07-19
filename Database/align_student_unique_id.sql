-- ============================================================
-- align_student_unique_id.sql
-- One-time alignment of the new-DB students.student_unique_id with the
-- legacy StuUnqID, for students created by the SYNC TOOL's "Export
-- Students" BEFORE StuUnqID export existed (those rows got a
-- web-generated unique id via MAX+1 / reuse-by-admission_no).
--
-- WHY: the sync now matches existing students by student_unique_id +
-- academic_year (so a legacy admission_no change on promotion no longer
-- creates a duplicate). Rows whose student_unique_id != legacy StuUnqID
-- would fail that match on the next sync and could duplicate. This
-- script aligns them so future syncs match cleanly.
--
-- NOT NEEDED for schools migrated via AscentMigration — StudentsMigrator
-- already stored student_unique_id = StuUnqID.
--
-- PRECONDITIONS
--   * The tenant DB and the legacy VB6 DB are on the SAME SQL Server
--     instance (this uses a cross-database join). If they are on
--     different instances, set up a linked server or export/import the
--     (StuAmnNo, StuAcdYear, StuUnqID) triples into a temp table first.
--   * Set @SchoolId, the tenant DB (USE ...), and the legacy DB name
--     ([ascent_legacy]) below.
--
-- SAFETY: Step 1 is a read-only PREVIEW. Step 2 is transaction-wrapped
-- and defaults to ROLLBACK — review the PRINT, then switch ROLLBACK to
-- COMMIT to apply.
--
-- LIMITATIONS (read before running)
--   * Legacy SAS_StudentMaster holds ONE (current) row per student, so
--     only the matching current admission_no + year row is aligned;
--     historical prior-year rows in the new DB are not touched. That is
--     enough to stop FUTURE sync duplicates (the sync only ever pushes
--     the current legacy state).
--   * If a student already has duplicate rows for the SAME year (created
--     by the old admission_no-only match), this does not merge them —
--     inspect the duplicate report at the bottom and clean up manually.
-- ============================================================

USE ascent_group_1;          -- ← target tenant DB
DECLARE @SchoolId INT = 1;   -- ← target school_id

-- ── Step 1: PREVIEW — rows whose student_unique_id would change ──
;WITH legacy AS (
    SELECT LTRIM(RTRIM(l.StuAmnNo))  COLLATE DATABASE_DEFAULT AS AdmissionNo,
           LTRIM(RTRIM(l.StuAcdYear)) COLLATE DATABASE_DEFAULT AS AcademicYear,
           TRY_CAST(l.StuUnqID AS INT) AS StuUnqID
    FROM [ascent_legacy].dbo.SAS_StudentMaster l   -- ← legacy DB name
    WHERE l.StuUnqID IS NOT NULL
)
SELECT s.student_id, s.admission_no, ay.academic_year,
       s.student_unique_id AS CurrentUniqueId,
       lg.StuUnqID         AS TargetUniqueId
FROM students s
JOIN academic_years ay ON ay.academic_year_id = s.academic_year_id
JOIN legacy lg
  ON lg.AdmissionNo  = s.admission_no                  COLLATE DATABASE_DEFAULT
 AND lg.AcademicYear = ay.academic_year COLLATE DATABASE_DEFAULT
WHERE s.school_id = @SchoolId
  AND ISNULL(s.student_unique_id, -1) <> lg.StuUnqID
ORDER BY s.admission_no;

-- ── Step 2: APPLY (review Step 1 first, then set COMMIT) ──
SET XACT_ABORT ON;
BEGIN TRANSACTION;

;WITH legacy AS (
    SELECT LTRIM(RTRIM(l.StuAmnNo))  COLLATE DATABASE_DEFAULT AS AdmissionNo,
           LTRIM(RTRIM(l.StuAcdYear)) COLLATE DATABASE_DEFAULT AS AcademicYear,
           TRY_CAST(l.StuUnqID AS INT) AS StuUnqID
    FROM [ascent_legacy].dbo.SAS_StudentMaster l   -- ← legacy DB name
    WHERE l.StuUnqID IS NOT NULL
)
UPDATE s
SET s.student_unique_id = lg.StuUnqID
FROM students s
JOIN academic_years ay ON ay.academic_year_id = s.academic_year_id
JOIN legacy lg
  ON lg.AdmissionNo  = s.admission_no                  COLLATE DATABASE_DEFAULT
 AND lg.AcademicYear = ay.academic_year COLLATE DATABASE_DEFAULT
WHERE s.school_id = @SchoolId
  AND ISNULL(s.student_unique_id, -1) <> lg.StuUnqID;

PRINT CAST(@@ROWCOUNT AS VARCHAR) + ' student rows aligned.';

ROLLBACK TRANSACTION;   -- ← change to COMMIT TRANSACTION to apply
GO

-- ── Optional: same-year duplicate report (student appearing twice in one year) ──
-- Run after aligning to spot rows the old admission_no-only match duplicated.
DECLARE @SchoolId INT = 1;
SELECT student_unique_id, academic_year_id, COUNT(*) AS Rows,
       STRING_AGG(CAST(student_id AS VARCHAR) + ':' + admission_no, ', ') AS StudentRows
FROM students
WHERE school_id = @SchoolId AND student_unique_id IS NOT NULL
GROUP BY student_unique_id, academic_year_id
HAVING COUNT(*) > 1
ORDER BY student_unique_id;
GO
