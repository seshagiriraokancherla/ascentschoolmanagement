-- ============================================================
-- bus_fee_structures — remove duplicate rows + add unique indexes
--
-- BUG FIXED: the Bus Fee Structure screen (Transport → Bus Fee
-- Structure) could show the same term/period more than once, and
-- the amount typed for it seemed to "not clear" between Load clicks.
-- Root cause: bus_fee_structures had NO unique constraint on
-- (route_id, academic_year_id, term_id/fee_period_id, school_id),
-- so if Save ever ran twice concurrently (e.g. a double-click, or a
-- slow request overlapping a new one), TWO rows could end up saved
-- for the same term — and the fee-structure grid query then showed
-- one row per matching saved row, i.e. the term appeared twice.
--
-- The API code (TransportRepository) has been fixed to:
--   1. always show exactly ONE row per term/period (picks the
--      latest saved amount, even if stray duplicates still exist), and
--   2. save inside a single transaction with de-duplicated input,
--      so new duplicates can no longer be created.
-- This migration is the DB-level cleanup + guarantee: it removes any
-- duplicate rows already sitting in the table, and adds unique
-- indexes so the database itself refuses a future duplicate insert.
--
-- Idempotent — safe to re-run. Run once per existing tenant DB.
-- New DBs get the same indexes via tenant_tables.sql.
-- ============================================================

-- Replace with the actual tenant DB name before running.
-- USE ascent_group_1;
-- GO

-- ── Step 1: remove duplicate rows, keeping the most recently saved one ──────
-- "Duplicate" = same route + academic year + term (or same route + academic
-- year + fee period) + school. Keeps the row with the highest
-- bus_fee_structure_id (the latest save) and deletes the rest.
;WITH ranked AS (
    SELECT
        bus_fee_structure_id,
        ROW_NUMBER() OVER (
            PARTITION BY school_id, route_id, academic_year_id,
                         ISNULL(term_id, 0), ISNULL(fee_period_id, 0)
            ORDER BY bus_fee_structure_id DESC
        ) AS rn
    FROM bus_fee_structures
)
DELETE bfs
FROM bus_fee_structures bfs
JOIN ranked r ON r.bus_fee_structure_id = bfs.bus_fee_structure_id
WHERE r.rn > 1;
PRINT 'bus_fee_structures duplicate rows removed: ' + CAST(@@ROWCOUNT AS VARCHAR);
GO

-- ── Step 2: unique indexes so duplicates can never be created again ────────
-- Two filtered indexes (mirrors the fee_concessions term/period pattern) —
-- term_id and fee_period_id are mutually exclusive per row (Term vs Monthly),
-- and SQL Server treats NULLs as distinct, so a single index on both columns
-- would not prevent duplicates on whichever one is NULL for a given row.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_bus_fee_structures_term')
BEGIN
    CREATE UNIQUE INDEX UQ_bus_fee_structures_term
        ON bus_fee_structures (school_id, route_id, academic_year_id, term_id)
        WHERE term_id IS NOT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_bus_fee_structures_period')
BEGIN
    CREATE UNIQUE INDEX UQ_bus_fee_structures_period
        ON bus_fee_structures (school_id, route_id, academic_year_id, fee_period_id)
        WHERE fee_period_id IS NOT NULL;
END
GO

PRINT 'bus_fee_structures_dedupe_migration complete.';
GO
