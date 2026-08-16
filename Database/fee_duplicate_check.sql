/* ============================================================================
   Duplicate fee-line diagnosis  (symptom: "1st Term Fee" shows twice on the
   fee collection screen, and ticking one Term checkbox multiplies the total)

   Run this in SSMS against ONE tenant DB (ascent_group_{N}).
   Sections 1-4 are READ-ONLY. Each cleanup block is commented out and
   transaction-wrapped with ROLLBACK by default - review output, switch to
   COMMIT, then re-run.

   Set the school + (optionally) the academic year you are looking at.
   ============================================================================ */

DECLARE @SchoolId       INT = 1;     -- <<< set this
DECLARE @AcademicYearId INT = NULL;  -- NULL = all years

SET NOCOUNT ON;

/* ---------------------------------------------------------------------------
   SECTION 1 - Duplicate fee_structures rows        <-- most common cause
   ---------------------------------------------------------------------------
   Two (or more) Active structure rows for the SAME class + category + year +
   fee type + term/period. The collection screen shows one line per row, so the
   same term appears twice and the amount doubles.

   Usual origin: the Fee Structure page was saved once with Admission Type =
   "All" (admission_type NULL) and again with "New"/"Old" selected. Saving only
   deletes the slice you had selected, so both slices survive - and the
   collection query accepts NULL *or* the student's join_type, matching both.
   Look at the AdmissionTypes column below to confirm.
--------------------------------------------------------------------------- */
PRINT '=== 1. Duplicate fee_structures rows ===';

SELECT  fs.academic_year_id, ay.academic_year,
        fs.class_id,  c.class_name,
        fs.fee_category_id, fcat.category_name,
        fs.fee_type_id, ft.fee_type_name,
        fs.term_id, t.term_name,
        fs.fee_period_id, fp.period_label,
        COUNT(*)                                        AS RowCnt,
        STRING_AGG(ISNULL(fs.admission_type,'(All)'), ' | ') AS AdmissionTypes,
        STRING_AGG(CONVERT(VARCHAR(20), fs.amount), ' | ')   AS Amounts,
        STRING_AGG(CONVERT(VARCHAR(20), fs.fee_structure_id), ' | ') AS StructureIds
FROM        fee_structures fs
LEFT  JOIN  academic_years ay   ON ay.academic_year_id = fs.academic_year_id
LEFT  JOIN  classes        c    ON c.class_id          = fs.class_id
LEFT  JOIN  fee_categories fcat ON fcat.fee_category_id = fs.fee_category_id
LEFT  JOIN  fee_types      ft   ON ft.fee_type_id      = fs.fee_type_id
LEFT  JOIN  terms          t    ON t.term_id           = fs.term_id
LEFT  JOIN  fee_periods    fp   ON fp.fee_period_id    = fs.fee_period_id
WHERE   fs.school_id = @SchoolId
  AND   ISNULL(fs.status,'Active') <> 'Inactive'
  AND  (@AcademicYearId IS NULL OR fs.academic_year_id = @AcademicYearId)
GROUP BY fs.academic_year_id, ay.academic_year, fs.class_id, c.class_name,
         fs.fee_category_id, fcat.category_name, fs.fee_type_id, ft.fee_type_name,
         fs.term_id, t.term_name, fs.fee_period_id, fp.period_label
HAVING  COUNT(*) > 1
ORDER BY ay.academic_year, c.class_name, ft.fee_type_name, t.term_name, fp.period_label;

/*  -- STRING_AGG needs SQL Server 2017+. On 2016 use this instead:
SELECT  fs.academic_year_id, fs.class_id, fs.fee_category_id, fs.fee_type_id,
        fs.term_id, fs.fee_period_id, COUNT(*) AS RowCnt
FROM    fee_structures fs
WHERE   fs.school_id = @SchoolId
  AND   ISNULL(fs.status,'Active') <> 'Inactive'
  AND  (@AcademicYearId IS NULL OR fs.academic_year_id = @AcademicYearId)
GROUP BY fs.academic_year_id, fs.class_id, fs.fee_category_id, fs.fee_type_id,
         fs.term_id, fs.fee_period_id
HAVING  COUNT(*) > 1;
*/

PRINT '=== 1b. Of those, which are TRUE duplicates vs admission_type overlap ===';

