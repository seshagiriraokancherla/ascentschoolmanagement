-- ============================================================
-- cleanup_migration_data.sql
-- Manual alternative to the migration tool's Cleanup=true flag.
-- Deletes all MIGRATED data for one school in reverse FK-dependency
-- order (children first), then clears the migration log so the tool
-- re-runs every migrator on the next pass.
--
-- USAGE
--   1. Connect to the tenant DB:  ascent_group_{N}
--   2. Set @SchoolId below to the school_id you want to wipe.
--   3. Review, then run. Wrapped in a transaction — nothing commits
--      unless every delete succeeds.
--
-- SAFETY
--   If a table OUTSIDE the migration set (e.g. student_attendance,
--   student_marks, fee_concessions created by real usage) still
--   references a migrated row, that DELETE fails and the whole
--   transaction rolls back — meaning the school has live data and
--   should NOT be wiped. Investigate before forcing.
-- ============================================================

DECLARE @SchoolId INT = 1   -- ← set the target school_id

SET XACT_ABORT ON           -- any error aborts the whole transaction
BEGIN TRANSACTION

    -- ── Children first, parents last (reverse of migration insert order) ──
    DELETE FROM fee_receipt_items   WHERE school_id = @SchoolId;
    PRINT 'fee_receipt_items   : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM fee_receipts        WHERE school_id = @SchoolId;
    PRINT 'fee_receipts        : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM students            WHERE school_id = @SchoolId;
    PRINT 'students            : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM sections            WHERE school_id = @SchoolId;
    PRINT 'sections            : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM bus_fee_structures  WHERE school_id = @SchoolId;
    PRINT 'bus_fee_structures  : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM fee_structures      WHERE school_id = @SchoolId;
    PRINT 'fee_structures      : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM buses               WHERE school_id = @SchoolId;
    PRINT 'buses               : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM bus_routes          WHERE school_id = @SchoolId;
    PRINT 'bus_routes          : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM terms               WHERE school_id = @SchoolId;
    PRINT 'terms               : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM fee_types           WHERE school_id = @SchoolId;
    PRINT 'fee_types           : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM classes             WHERE school_id = @SchoolId;
    PRINT 'classes             : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM fee_categories      WHERE school_id = @SchoolId;
    PRINT 'fee_categories      : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM class_groups        WHERE school_id = @SchoolId;
    PRINT 'class_groups        : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM academic_years      WHERE school_id = @SchoolId;
    PRINT 'academic_years      : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM payment_modes       WHERE school_id = @SchoolId;
    PRINT 'payment_modes       : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    -- ── Clear the migration log so the tool re-runs every migrator ──
    -- (Safe if the table doesn't exist yet — guarded.)
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '_migration_log')
    BEGIN
        DELETE FROM _migration_log;
        PRINT '_migration_log      : cleared';
    END

COMMIT TRANSACTION
PRINT '=== Cleanup complete for school_id=' + CAST(@SchoolId AS VARCHAR) + ' ===';
GO
