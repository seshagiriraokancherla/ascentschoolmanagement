using AscentMigration.Config;
using AscentMigration.Models.Legacy;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AscentMigration.Migrators
{
    public class FeeCategoriesMigrator : BaseMigrator
    {
        public override string Name => "fee_categories";

        public FeeCategoriesMigrator(MigrationConfig config) : base(config) { }

        public override async Task<MigrationResult> RunAsync()
        {
            var result = new MigrationResult { Entity = Name };

            using (var src  = OpenSource())
            using (var dest = OpenDest())
            {
                // 1. Load all legacy rows
                var rows = (await src.QueryAsync<LegacyFeeCategory>("SELECT * FROM SAS_FeeCategory")).AsList();
                result.Total = rows.Count;
                Log($"Found {result.Total} rows in SAS_FeeCategory");

                // 2. Handle Truncate vs Skip
                var mode = Config.GetTableMode(Name);
                Log($"Mode: {mode}");

                if (mode == MigrationMode.Truncate && !Config.DryRun)
                {
                    var deleted = await dest.ExecuteAsync(
                        "DELETE FROM fee_categories WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    Log($"Deleted {deleted} existing rows for school_id={Config.SchoolId}");
                }

                // For Skip mode: load existing category names to check duplicates
                var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (mode == MigrationMode.Skip)
                {
                    var names = await dest.QueryAsync<string>(
                        "SELECT category_name FROM fee_categories WHERE school_id = @SchoolId",
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

                    var categoryName = row.FeeCategoryNam?.Trim();
                    if (string.IsNullOrWhiteSpace(categoryName))
                    {
                        result.Errors.Add(new MigrationError
                        {
                            RowIdentifier = $"FeeCatgID={row.FeeCatgID?.Trim() ?? "null"}",
                            Reason        = "FeeCategoryNam is empty — skipped"
                        });
                        continue;
                    }

                    if (mode == MigrationMode.Skip && existingNames.Contains(categoryName))
                    {
                        result.Skipped++;
                        continue;
                    }

                    try
                    {
                        if (!Config.DryRun)
                        {
                            await dest.ExecuteAsync(@"
                                INSERT INTO fee_categories
                                    (category_name, academic_year_id, description,
                                     status, school_id, created_by, created_at)
                                VALUES
                                    (@CategoryName, @AcademicYearId, @Description,
                                     @Status, @SchoolId, @CreatedBy, @CreatedAt)",
                                new
                                {
                                    CategoryName   = categoryName,
                                    AcademicYearId = defaultYearId,
                                    Description    = row.Descrpt?.Trim(),
                                    Status         = MapStatus(row.CategoryStatus),
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
                            RowIdentifier = categoryName,
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