SELECT  fs.academic_year_id, fs.class_id, fs.fee_category_id,
        fs.fee_type_id, ft.fee_type_name, fs.term_id, t.term_name,
        fs.fee_period_id,
        ISNULL(fs.admission_type,'(All)') AS AdmissionType,
        COUNT(*) AS RowsInThisSlice
FROM        fee_structures fs
LEFT  JOIN  fee_types ft ON ft.fee_type_id = fs.fee_type_id
LEFT  JOIN  terms     t  ON t.term_id      = fs.term_id
WHERE   fs.school_id = @SchoolId
  AND   ISNULL(fs.status,'Active') <> 'Inactive'
  AND  (@AcademicYearId IS NULL OR fs.academic_year_id = @AcademicYearId)
GROUP BY fs.academic_year_id, fs.class_id, fs.fee_category_id, fs.fee_type_id,
         ft.fee_type_name, fs.term_id, t.term_name, fs.fee_period_id, fs.admission_type
HAVING  COUNT(*) > 1
ORDER BY fs.academic_year_id, fs.class_id, ft.fee_type_name, t.term_name;

/* ---- CLEANUP 1: TRUE duplicates only (same admission_type slice)
   ---------------------------------------------------------------------------
   This collapses rows returned by 1b - identical key AND identical
   admission_type - keeping the newest fee_structure_id. Safe: within one slice
   the extra rows are pure duplication.

   It deliberately does NOT touch cross-slice overlap (a "(All)" row plus a
   "New"/"Old" row for the same fee+term, i.e. rows in section 1 that do NOT
   appear in 1b). Deactivating the "(All)" row there would remove that fee from
   every student whose join_type doesn't match the surviving row - a real fee
   loss. The API now resolves that overlap at read time (join_type-specific row
   wins, else the newest), so the screen is already correct; clean it up in the
   UI instead - see the note under CLEANUP 1b.

   Deactivate (status='Inactive'), never DELETE - the read queries already
   exclude Inactive rows and the history stays auditable.

   Review 1b, then uncomment, run, check the PRINT, and if the count looks right
   change ROLLBACK to COMMIT and run again.
--------------------------------------------------------------------------- */
/*
BEGIN TRAN;

;WITH ranked AS (
    SELECT  fs.fee_structure_id,
            ROW_NUMBER() OVER (
                PARTITION BY fs.academic_year_id, fs.class_id, fs.fee_category_id,
                             fs.fee_type_id, ISNULL(fs.term_id,0), ISNULL(fs.fee_period_id,0),
                             ISNULL(fs.admission_type,'')
                ORDER BY fs.fee_structure_id DESC
            ) rn
    FROM    fee_structures fs
    WHERE   fs.school_id = @SchoolId
      AND   ISNULL(fs.status,'Active') <> 'Inactive'
      AND  (@AcademicYearId IS NULL OR fs.academic_year_id = @AcademicYearId)
)
UPDATE  fs
SET     fs.status = 'Inactive'
FROM    fee_structures fs
JOIN    ranked r ON r.fee_structure_id = fs.fee_structure_id
WHERE   r.rn > 1;

PRINT CONCAT('fee_structures rows deactivated: ', @@ROWCOUNT);

ROLLBACK;   -- change to COMMIT once the number looks right
*/

/* ---- CLEANUP 1b: admission_type overlap - fix from the UI, not SQL
   ---------------------------------------------------------------------------
   Decide with the school which model they want for that class/category/year:

   (a) One amount for everyone -> Fee Structure page, set Admission Type to the
       specific value ("New", then "Old"), clear every amount to 0 and Save.
       That deletes those slices, leaving only the "(All)" rows.

   (b) Different amounts for New vs Old -> keep the New/Old slices and clear the
       "(All)" slice the same way (select the blank/All Admission Type, zero the
       amounts, Save). Then make sure EVERY join_type in use has its own rows:

         SELECT ISNULL(join_type,'(none)') JoinType, COUNT(*) Students
         FROM   students
         WHERE  school_id = @SchoolId AND status IN ('Active','Y')
         GROUP  BY join_type;

       Students whose join_type is NULL/blank only ever match "(All)" rows, so
       if you drop the "(All)" slice they will show NO fees at all.
--------------------------------------------------------------------------- */


