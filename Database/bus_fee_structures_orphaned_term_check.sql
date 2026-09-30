-- ============================================================
-- bus_fee_structures — find rows orphaned by the terms cleanup
--
-- SYMPTOM: after removing duplicate rows from `terms`, the Bus Fee
-- Structure grid shows amount = 0/blank for a term, even though a
-- row with that amount still exists in bus_fee_structures.
--
-- WHY: the amount was saved against whichever duplicate term_id was
-- selected in the dropdown at the time (e.g. term_id = 12, a
-- duplicate of "Term 1"). Deleting that duplicate row from `terms`
-- (keeping term_id = 5, say) did NOT move the linked
-- bus_fee_structures row — it's still sitting there with
-- term_id = 12, but `terms` no longer has a row with that id. The
-- fee-structure grid is built by listing `terms` first and then
-- looking up the amount for each term_id — so the surviving term
-- (id 5) finds nothing, while the real amount is stranded on the
-- now-nonexistent id 12 ("orphaned").
--
-- (bus_fee_structures.term_id has an FK to terms(term_id) — if it's
-- currently enforced, this situation can only happen if the FK was
-- dropped/disabled during the cleanup, or the row was already
-- orphaned before the FK was added. Either way, this script finds
-- and fixes it going forward regardless of how it happened, and the
-- last step re-adds the FK so it can't happen silently again.)
--
-- USAGE
--   1. Connect to the tenant DB: ascent_group_{N}
--   2. Run STEP 1 to see the orphaned rows (amounts that exist but
--      aren't showing on screen).
--   3. Run STEP 2 to see the current (post-cleanup) terms, so you
--      can tell which surviving term_id each orphaned amount
--      belongs to (match by order/sequence — Term 1, Term 2, ...).
--   4. Fill in and run the UPDATE template in STEP 3, one line per
--      orphaned row, to repoint it to the correct surviving term_id.
--      (If two orphaned rows would then collide on the same
--      route+year+term, the UPDATE will fail with a duplicate-key
--      error if UQ_bus_fee_structures_term already exists — that's
--      a sign the amount was saved twice and you should decide which
--      one to keep instead of blindly repointing both.)
--   5. Run STEP 4 to confirm zero orphans remain.
-- ============================================================

-- Replace with the actual tenant DB name before running.
-- USE ascent_group_1;
-- GO

-- ── STEP 1: orphaned bus_fee_structures rows (term side) ────────────────────
-- These rows exist with a real amount, but their term_id no longer exists
-- in `terms` — this is exactly the "amount is 0 on screen but exists in
-- the DB" symptom. route/year are shown so you can find them on the page.
SELECT
    bfs.bus_fee_structure_id,
    bfs.term_id            AS OrphanedTermId,
    bfs.amount             AS Amount,
    bfs.payment_type       AS PaymentType,
    bfs.school_id          AS SchoolId,
    r.route_name           AS RouteName,
    ay.academic_year       AS AcademicYear,
    bfs.academic_year_id   AS AcademicYearId,
    bfs.created_at         AS CreatedAt
FROM bus_fee_structures bfs
LEFT JOIN bus_routes    r  ON r.route_id  = bfs.route_id
LEFT JOIN academic_years ay ON ay.academic_year_id = bfs.academic_year_id
WHERE bfs.term_id IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM terms t WHERE t.term_id = bfs.term_id)
ORDER BY ay.academic_year, r.route_name;
GO

-- ── STEP 1b: same check for fee_period_id (Monthly payment type) ───────────
-- Only relevant if this school also uses Monthly bus fee structures and
-- fee_periods had duplicates removed too.
SELECT
    bfs.bus_fee_structure_id,
    bfs.fee_period_id      AS OrphanedFeePeriodId,
    bfs.amount             AS Amount,
    r.route_name           AS RouteName,
    ay.academic_year       AS AcademicYear,
    bfs.academic_year_id   AS AcademicYearId,
    bfs.created_at         AS CreatedAt
FROM bus_fee_structures bfs
LEFT JOIN bus_routes    r  ON r.route_id  = bfs.route_id
LEFT JOIN academic_years ay ON ay.academic_year_id = bfs.academic_year_id
WHERE bfs.fee_period_id IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM fee_periods fp WHERE fp.fee_period_id = bfs.fee_period_id)
ORDER BY ay.academic_year, r.route_name;
GO

-- ── STEP 2: the surviving terms per academic year (for matching) ──────────
-- Use this to work out which term_id each orphaned row (STEP 1) should now
-- point to — match by term_name / order_no (Term 1, Term 2, ...), NOT by
-- guessing from the old id number.
SELECT
    ay.academic_year, t.academic_year_id, t.term_id, t.term_name, t.order_no
FROM terms t
JOIN academic_years ay ON ay.academic_year_id = t.academic_year_id
ORDER BY ay.academic_year, ISNULL(t.order_no, 9999), t.term_id;
GO

-- ── STEP 3: repair template — fill in one line per orphaned row ───────────
-- bus_fee_structure_id and NewTermId come from comparing STEP 1 and STEP 2
-- (same academic year, matching term name/order). Uncomment and edit:
--
-- UPDATE bus_fee_structures SET term_id = <NewTermId> WHERE bus_fee_structure_id = <IdFromStep1>;
-- UPDATE bus_fee_structures SET term_id = <NewTermId> WHERE bus_fee_structure_id = <IdFromStep1>;
-- ...

-- ── STEP 4: confirm no orphans remain ──────────────────────────────────────
SELECT COUNT(*) AS RemainingOrphanedTermRows
FROM bus_fee_structures bfs
WHERE bfs.term_id IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM terms t WHERE t.term_id = bfs.term_id);
GO

-- ── STEP 5 (optional, once STEP 4 = 0): make sure the FK is actually there ──
-- If the cleanup required dropping/disabling FK_bus_fee_structures_term to
-- delete the duplicate terms rows, re-add it so this can't happen silently
-- again (a future attempt to delete a still-referenced term will then be
-- blocked with a clear error instead of quietly orphaning data).
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_bus_fee_structures_term')
BEGIN
    ALTER TABLE bus_fee_structures WITH CHECK ADD CONSTRAINT FK_bus_fee_structures_term
        FOREIGN KEY (term_id) REFERENCES terms(term_id);
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_bus_fee_structures_period')
BEGIN
    ALTER TABLE bus_fee_structures WITH CHECK ADD CONSTRAINT FK_bus_fee_structures_period
        FOREIGN KEY (fee_period_id) REFERENCES fee_periods(fee_period_id);
END
GO

PRINT 'bus_fee_structures_orphaned_term_check complete.';
GO
