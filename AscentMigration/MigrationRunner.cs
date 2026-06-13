using AscentMigration.Config;
using AscentMigration.Migrators;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace AscentMigration
{
    public class MigrationRunner
    {
        private readonly MigrationConfig _config;

        public MigrationRunner(MigrationConfig config)
        {
            _config = config;
        }

        public async Task RunAsync()
        {
            var migrators = BuildMigrators();

            if (migrators.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("No migrators matched the configured Modules list.");
                Console.ResetColor();
                return;
            }

            using (var dest = new SqlConnection(_config.DestConnectionString))
            {
                dest.Open();

                // Auto-create _migration_log table if not present
                await MigrationLogger.EnsureLogTableAsync(dest);

                // Optional cleanup phase — wipe target tables (reverse FK order) + log
                if (_config.Cleanup)
                    await CleanupAsync(dest);

                Console.WriteLine("Current migration log:");
                await MigrationLogger.PrintLogAsync(dest);
                Console.WriteLine();

                var results = new List<MigrationResult>();

                foreach (var migrator in migrators)
                {
                    bool alreadyDone = await MigrationLogger.IsCompletedAsync(dest, migrator.Name);
                    bool forced      = _config.IsForced(migrator.Name);

                    if (alreadyDone && !forced)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine($"[{migrator.Name}] Already completed — skipping.");
                        Console.WriteLine($"  To re-run: add <add key=\"{migrator.Name}.force\" value=\"true\" /> to App.config");
                        Console.ResetColor();
                        Console.WriteLine();
                        continue;
                    }

                    if (alreadyDone && forced)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"[{migrator.Name}] Force re-run — clearing previous log entry.");
                        Console.ResetColor();

                        if (!_config.DryRun)
                            await MigrationLogger.ClearAsync(dest, migrator.Name);
                    }

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"Running: {migrator.Name}...");
                    Console.ResetColor();

                    MigrationResult result;
                    try
                    {
                        result = await migrator.RunAsync();

                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"  [{migrator.Name}] FATAL: {ex.Message}");
                        Console.ResetColor();

                        result = new MigrationResult
                        {
                            Entity               = migrator.Name,
                            CompletedSuccessfully = false,
                            Errors               = new List<MigrationError>
                            {
                                new MigrationError { RowIdentifier = "N/A", Reason = ex.Message }
                            }
                        };
                    }

                    result.Print();

                    if (!_config.DryRun)
                        await MigrationLogger.WriteAsync(dest, result);

                    results.Add(result);
                    Console.WriteLine();
                }

                PrintSummary(results);

                Console.WriteLine();
                Console.WriteLine("Updated migration log:");
                await MigrationLogger.PrintLogAsync(dest);
            }
        }

        // Deletes all migrated tables for the configured school in REVERSE FK-dependency
        // order (children first), then clears the migration log so every migrator re-runs.
        // Enabled via <add key="Cleanup" value="true" /> in App.config.
        private async Task CleanupAsync(SqlConnection dest)
        {
            // Reverse of the migration insert order — children before parents.
            var tables = new[]
            {
                "fee_receipt_items",
                "fee_receipts",
                "students",
                "sections",
                "bus_fee_structures",
                "fee_structures",
                "buses",
                "bus_routes",
                "terms",
                "fee_types",
                "classes",
                "fee_categories",
                "class_groups",
                "academic_years",
                "payment_modes",
            };

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"=== CLEANUP — school_id={_config.SchoolId} (reverse FK order) ===");
            Console.ResetColor();

            if (_config.DryRun)
            {
                Console.WriteLine("  *** DRY RUN — no rows will be deleted ***");
                foreach (var t in tables)
                    Console.WriteLine($"  Would delete from {t} WHERE school_id = {_config.SchoolId}");
                Console.WriteLine();
                return;
            }

            foreach (var t in tables)
            {
                try
                {
                    var n = await dest.ExecuteAsync(
                        $"DELETE FROM {t} WHERE school_id = @SchoolId",
                        new { _config.SchoolId });
                    Console.WriteLine($"  Deleted {n,6} rows from {t}");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  FAILED to clean {t}: {ex.Message}");
                    Console.WriteLine("  (a table outside the migration set still references this one — " +
                                      "the school likely has live data. Aborting cleanup.)");
                    Console.ResetColor();
                    throw;
                }
            }

            // Clear the log so all migrators re-run on this pass
            await MigrationLogger.ClearAllAsync(dest);
            Console.WriteLine("  Cleared _migration_log.");
            Console.WriteLine();
        }

        private List<IMigrator> BuildMigrators()
        {
            // Tables are listed in dependency order:
            // master/lookup tables first, FK-dependent tables after.
            // ORDER MATTERS — parent tables must run before child tables (FK dependencies).
            var all = new List<IMigrator>
            {
                new AcademicYearsMigrator(_config),  // no dependencies — must be first
                new ClassGroupsMigrator(_config),    // no dependencies
                new FeeCategoriesMigrator(_config),  // depends on academic_years
                new ClassesMigrator(_config),        // depends on class_groups + fee_categories
                new FeeTypesMigrator(_config),       // depends on academic_years
                new TermsMigrator(_config),          // depends on academic_years (lookup by year string)
                new FeeStructuresMigrator(_config),  // depends on academic_years, classes, fee_categories, fee_types, terms
                new BusRoutesMigrator(_config),          // no FK deps on migrated tables
                new BusesMigrator(_config),              // no FK deps on migrated tables
                new BusFeeStructuresMigrator(_config),   // depends on bus_routes, academic_years, terms
                new SectionsMigrator(_config),           // depends on classes; reads distinct StuSect from source
                new StudentsMigrator(_config),           // depends on academic_years, classes, sections, bus_routes
                new PaymentModesMigrator(_config),       // no FK deps; appends fixed Online row (is_online=1) after legacy rows
                new FeeReceiptsMigrator(_config),        // depends on academic_years, students, payment_modes, fee_types, terms
                // next migrators will be added here in order
            };

            var active = new List<IMigrator>();
            foreach (var m in all)
            {
                if (_config.ShouldRun(m.Name))
                    active.Add(m);
            }
            return active;
        }

        private void PrintSummary(List<MigrationResult> results)
        {
            Console.WriteLine(new string('=', 45));
            Console.WriteLine("Migration Summary");
            Console.WriteLine(new string('=', 45));

            int totalMigrated = 0, totalSkipped = 0, totalErrors = 0;
            foreach (var r in results)
            {
                totalMigrated += r.Migrated;
                totalSkipped  += r.Skipped;
                totalErrors   += r.ErrorCount;
                r.Print();
            }

            Console.WriteLine(new string('-', 45));
            Console.WriteLine($"  Total migrated : {totalMigrated}");
            Console.WriteLine($"  Total skipped  : {totalSkipped}");
            Console.WriteLine($"  Total errors   : {totalErrors}");

            if (_config.DryRun)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine();
                Console.WriteLine("  *** DRY RUN — no data was written ***");
                Console.ResetColor();
            }
        }
    }
}
