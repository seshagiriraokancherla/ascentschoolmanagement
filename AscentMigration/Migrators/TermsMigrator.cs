using AscentMigration.Config;
using AscentMigration.Models.Legacy;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AscentMigration.Migrators
{
    public class TermsMigrator : BaseMigrator
    {
        public override string Name => "terms";

        public TermsMigrator(MigrationConfig config) : base(config) { }

        public override async Task<MigrationResult> RunAsync()
        {
            var result = new MigrationResult { Entity = Name };

            using (var src  = OpenSource())
            using (var dest = OpenDest())
            {
                // 1. Load only term rows (filter out month rows)
                var rows = (await src.QueryAsync<LegacyTerm>(
                    "SELECT * FROM SAS_TermMonthData WHERE TrmMnthData LIKE '%Term%'")).AsList();
                result.Total = rows.Count;
                Log($"Found {result.Total} term rows in SAS_TermMonthData (LIKE '%Term%')");

                // 2. Pre-load academic_years lookup: academic_year string → academic_year_id
                var acadYears = await dest.QueryAsync<AcademicYearLookup>(
                    "SELECT academic_year_id, academic_year FROM academic_years WHERE school_id = @SchoolId",
                    new { Config.SchoolId });
                // Tolerant of duplicate academic_year rows in dest (e.g. re-runs without
                // cleanup) — ToDictionary throws on dup keys; keep the first id and warn.
                var yearMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var y in acadYears)
                {
                    var key = y.academic_year?.Trim() ?? "";
                    if (!yearMap.ContainsKey(key)) yearMap[key] = y.academic_year_id;
                    else Log($"Warning: duplicate academic_year '{key}' in dest — keeping first (id={yearMap[key]})");
                }

                // 3. Handle Truncate vs Skip
                var mode = Config.GetTableMode(Name);
                Log($"Mode: {mode}");

                if (mode == MigrationMode.Truncate && !Config.DryRun)
                {
                    var deleted = await dest.ExecuteAsync(
                        "DELETE FROM terms WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    Log($"Deleted {deleted} existing rows for school_id={Config.SchoolId}");
                }

                // For Skip mode: composite key term_name|academic_year_id to handle
                // same term name across different academic years
                var existingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (mode == MigrationMode.Skip)
                {
                    var existing = await dest.QueryAsync<TermLookup>(
                        "SELECT term_name, academic_year_id FROM terms WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    foreach (var t in existing)
                        existingKeys.Add(CompositeKey(t.term_name, t.academic_year_id));
                }

                // Resolve fallback academic_year_id (config override or latest in dest)
                int defaultYearId = await ResolveDefaultAcademicYearIdAsync(dest);
                Log($"Fallback academic_year_id = {defaultYearId}");

                // 4. Migrate row by row
                int processed = 0;
                foreach (var row in rows)
                {
                    processed++;
                    LogProgress(processed, result.Total);

                    var termName = row.TrmMnthData?.Trim();
                    if (string.IsNullOrWhiteSpace(termName))
                    {
                        result.Errors.Add(new MigrationError
                        {
                            RowIdentifier = $"TrmID={row.TrmID?.Trim() ?? "null"}",
                            Reason        = "TrmMnthData is empty — skipped"
                        });
                        continue;
                    }

                    // Resolve academic_year_id by AcdYear string; fallback to default
                    int academicYearId = defaultYearId;
                    var acdYear = row.AcdYear?.Trim();
                    if (!string.IsNullOrWhiteSpace(acdYear))
                    {
                        if (yearMap.TryGetValue(acdYear, out var mappedId))
                            academicYearId = mappedId;
                        else
                            Log($"Warning: academic_year '{acdYear}' not found for term '{termName}' — using fallback academic_year_id={defaultYearId}");
                    }

                    if (mode == MigrationMode.Skip && existingKeys.Contains(CompositeKey(termName, academicYearId)))
                    {
                        result.Skipped++;
                        continue;
                    }

                    try
                    {
                        if (!Config.DryRun)
                        {
                            await dest.ExecuteAsync(@"
                                INSERT INTO terms
                                    (term_name, year_name, order_no, description,
                                     academic_year_id, status, school_id, created_by, created_at)
                                VALUES
                                    (@TermName, @YearName, @OrderNo, @Description,
                                     @AcademicYearId, @Status, @SchoolId, @CreatedBy, @CreatedAt)",
                                new
                                {
                                    TermName       = termName,
                                    YearName       = row.YearNam?.Trim(),
                                    OrderNo        = row.OrdrNo,
                                    Description    = row.Descrpt?.Trim(),
                                    AcademicYearId = academicYearId,
                                    Status         = MapStatus(row.TraStatus),
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
                            RowIdentifier = $"{termName} (year={acdYear})",
                            Reason        = ex.Message
                        });
                    }
                }

                LogProgressDone();
            }

            return result;
        }

        private static string CompositeKey(string termName, int? academicYearId)
        {
            return $"{termName?.Trim()}|{academicYearId}";
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

        private class AcademicYearLookup
        {
            public int    academic_year_id { get; set; }
            public string academic_year    { get; set; }
        }

        private class TermLookup
        {
            public string term_name        { get; set; }
            public int?   academic_year_id { get; set; }
        }
    }
}
