using AscentMigration.Config;
using AscentMigration.Models.Legacy;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AscentMigration.Migrators
{
    public class FeeTypesMigrator : BaseMigrator
    {
        public override string Name => "fee_types";

        public FeeTypesMigrator(MigrationConfig config) : base(config) { }

        public override async Task<MigrationResult> RunAsync()
        {
            var result = new MigrationResult { Entity = Name };

            using (var src  = OpenSource())
            using (var dest = OpenDest())
            {
                // 1. Load all legacy rows
                var rows = (await src.QueryAsync<LegacyFeeType>("SELECT * FROM SAS_FeeTypes")).AsList();
                result.Total = rows.Count;
                Log($"Found {result.Total} rows in SAS_FeeTypes");

                // 2. Handle Truncate vs Skip
                var mode = Config.GetTableMode(Name);
                Log($"Mode: {mode}");

                if (mode == MigrationMode.Truncate && !Config.DryRun)
                {
                    var deleted = await dest.ExecuteAsync(
                        "DELETE FROM fee_types WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    Log($"Deleted {deleted} existing rows for school_id={Config.SchoolId}");
                }

                // For Skip mode: load existing fee type names to check duplicates
                var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (mode == MigrationMode.Skip)
                {
                    var names = await dest.QueryAsync<string>(
                        "SELECT fee_type_name FROM fee_types WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    foreach (var n in names) existingNames.Add(n);
                }

                // Resolve fallback academic_year_id (config override or latest in dest)
                int defaultYearId = await ResolveDefaultAcademicYearIdAsync(dest);
                Log($"Using academic_year_id = {defaultYearId}");

                // 3. Migrate row by row
                int processed = 0;
                foreach (var row in rows)
                {
                    processed++;
                    LogProgress(processed, result.Total);

                    var feeTypeName = row.FeeTyp?.Trim();
                    if (string.IsNullOrWhiteSpace(feeTypeName))
                    {
                        result.Errors.Add(new MigrationError
                        {
                            RowIdentifier = $"FeeTypID={row.FeeTypID?.Trim() ?? "null"}",
                            Reason        = "FeeTyp is empty — skipped"
                        });
                        continue;
                    }

                    if (mode == MigrationMode.Skip && existingNames.Contains(feeTypeName))
                    {
                        result.Skipped++;
                        continue;
                    }

                    try
                    {
                        if (!Config.DryRun)
                        {
                            await dest.ExecuteAsync(@"
                                INSERT INTO fee_types
                                    (fee_type_name, academic_year_id, sequence_no,
                                     description, status, school_id, created_by, created_at)
                                VALUES
                                    (@FeeTypeName, @AcademicYearId, @SequenceNo,
                                     @Description, @Status, @SchoolId, @CreatedBy, @CreatedAt)",
                                new
                                {
                                    FeeTypeName    = feeTypeName,
                                    AcademicYearId = defaultYearId,
                                    SequenceNo     = row.SeqNo,
                                    Description    = row.FeeDesc?.Trim(),
                                    Status         = MapStatus(row.FeeTypStatus),
                                    SchoolId       = Config.SchoolId,
                                    CreatedBy      = "migration",
                                    CreatedAt      = DateTime.Now
                                });
                        }

                        result.Migrated++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(new MigrationError
                        {
                            RowIdentifier = feeTypeName,
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
