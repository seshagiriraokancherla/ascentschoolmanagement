using AscentMigration.Config;
using AscentMigration.Models.Legacy;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AscentMigration.Migrators
{
    public class ClassGroupsMigrator : BaseMigrator
    {
        public override string Name => "class_groups";

        public ClassGroupsMigrator(MigrationConfig config) : base(config) { }

        public override async Task<MigrationResult> RunAsync()
        {
            var result = new MigrationResult { Entity = Name };

            using (var src  = OpenSource())
            using (var dest = OpenDest())
            {
                // 1. Load all legacy rows
                var rows = (await src.QueryAsync<LegacyClassGroup>("SELECT * FROM SAS_ClassGrps")).AsList();
                result.Total = rows.Count;
                Log($"Found {result.Total} rows in SAS_ClassGrps");

                // 2. Handle Truncate vs Skip
                var mode = Config.GetTableMode(Name);
                Log($"Mode: {mode}");

                if (mode == MigrationMode.Truncate && !Config.DryRun)
                {
                    var deleted = await dest.ExecuteAsync(
                        "DELETE FROM class_groups WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    Log($"Deleted {deleted} existing rows for school_id={Config.SchoolId}");
                }

                // For Skip mode: load existing group names to check duplicates
                var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (mode == MigrationMode.Skip)
                {
                    var names = await dest.QueryAsync<string>(
                        "SELECT group_name FROM class_groups WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    foreach (var n in names) existingNames.Add(n);
                }

                // 3. Migrate row by row
                int processed = 0;
                foreach (var row in rows)
                {
                    processed++;
                    LogProgress(processed, result.Total);

                    var groupName = row.GrpNam?.Trim();
                    if (string.IsNullOrWhiteSpace(groupName))
                    {
                        result.Errors.Add(new MigrationError
                        {
                            RowIdentifier = $"ClsGrpID={row.ClsGrpID?.Trim() ?? "null"}",
                            Reason        = "GrpNam is empty — skipped"
                        });
                        continue;
                    }

                    if (mode == MigrationMode.Skip && existingNames.Contains(groupName))
                    {
                        result.Skipped++;
                        continue;
                    }

                    try
                    {
                        if (!Config.DryRun)
                        {
                            await dest.ExecuteAsync(@"
                                INSERT INTO class_groups
                                    (group_name, description, prefix, status,
                                     school_id, created_by, created_at)
                                VALUES
                                    (@GroupName, @Description, @Prefix, @Status,
                                     @SchoolId, @CreatedBy, @CreatedAt)",
                                new
                                {
                                    GroupName   = groupName,
                                    Description = row.GrpDesc?.Trim(),
                                    Prefix      = row.GrpPrfx?.Trim(),
                                    Status      = MapStatus(row.GrpStat),
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
                            RowIdentifier = groupName,
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
                case "I": return "Inactive";
                case "D": return "Inactive";
                default:  return "Inactive";
            }
        }
    }
}
