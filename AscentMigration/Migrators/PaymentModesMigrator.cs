using AscentMigration.Config;
using AscentMigration.Models.Legacy;
using Dapper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AscentMigration.Migrators
{
    public class PaymentModesMigrator : BaseMigrator
    {
        public override string Name => "payment_modes";

        public PaymentModesMigrator(MigrationConfig config) : base(config) { }

        public override async Task<MigrationResult> RunAsync()
        {
            var result = new MigrationResult { Entity = Name };

            using (var src  = OpenSource())
            using (var dest = OpenDest())
            {
                // 1. Load all legacy rows
                var rows = (await src.QueryAsync<LegacyPaymentMode>("SELECT * FROM SAS_PaymentMods")).AsList();
                // +1 for the fixed Online row
                result.Total = rows.Count + 1;
                Log($"Found {rows.Count} rows in SAS_PaymentMods (+1 fixed Online row = {result.Total} total)");

                // 2. Handle Truncate vs Skip
                var mode = Config.GetTableMode(Name);
                Log($"Mode: {mode}");

                if (mode == MigrationMode.Truncate && !Config.DryRun)
                {
                    var deleted = await dest.ExecuteAsync(
                        "DELETE FROM payment_modes WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    Log($"Deleted {deleted} existing rows for school_id={Config.SchoolId}");
                }

                // For Skip mode: dedup by mode_name
                var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (mode == MigrationMode.Skip)
                {
                    var names = await dest.QueryAsync<string>(
                        "SELECT mode_name FROM payment_modes WHERE school_id = @SchoolId AND mode_name IS NOT NULL",
                        new { Config.SchoolId });
                    foreach (var n in names) existingNames.Add(n);
                }

                // 3. Migrate legacy rows (is_online = 0)
                int processed = 0;
                foreach (var row in rows)
                {
                    processed++;
                    LogProgress(processed, result.Total);

                    var modeName = row.TraTyp?.Trim();
                    if (string.IsNullOrWhiteSpace(modeName))
                    {
                        result.Errors.Add(new MigrationError
                        {
                            RowIdentifier = $"TraID={row.TraID?.Trim() ?? "null"}",
                            Reason        = "TraTyp is empty — skipped"
                        });
                        continue;
                    }

                    if (mode == MigrationMode.Skip && existingNames.Contains(modeName))
                    {
                        result.Skipped++;
                        continue;
                    }

                    try
                    {
                        if (!Config.DryRun)
                        {
                            await dest.ExecuteAsync(@"
                                INSERT INTO payment_modes
                                    (mode_name, description, status, is_online,
                                     school_id, created_by, created_at)
                                VALUES
                                    (@ModeName, @Description, @Status, 0,
                                     @SchoolId, @CreatedBy, @CreatedAt)",
                                new
                                {
                                    ModeName    = modeName,
                                    Description = row.TraDesc?.Trim(),
                                    Status      = MapStatus(row.TraStatus),
                                    SchoolId    = Config.SchoolId,
                                    CreatedBy   = "migration",
                                    CreatedAt   = DateTime.Now
                                });
                        }

                        result.Migrated++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(new MigrationError
                        {
                            RowIdentifier = modeName,
                            Reason        = ex.Message
                        });
                    }
                }

                // 4. Insert fixed Online row (is_online = 1)
                processed++;
                LogProgress(processed, result.Total);

                if (mode == MigrationMode.Skip && existingNames.Contains("Online"))
                {
                    Log("Online row already exists — skipping");
                    result.Skipped++;
                }
                else
                {
                    try
                    {
                        if (!Config.DryRun)
                        {
                            await dest.ExecuteAsync(@"
                                INSERT INTO payment_modes
                                    (mode_name, description, status, is_online,
                                     school_id, created_by, created_at)
                                VALUES
                                    ('Online', 'Online', 'Active', 1,
                                     @SchoolId, @CreatedBy, @CreatedAt)",
                                new
                                {
                                    SchoolId  = Config.SchoolId,
                                    CreatedBy = "migration",
                                    CreatedAt = DateTime.Now
                                });
                        }

                        Log("Inserted fixed Online row (is_online=1)");
                        result.Migrated++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(new MigrationError
                        {
                            RowIdentifier = "Online (fixed row)",
                            Reason        = ex.Message
                        });
                    }
                }

                LogProgressDone();
            }

            return result;
        }

        private static string MapStatus(string legacy)
        {
            switch (legacy?.Trim())
            {
                case "A": return "Active";
                case "D": return "Inactive";
                default:  return "Inactive";
            }
        }
    }
}