/* ---------------------------------------------------------------------------
   SECTION 2 - Duplicate fee_types master rows
   ---------------------------------------------------------------------------
   If the SAME fee type name exists twice for a year (two fee_type_ids), each
   one carries its own structure rows, so the collection screen shows two lines
   with the SAME name. Section 1 will NOT report these (the DB sees two
   different fee types) and the API cannot dedupe them either - this one has to
   be fixed in the data.
--------------------------------------------------------------------------- */
PRINT '=== 2. Duplicate fee_types (same name within a year) ===';

SELECT  ft.academic_year_id, ay.academic_year, ft.fee_type_name,
        COUNT(*) AS RowCnt,
        STRING_AGG(CONVERT(VARCHAR(20), ft.fee_type_id), ' | ') AS FeeTypeIds,
        STRING_AGG(ISNULL(ft.description,''), ' | ')            AS Descriptions,
        STRING_AGG(ISNULL(ft.status,''), ' | ')                 AS Statuses
FROM        fee_types ft
LEFT  JOIN  academic_years ay ON ay.academic_year_id = ft.academic_year_id
WHERE   ft.school_id = @SchoolId
  AND   ISNULL(ft.status,'Active') NOT IN ('Inactive','N')
  AND  (@AcademicYearId IS NULL OR ft.academic_year_id = @AcademicYearId)
GROUP BY ft.academic_year_id, ay.academic_year, ft.fee_type_name
HAVING  COUNT(*) > 1
ORDER BY ay.academic_year, ft.fee_type_name;

/* ---- CLEANUP 2 (per duplicate pair - do these one at a time, deliberately):
   1. Pick the keeper id (the one already used on receipts - check first):

        SELECT ri.fee_type_id, COUNT(*) Items, SUM(ri.net_amount) Paid
        FROM fee_receipt_items ri
        WHERE ri.fee_type_id IN (<id1>, <id2>) AND ri.school_id = @SchoolId
        GROUP BY ri.fee_type_id;

   2. Re-point the loser's structure rows at the keeper, then let CLEANUP 1
      collapse the resulting duplicates:

        BEGIN TRAN;
        UPDATE fee_structures SET fee_type_id = <keeperId>
        WHERE  fee_type_id = <loserId> AND school_id = @SchoolId;
        PRINT CONCAT('structures re-pointed: ', @@ROWCOUNT);
        ROLLBACK;   -- COMMIT when correct

   3. Retire the loser so it can never be picked again:

        UPDATE fee_types SET status = 'Inactive'
        WHERE  fee_type_id = <loserId> AND school_id = @SchoolId;

   Leave fee_receipt_items history pointing wherever it already points - do NOT
   rewrite paid history.
--------------------------------------------------------------------------- */


/* ---------------------------------------------------------------------------
   SECTION 3 - Duplicate terms / fee_periods
   ---------------------------------------------------------------------------
   Two term rows named "1st Term" in the same year = two term_ids = two Term
   checkboxes and two fee lines that look identical.
--------------------------------------------------------------------------- */
PRINT '=== 3a. Duplicate terms (same name within a year) ===';

SELECT  t.academic_year_id, ay.academic_year, t.term_name,
        COUNT(*) AS RowCnt,
        STRING_AGG(CONVERT(VARCHAR(20), t.term_id), ' | ') AS TermIds
FROM        terms t
LEFT  JOIN  academic_years ay ON ay.academic_year_id = t.academic_year_id
WHERE   t.school_id = @SchoolId
  AND   ISNULL(t.status,'Active') NOT IN ('Inactive','N')
  AND  (@AcademicYearId IS NULL OR t.academic_year_id = @AcademicYearId)
GROUP BY t.academic_year_id, ay.academic_year, t.term_name
HAVING  COUNT(*) > 1
ORDER BY ay.academic_year, t.term_name;

PRINT '=== 3b. Duplicate fee_periods (same label within a year) ===';

SELECT  fp.academic_year_id, ay.academic_year, fp.period_label,
        COUNT(*) AS RowCnt,
        STRING_AGG(CONVERT(VARCHAR(20), fp.fee_period_id), ' | ') AS PeriodIds
