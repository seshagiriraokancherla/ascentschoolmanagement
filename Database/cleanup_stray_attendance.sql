-- ============================================================
-- cleanup_stray_attendance.sql
-- Removes "stray" attendance rows that were saved against NON-current
-- academic-year student rows by the old all-years attendance grid bug
-- (fixed in Phase 66/67 — the grid/reports/dashboard now scope to the
-- current academic year, so these rows are already excluded from counts;
-- this script physically removes them).
--
-- HOW TO USE
--   1. Connect to the tenant DB (ascent_group_{N}) in SSMS.
--   2. Set @SchoolId below.
--   3. Run as-is — it ONLY PREVIEWS (two SELECTs). Nothing is deleted.
--   4. Review the previews, then uncomment ONE delete block and run again.
--
-- SAFETY
--   * academic_years has no real date range, so we cannot date-match.
--   * The "SAFE DELETE" removes only PROVABLE duplicates: a prior-year row
--     for a physical student (matched by student_unique_id) who ALSO has a
--     current-year attendance row for the SAME date — impossible to create
--     legitimately, so always a bug artifact. Genuine historical attendance
--     is NOT touched.
--   * The "AGGRESSIVE DELETE" wipes ALL non-current-year attendance — use
--     only if you do not need any prior-year attendance history.
--   * Both deletes are transaction-wrapped; review @@ROWCOUNT then COMMIT.
-- ============================================================

-- USE ascent_group_1;   -- <- set your tenant DB
DECLARE @SchoolId INT = 1;   -- <- set to your school_id (ascent_master.schools)

DECLARE @CurrentYearId INT = (
    SELECT TOP 1 academic_year_id FROM academic_years
    WHERE school_id = @SchoolId AND status = 'Active'
    ORDER BY academic_year_id DESC);

IF @CurrentYearId IS NULL
BEGIN
    RAISERROR('No Active academic year found for SchoolId %d — aborting.', 16, 1, @SchoolId);
    RETURN;
END
PRINT 'Current academic_year_id = ' + CAST(@CurrentYearId AS VARCHAR(10));

-- ── PREVIEW 1: every attendance row tied to a NON-current-year student row ──
PRINT '--- PREVIEW 1: all attendance on non-current-year student rows ---';
SELECT sa.attendance_id, sa.student_id, s.student_unique_id, s.admission_no, s.student_name,
       ay.academic_year AS WrongYear, sa.attendance_date, sa.status, sa.marked_by, sa.marked_at
FROM   student_attendance sa
JOIN   students s            ON s.student_id        = sa.student_id
LEFT JOIN academic_years ay  ON ay.academic_year_id = s.academic_year_id
WHERE  sa.school_id = @SchoolId
  AND  s.academic_year_id <> @CurrentYearId
ORDER BY sa.attendance_date DESC, s.student_name;

-- ── PREVIEW 2: provable duplicates (SAFE DELETE targets these) ──────────────
PRINT '--- PREVIEW 2: provable stray duplicates (same student + same date also marked in current year) ---';
SELECT sa.attendance_id, sa.student_id, s.student_unique_id, s.admission_no, s.student_name,
       ay.academic_year AS WrongYear, sa.attendance_date, sa.status, sa.marked_at
FROM   student_attendance sa
JOIN   students s            ON s.student_id        = sa.student_id
LEFT JOIN academic_years ay  ON ay.academic_year_id = s.academic_year_id
WHERE  sa.school_id = @SchoolId
  AND  s.academic_year_id <> @CurrentYearId
  AND  EXISTS (
        SELECT 1
        FROM   student_attendance sa2
        JOIN   students s2 ON s2.student_id = sa2.student_id
        WHERE  sa2.school_id        = @SchoolId
          AND  s2.academic_year_id  = @CurrentYearId
          AND  s2.student_unique_id = s.student_unique_id
          AND  sa2.attendance_date  = sa.attendance_date
       )
ORDER BY sa.attendance_date DESC, s.student_name;


/* ============================================================
   SAFE DELETE — provable duplicates only (recommended)
   Uncomment, run, check the printed count, then COMMIT.
   ============================================================
SET XACT_ABORT ON;
BEGIN TRAN;
    DELETE sa
    FROM   student_attendance sa
    JOIN   students s ON s.student_id = sa.student_id
    WHERE  sa.school_id = @SchoolId
      AND  s.academic_year_id <> @CurrentYearId
      AND  EXISTS (
            SELECT 1
            FROM   student_attendance sa2
            JOIN   students s2 ON s2.student_id = sa2.student_id
            WHERE  sa2.school_id        = @SchoolId
              AND  s2.academic_year_id  = @CurrentYearId
              AND  s2.student_unique_id = s.student_unique_id
              AND  sa2.attendance_date  = sa.attendance_date
           );
    PRINT CAST(@@ROWCOUNT AS VARCHAR(10)) + ' provable stray duplicate(s) deleted.';
-- COMMIT;     -- uncomment to apply
ROLLBACK;      -- default: rolls back so a dry-run does not change data
*/


/* ============================================================
   AGGRESSIVE DELETE — ALL non-current-year attendance
   Use ONLY if you do NOT need any prior-year attendance history.
   This DOES delete legitimate past-year attendance.
   ============================================================
SET XACT_ABORT ON;
BEGIN TRAN;
    DELETE sa
    FROM   student_attendance sa
    JOIN   students s ON s.student_id = sa.student_id
    WHERE  sa.school_id = @SchoolId
      AND  s.academic_year_id <> @CurrentYearId;
    PRINT CAST(@@ROWCOUNT AS VARCHAR(10)) + ' non-current-year row(s) deleted.';
-- COMMIT;
ROLLBACK;
*/
