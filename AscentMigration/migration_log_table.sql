-- ============================================================
-- _migration_log — tracks which migrators have completed.
-- Run this manually in SSMS on the destination tenant DB
-- before first use, OR let the tool auto-create it on startup.
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME = '_migration_log'
)
BEGIN
    CREATE TABLE _migration_log (
        id            INT          NOT NULL IDENTITY(1,1),
        migrator_name VARCHAR(50)  NOT NULL,
        run_at        DATETIME     NOT NULL DEFAULT GETDATE(),
        status        VARCHAR(10)  NOT NULL,   -- 'Success' | 'Failed'
        rows_total    INT          NULL,
        rows_migrated INT          NULL,
        rows_skipped  INT          NULL,
        rows_errors   INT          NULL,
        CONSTRAINT PK_migration_log PRIMARY KEY (id)
    );
    PRINT '_migration_log table created.';
END
ELSE
    PRINT '_migration_log already exists — skipping.';
GO

-- ============================================================
-- Useful queries during migration
-- ============================================================

-- View current migration status
-- SELECT * FROM _migration_log ORDER BY run_at DESC;

-- Force re-run a specific migrator (delete its log entry,
-- then set <add key="classes.force" value="true" /> in App.config)
-- DELETE FROM _migration_log WHERE migrator_name = 'classes';

-- Reset all (start fresh)
-- DELETE FROM _migration_log;
