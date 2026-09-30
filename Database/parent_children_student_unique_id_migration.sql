-- ============================================================
-- parent_children_student_unique_id_migration.sql
-- Adds parent_children.student_unique_id (ascent_master) and backfills
-- it for every EXISTING link, so the "Mobile App Adoption" report can
-- match parents to students accurately even after a promotion changes
-- admission_no. Run ONCE on ascent_master — NOT per tenant.
--
-- WHY: parent_children previously only stored student_id (a year-
-- specific IDENTITY that changes on promotion) and admission_no (which
-- some schools renumber on promotion, per Phase 76). student_unique_id
-- is the one identifier that stays stable across both. New/refreshed
-- links (login, child switch) already write it once the API is deployed
-- with this column present — this script catches up every link that
-- already exists so the report is accurate from day one, not just for
-- future logins.
--
-- HOW: for each distinct tenant DB referenced by parent_children.db_name,
-- match its rows to that DB's students table (by student_id first, else
-- admission_no+school_id) and copy student_unique_id across. Requires
-- every tenant DB to be on the SAME SQL Server instance as ascent_master
-- (true for this deployment) — uses a three-part cross-database name per
-- referenced DB via dynamic SQL.
--
-- SAFETY: purely additive — only fills rows where student_unique_id IS
-- NULL; never overwrites an existing value or touches any other column.
-- A tenant DB that's unreachable/missing is skipped (printed) without
-- aborting the rest. Re-runnable any time (idempotent — nothing left to
-- do for already-matched rows on a second run).
-- ============================================================

USE ascent_master;
GO

-- ── Step 1: add the column (idempotent) ──────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('parent_children') AND name = 'student_unique_id')
BEGIN
    ALTER TABLE parent_children ADD student_unique_id INT NULL;
    PRINT 'Added parent_children.student_unique_id';
END
ELSE
    PRINT 'parent_children.student_unique_id already exists — skipping ALTER';
GO

-- ── Step 2: preview — how many rows per tenant DB still need backfilling ──
SELECT db_name,
       COUNT(*)                                                   AS TotalLinks,
       SUM(CASE WHEN student_unique_id IS NULL THEN 1 ELSE 0 END) AS StillMissing
FROM parent_children
GROUP BY db_name
ORDER BY db_name;
GO

-- ── Step 3: backfill — one dynamic UPDATE per distinct tenant DB referenced ──
-- Match priority: exact student_id (fast path, current-year links) else
-- admission_no + school_id, latest academic year on a tie (covers a link
-- whose student_id has gone stale after a promotion).
DECLARE @dbName SYSNAME;
DECLARE @sql     NVARCHAR(MAX);

DECLARE db_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT DISTINCT db_name
    FROM parent_children
    WHERE db_name IS NOT NULL
      AND student_unique_id IS NULL
      AND db_name LIKE 'ascent[_]group[_]%';   -- defensive: only touch our own naming convention

OPEN db_cursor;
FETCH NEXT FROM db_cursor INTO @dbName;

WHILE @@FETCH_STATUS = 0
BEGIN
    BEGIN TRY
        SET @sql = N'
            UPDATE pc
            SET pc.student_unique_id = COALESCE(byId.student_unique_id, byAdm.student_unique_id)
            FROM ascent_master.dbo.parent_children pc
            OUTER APPLY (
                SELECT TOP 1 s.student_unique_id
                FROM ' + QUOTENAME(@dbName) + N'.dbo.students s
                WHERE s.student_id = pc.student_id
            ) byId
            OUTER APPLY (
                SELECT TOP 1 s2.student_unique_id
                FROM ' + QUOTENAME(@dbName) + N'.dbo.students s2
                WHERE s2.admission_no = pc.admission_no AND s2.school_id = pc.school_id
                ORDER BY s2.academic_year_id DESC
            ) byAdm
            WHERE pc.db_name = @p_dbName
              AND pc.student_unique_id IS NULL
              AND (byId.student_unique_id IS NOT NULL OR byAdm.student_unique_id IS NOT NULL);';

        EXEC sp_executesql @sql, N'@p_dbName VARCHAR(100)', @p_dbName = @dbName;
        PRINT 'Backfilled parent_children for ' + @dbName + ' (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows)';
    END TRY
    BEGIN CATCH
        PRINT 'Skipped ' + @dbName + ' — ' + ERROR_MESSAGE();
    END CATCH

    FETCH NEXT FROM db_cursor INTO @dbName;
END

CLOSE db_cursor;
DEALLOCATE db_cursor;
GO

-- ── Step 4: verify — StillMissing should shrink toward 0 ─────────────
-- Any remainder is a link whose student couldn't be found at all (e.g. a
-- deleted student) — harmless; the report's admission_no fallback still
-- covers it, and it self-heals the next time that parent logs in.
SELECT db_name,
       COUNT(*)                                                   AS TotalLinks,
       SUM(CASE WHEN student_unique_id IS NULL THEN 1 ELSE 0 END) AS StillMissing
FROM parent_children
GROUP BY db_name
ORDER BY db_name;
GO
