using Dapper;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace AscentMigration
{
    public static class MigrationLogger
    {
        private const string CreateTableSql = @"
            IF NOT EXISTS (
                SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_NAME = '_migration_log'
            )
            BEGIN
                CREATE TABLE _migration_log (
                    id            INT          NOT NULL IDENTITY(1,1),
                    migrator_name VARCHAR(50)  NOT NULL,
                    run_at        DATETIME     NOT NULL DEFAULT GETDATE(),
                    status        VARCHAR(10)  NOT NULL,
                    rows_total    INT          NULL,
                    rows_migrated INT          NULL,
                    rows_skipped  INT          NULL,
                    rows_errors   INT          NULL,
                    CONSTRAINT PK_migration_log PRIMARY KEY (id)
                )
            END";

        public static async Task EnsureLogTableAsync(SqlConnection dest)
        {
            await dest.ExecuteAsync(CreateTableSql);
        }

        // Returns true if this migrator has a Success entry
        public static async Task<bool> IsCompletedAsync(SqlConnection dest, string migratorName)
        {
            var count = await dest.ExecuteScalarAsync<int>(
                @"SELECT COUNT(1) FROM _migration_log
                  WHERE migrator_name = @name AND status = 'Success'",
                new { name = migratorName });
            return count > 0;
        }

        // Removes all log entries for this migrator (used on forced re-run)
        public static async Task ClearAsync(SqlConnection dest, string migratorName)
        {
            await dest.ExecuteAsync(
                "DELETE FROM _migration_log WHERE migrator_name = @name",
                new { name = migratorName });
        }

        // Removes ALL log entries (used by the Cleanup phase so every migrator re-runs)
        public static async Task ClearAllAsync(SqlConnection dest)
        {
            await dest.ExecuteAsync("DELETE FROM _migration_log");
        }

        public static async Task WriteAsync(SqlConnection dest, MigrationResult result)
        {
            var status = result.CompletedSuccessfully ? "Success" : "Failed";

            await dest.ExecuteAsync(@"
                INSERT INTO _migration_log
                    (migrator_name, status, rows_total, rows_migrated, rows_skipped, rows_errors)
                VALUES
                    (@Name, @Status, @Total, @Migrated, @Skipped, @Errors)",
                new
                {
                    Name     = result.Entity,
                    Status   = status,
                    Total    = result.Total,
                    Migrated = result.Migrated,
                    Skipped  = result.Skipped,
                    Errors   = result.ErrorCount
                });
        }

        // Prints the current log table contents
        public static async Task PrintLogAsync(SqlConnection dest)
        {
            var rows = await dest.QueryAsync(
                "SELECT migrator_name, status, rows_migrated, rows_errors, run_at FROM _migration_log ORDER BY run_at");

            System.Console.WriteLine("  _migration_log:");
            foreach (var r in rows)
                System.Console.WriteLine(
                    $"    {r.migrator_name,-20} {r.status,-10} migrated={r.rows_migrated}  errors={r.rows_errors}  at={r.run_at:yyyy-MM-dd HH:mm}");
        }
    }
}
