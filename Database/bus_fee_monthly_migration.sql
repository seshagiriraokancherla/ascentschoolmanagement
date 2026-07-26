-- ============================================================
-- Bus/Transport fee — Monthly (fee_periods) support migration
-- Adds payment_type + fee_period_id to bus_fee_structures, and the
-- Monthly transport concession unique index to fee_concessions.
-- Idempotent — safe to re-run. Run on all existing tenant DBs.
-- New DBs get these via tenant_tables.sql.
-- ============================================================

-- Replace with the actual tenant DB name before running.
-- USE ascent_group_1;
-- GO

-- ── bus_fee_structures: fee_period_id + payment_type ─────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('bus_fee_structures') AND name = 'fee_period_id')
BEGIN
    ALTER TABLE bus_fee_structures ADD fee_period_id INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('bus_fee_structures') AND name = 'payment_type')
BEGIN
    ALTER TABLE bus_fee_structures ADD payment_type VARCHAR(10) NOT NULL DEFAULT 'Term';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_bus_fee_structures_period')
BEGIN
    ALTER TABLE bus_fee_structures ADD CONSTRAINT FK_bus_fee_structures_period
        FOREIGN KEY (fee_period_id) REFERENCES fee_periods(fee_period_id);
END
GO

-- ── fee_concessions: Monthly transport concession index ──────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_fee_concessions_transport_period')
BEGIN
    CREATE UNIQUE INDEX UQ_fee_concessions_transport_period
        ON fee_concessions (student_id, bus_route_id, school_id, fee_period_id)
        WHERE status = 'Active' AND bus_route_id IS NOT NULL AND fee_period_id IS NOT NULL;
END
GO

PRINT 'bus_fee_monthly_migration complete.';
GO
