-- ============================================================
-- terms — fix rows mis-tagged to the wrong academic year by the
-- pre-fix migration tool
--
-- BACKGROUND: before the TermsMigrator fix (see CLAUDE.md "Migration
-- tool fix" note), a term whose legacy AcdYear didn't match any
-- migrated academic_years row was silently assigned the LATEST
-- academic_year_id instead of being left unresolved. So terms that
-- actually belong to an older year can be sitting under the current/
-- latest year's academic_year_id — which is exactly why selecting
-- "the current year" on screens like Fee Concession shows terms that
-- look like they belong to other years: they're all tagged with the
-- same (wrong) academic_year_id.
--
-- THE FIX USES A DIFFERENT COLUMN THAN THE ONE THAT WAS MIS-MAPPED:
-- `terms.year_name` was populated from the legacy YearNam column,
-- which is a SEPARATE field from AcdYear (the one that drove
-- academic_year_id) — so year_name still carries the term's true
-- year label even on a row whose academic_year_id was wrongly
-- defaulted. We use year_name to find the CORRECT academic_years row
-- and repoint academic_year_id to it.
--
-- SAFE BY CONSTRUCTION: this only ever changes terms.academic_year_id
-- on the SAME term_id row — term_id itself never changes, so every
-- FK that points at a term (fee_structures, bus_fee_structures,
-- fee_concessions, fee_receipt_items) keeps working exactly as
-- before; only the term's own year classification is corrected.
--
-- USAGE
--   1. Connect to the tenant DB: ascent_group_{N}
--   2. Run STEP 1 to see which academic years have a suspiciously
--      wide spread of year_name values (the symptom).
--   3. Run STEP 2 to see the exact rows that would be repointed and
--      to what — review before applying.
--   4. Run STEP 3 to apply (wrapped in a transaction; only touches
--      rows with an UNAMBIGUOUS single matching academic_years row
--      whose id actually differs from the current one — never
--      guesses).
--   5. Run STEP 1 again to confirm the spread is gone.
-- ============================================================

-- Replace with the actual tenant DB name before running.
-- USE ascent_group_1;
-- GO

-- ── STEP 1: which academic years have mixed year_name values? ─────────────
-- A clean year should show ONE distinct year_name (or blank/NULL) across
-- all its terms. More than one distinct non-blank value is the symptom.
SELECT
    ay.academic_year_id,
    ay.academic_year,
    COUNT(*)                                   AS TermCount,
    COUNT(DISTINCT NULLIF(LTRIM(RTRIM(t.year_name)), '')) AS DistinctYearNames,
    STUFF((
        SELECT DISTINCT ', ' + NULLIF(LTRIM(RTRIM(t2.year_name)), '')
        FROM terms t2
        WHERE t2.academic_year_id = ay.academic_year_id
          AND NULLIF(LTRIM(RTRIM(t2.year_name)), '') IS NOT NULL
        FOR XML PATH('')), 1, 2, '')            AS YearNamesSeen
FROM terms t
JOIN academic_years ay ON ay.academic_year_id = t.academic_year_id
GROUP BY ay.academic_year_id, ay.academic_year
HAVING COUNT(DISTINCT NULLIF(LTRIM(RTRIM(t.year_name)), '')) > 1
ORDER BY ay.academic_year;
GO

-- ── STEP 2: exact rows that would be repointed (review before applying) ───
-- Matches each term's year_name to the academic_years row it should really
-- belong to. Only rows with EXACTLY ONE matching academic_years row are
-- shown (an ambiguous/no match is left alone — see the note after this).
;WITH candidates AS (
    SELECT
        t.term_id, t.term_name, t.year_name,
        t.academic_year_id                          AS CurrentYearId,
        cur.academic_year                            AS CurrentYearLabel,
        correct.academic_year_id                     AS CorrectYearId,
        correct.academic_year                        AS CorrectYearLabel,
        COUNT(*) OVER (PARTITION BY t.term_id)       AS MatchCount
    FROM terms t
    JOIN academic_years cur ON cur.academic_year_id = t.academic_year_id
    JOIN academic_years correct
         ON correct.school_id = t.school_id
        AND correct.academic_year = LTRIM(RTRIM(t.year_name))
    WHERE NULLIF(LTRIM(RTRIM(t.year_name)), '') IS NOT NULL
      AND correct.academic_year_id <> t.academic_year_id
)
SELECT term_id, term_name, year_name, CurrentYearId, CurrentYearLabel, CorrectYearId, CorrectYearLabel
FROM candidates
WHERE MatchCount = 1   -- unambiguous only
ORDER BY CorrectYearLabel, term_name;
GO

-- Rows NOT covered above (for awareness — these need manual judgement,
-- not an automatic repair): blank/NULL year_name (no clue to go on), or
-- year_name matching MORE THAN ONE academic_years row (duplicate
-- academic_year labels still present — dedupe academic_years first).

-- ── STEP 3: apply the repair (transaction-wrapped) ─────────────────────────
BEGIN TRANSACTION;

;WITH candidates AS (
    SELECT
        t.term_id,
        correct.academic_year_id                     AS CorrectYearId,
        COUNT(*) OVER (PARTITION BY t.term_id)       AS MatchCount
    FROM terms t
    JOIN academic_years correct
         ON correct.school_id = t.school_id
        AND correct.academic_year = LTRIM(RTRIM(t.year_name))
    WHERE NULLIF(LTRIM(RTRIM(t.year_name)), '') IS NOT NULL
      AND correct.academic_year_id <> t.academic_year_id
)
UPDATE t
SET t.academic_year_id = c.CorrectYearId
FROM terms t
JOIN candidates c ON c.term_id = t.term_id AND c.MatchCount = 1;

PRINT 'terms repointed: ' + CAST(@@ROWCOUNT AS VARCHAR);

-- Review the row count printed above, then:
--   COMMIT;
-- or, if something looks wrong:
--   ROLLBACK;

-- ============================================================
-- ── ADDENDUM (read-only, diagnostic only): fee_periods ──────────────────
-- `FeePeriodsMigrator` had the identical fallback-to-latest bug (also
-- fixed going forward — see CLAUDE.md), so Monthly-mode fee/bus-fee
-- screens could show the same "periods from other years" symptom.
-- fee_periods has no equivalent of terms.year_name to recover the true
-- year from automatically — year_no is a CALENDAR year, not the academic
-- year label, and a Jan/Feb/Mar period's year_no belongs to the
-- PREVIOUS academic year (school year spans across the calendar-year
-- boundary), so a blind "year_no starts the label" match would
-- misclassify those months. Run this to see whether it's affected at
-- all before deciding whether a manual/targeted fix is worth doing:
SELECT
    ay.academic_year_id, ay.academic_year,
    COUNT(*)                          AS PeriodCount,
    MIN(fp.year_no)                   AS MinYearNo,
    MAX(fp.year_no)                   AS MaxYearNo
FROM fee_periods fp
JOIN academic_years ay ON ay.academic_year_id = fp.academic_year_id
GROUP BY ay.academic_year_id, ay.academic_year
ORDER BY ay.academic_year;
-- A year whose MinYearNo/MaxYearNo spread is wider than one normal
-- school-year boundary (e.g. more than 2 distinct calendar years) is
-- worth a closer look — ask before assuming it needs the same repair.
