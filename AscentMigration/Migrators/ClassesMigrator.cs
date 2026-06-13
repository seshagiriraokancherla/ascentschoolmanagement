using AscentMigration.Config;
using AscentMigration.Models.Legacy;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AscentMigration.Migrators
{
    public class ClassesMigrator : BaseMigrator
    {
        public override string Name => "classes";

        public ClassesMigrator(MigrationConfig config) : base(config) { }

        public override async Task<MigrationResult> RunAsync()
        {
            var result = new MigrationResult { Entity = Name };

            using (var src  = OpenSource())
            using (var dest = OpenDest())
            {
                // 1. Load all legacy rows
                var rows = (await src.QueryAsync<LegacyClass>("SELECT * FROM SAS_ClassNames")).AsList();
                result.Total = rows.Count;
                Log($"Found {result.Total} rows in SAS_ClassNames");

                // 2. Pre-load class_groups lookup: group_name → class_group_id
                var groups = await dest.QueryAsync<ClassGroupLookup>(
                    "SELECT class_group_id, group_name FROM class_groups WHERE school_id = @SchoolId",
                    new { Config.SchoolId });
                var groupMap = groups.ToDictionary(
                    g => g.group_name?.Trim() ?? "",
                    g => g.class_group_id,
                    StringComparer.OrdinalIgnoreCase);

                // 3. Handle Truncate vs Skip
                var mode = Config.GetTableMode(Name);
                Log($"Mode: {mode}");

                if (mode == MigrationMode.Truncate && !Config.DryRun)
                {
                    var deleted = await dest.ExecuteAsync(
                        "DELETE FROM classes WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    Log($"Deleted {deleted} existing rows for school_id={Config.SchoolId}");
                }

                // For Skip mode: load existing class names to check duplicates
                var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (mode == MigrationMode.Skip)
                {
                    var names = await dest.QueryAsync<string>(
                        "SELECT class_name FROM classes WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    foreach (var n in names) existingNames.Add(n);
                }

                // 4. Migrate row by row
                int processed = 0;
                foreach (var row in rows)
                {
                    processed++;
                    LogProgress(processed, result.Total);

                    var className = row.ClassNam?.Trim();
                    if (string.IsNullOrWhiteSpace(className))
                    {
                        result.Errors.Add(new MigrationError
                        {
                            RowIdentifier = $"ClassID={row.ClassID?.Trim() ?? "null"}",
                            Reason        = "ClassNam is empty — skipped"
                        });
                        continue;
                    }

                    // Skip mode: skip if already exists
                    if (mode == MigrationMode.Skip && existingNames.Contains(className))
                    {
                        result.Skipped++;
                        continue;
                    }

                    try
                    {
                        // Resolve class_group_id (NULL if CategoryTyp missing or not found)
                        int? classGroupId = null;
                        var categoryTyp   = row.CategoryTyp?.Trim();
                        if (!string.IsNullOrWhiteSpace(categoryTyp))
                        {
                            if (groupMap.TryGetValue(categoryTyp, out var gid))
                                classGroupId = gid;
                            else
                                Log($"Warning: no class_group found for CategoryTyp='{categoryTyp}' (class '{className}') — class_group_id set to NULL");
                        }

                        if (!Config.DryRun)
                        {
                            await dest.ExecuteAsync(@"
                                INSERT INTO classes
                                    (class_name, branch_name, description, status,
                                     class_group_id, prefix_enabled, prefix_code,
                                     sequence_no, school_id, created_by, created_at)
                                VALUES
                                    (@ClassName, @BranchName, @Description, @Status,
                                     @ClassGroupId, @PrefixEnabled, @PrefixCode,
                                     @SequenceNo, @SchoolId, @CreatedBy, @CreatedAt)",
                                new
                                {
                                    ClassName    = className,
                                    BranchName   = row.BranchNam?.Trim(),
                                    Description  = row.ClassDesc?.Trim(),
                                    Status       = MapStatus(row.ClassStat),
                                    ClassGroupId = classGroupId,
                                    PrefixEnabled= row.PrefixStat?.Trim(),
                                    PrefixCode   = row.PrefixCod?.Trim(),
                                    SequenceNo   = row.SeqN,
                                    SchoolId     = Config.SchoolId,
                                    CreatedBy    = "migration",
                                    CreatedAt    = DateTime.Now
                                });
                        }

                        result.Migrated++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(new MigrationError
                        {
                            RowIdentifier = className,
                            Reason        = ex.Message
                        });
                    }
                }

                LogProgressDone();
            }

            return result;
        }

        private class ClassGroupLookup
        {
            public int    class_group_id { get; set; }
            public string group_name     { get; set; }
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