FROM        fee_periods fp
LEFT  JOIN  academic_years ay ON ay.academic_year_id = fp.academic_year_id
WHERE   fp.school_id = @SchoolId
  AND   ISNULL(fp.status,'Active') NOT IN ('Inactive','Deleted','N')
  AND  (@AcademicYearId IS NULL OR fp.academic_year_id = @AcademicYearId)
GROUP BY fp.academic_year_id, ay.academic_year, fp.period_label
HAVING  COUNT(*) > 1
ORDER BY ay.academic_year, fp.period_label;

/* ---- CLEANUP 3: same shape as CLEANUP 2 - re-point fee_structures (and
   fee_concessions) from the loser term/period id to the keeper, then retire
   the loser row. Do NOT rewrite fee_receipt_items history. */


/* ---------------------------------------------------------------------------
   SECTION 4 - Duplicate academic_years and duplicate student rows
   ---------------------------------------------------------------------------
   Duplicate academic_years make the SAME year appear as two tabs (each with a
   full set of terms). Duplicate student rows for one year do the same.
--------------------------------------------------------------------------- */
PRINT '=== 4a. Duplicate academic_years ===';

SELECT  ay.academic_year, COUNT(*) AS RowCnt,
        STRING_AGG(CONVERT(VARCHAR(20), ay.academic_year_id), ' | ') AS YearIds
FROM    academic_years ay
WHERE   ay.school_id = @SchoolId
GROUP BY ay.academic_year
HAVING  COUNT(*) > 1;

PRINT '=== 4b. Same student twice in one academic year ===';

SELECT  s.student_unique_id, s.academic_year_id, ay.academic_year,
        COUNT(*) AS RowCnt,
        STRING_AGG(CONVERT(VARCHAR(20), s.student_id), ' | ')  AS StudentIds,
        STRING_AGG(s.admission_no, ' | ')                      AS AdmissionNos,
        MAX(s.student_name)                                    AS StudentName
FROM        students s
LEFT  JOIN  academic_years ay ON ay.academic_year_id = s.academic_year_id
WHERE   s.school_id = @SchoolId
  AND   s.status IN ('Active','Y')
  AND  (@AcademicYearId IS NULL OR s.academic_year_id = @AcademicYearId)
GROUP BY s.student_unique_id, s.academic_year_id, ay.academic_year
HAVING  COUNT(*) > 1
ORDER BY ay.academic_year, s.student_unique_id;

/* Duplicate student rows: see Database\align_student_unique_id.sql (Phase 76)
   - the sync tool used to insert a second row when the legacy admission_no
   changed on promotion. Fix the unique-id alignment first, then decide which
   duplicate row to retire (keep the one referenced by fee_receipts). */


/* ---------------------------------------------------------------------------
   SECTION 5 - Verify one student the way the collection screen sees them
   ---------------------------------------------------------------------------
   Set the two variables and run: one row per fee line the School Fee screen
   will draw for that student/year. Any repeated FeeTypeName + TermName pair
   here is what the user is seeing on screen.
--------------------------------------------------------------------------- */
/*
DECLARE @StudentUniqueId INT = 0;    -- <<< set this
DECLARE @YearId          INT = 0;    -- <<< set this

SELECT  ft.fee_type_name, ft.description,
        t.term_name, fp.period_label,
        ISNULL(fs.admission_type,'(All)') AS AdmissionType,
        fs.amount, fs.fee_structure_id, s.join_type, s.student_id
FROM        students s
JOIN        fee_structures fs ON fs.class_id         = s.class_id
                            AND fs.fee_category_id  = s.fee_category_id
                            AND fs.academic_year_id = s.academic_year_id
                            AND fs.school_id        = s.school_id
                            AND ISNULL(fs.status,'Active') <> 'Inactive'
                            AND (fs.admission_type IS NULL OR fs.admission_type = s.join_type)
INNER JOIN  fee_types   ft ON ft.fee_type_id     = fs.fee_type_id
LEFT  JOIN  terms       t  ON t.term_id          = fs.term_id
LEFT  JOIN  fee_periods fp ON fp.fee_period_id   = fs.fee_period_id
WHERE   s.school_id        = @SchoolId
  AND   s.student_unique_id = @StudentUniqueId
  AND   s.academic_year_id  = @YearId
ORDER BY ft.fee_type_name, t.order_no, fp.sequence_no;
*/
