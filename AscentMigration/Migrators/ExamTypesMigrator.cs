using AscentMigration.Config;
using AscentMigration.Models.Legacy;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AscentMigration.Migrators
{
    public class ExamTypesMigrator : BaseMigrator
    {
        public override string Name => "exam_types";

        public ExamTypesMigrator(MigrationConfig config) : base(config) { }

        public override async Task<MigrationResult> RunAsync()
        {
            var result = new MigrationResult { Entity = Name };

            using (var src  = OpenSource())
            using (var dest = OpenDest())
            {
                // 1. Load distinct legacy exam rows (dedup exact duplicates at the source;
                //    per-year uniqueness is enforced below via the composite key)
                var rows = (await src.QueryAsync<LegacyExamType>(
                    "SELECT DISTINCT ExamNam, acdyear, examremarks, examstatus FROM SAS_ExamNam")).AsList();
                result.Total = rows.Count;
                Log($"Found {result.Total} distinct rows in SAS_ExamNam");

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
                        "DELETE FROM exam_types WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    Log($"Deleted {deleted} existing rows for school_id={Config.SchoolId}");
                }

                // For Skip mode: composite key exam_type_name|academic_year_id (same exam
                // name can repeat across academic years)
                var existingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (mode == MigrationMode.Skip)
                {
                    var existing = await dest.QueryAsync<ExamTypeLookup>(
                        "SELECT exam_type_name, academic_year_id FROM exam_types WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    foreach (var e in existing)
                        existingKeys.Add(CompositeKey(e.exam_type_name, e.academic_year_id));
                }

                // Resolve fallback academic_year_id (config override or latest in dest)
                int defaultYearId = await ResolveDefaultAcademicYearIdAsync(dest);
                Log($"Fallback academic_year_id = {defaultYearId}");

                // Guard against duplicate (name|year) within this same source batch
                var batchKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // 4. Migrate row by row
                int processed = 0;
                foreach (var row in rows)
                {
                    processed++;
                    LogProgress(processed, result.Total);

                    var examName = row.ExamNam?.Trim();
                    if (string.IsNullOrWhiteSpace(examName))
                    {
                        result.Errors.Add(new MigrationError
                        {
                            RowIdentifier = "ExamNam=null",
                            Reason        = "ExamNam is empty — skipped"
                        });
                        continue;
                    }

                    // Resolve academic_year_id by acdyear string; fallback to default
                    int academicYearId = defaultYearId;
                    var acdYear = row.acdyear?.Trim();
                    if (!string.IsNullOrWhiteSpace(acdYear))
                    {
                        if (yearMap.TryGetValue(acdYear, out var mappedId))
                            academicYearId = mappedId;
                        else
                            Log($"Warning: academic_year '{acdYear}' not found for exam '{examName}' — using fallback academic_year_id={defaultYearId}");
                    }

                    var key = CompositeKey(examName, academicYearId);

                    if (mode == MigrationMode.Skip && existingKeys.Contains(key))
                    {
                        result.Skipped++;
                        continue;
                    }

                    // Skip same (name|year) seen earlier in this batch (different remarks/status rows)
                    if (!batchKeys.Add(key))
                    {
                        result.Skipped++;
                        continue;
                    }

                    // examremarks (varchar) → display_order (int); null when unparseable
                    int? displayOrder = null;
                    if (int.TryParse(row.examremarks?.Trim(), out var ord))
                        displayOrder = ord;

                    try
                    {
                        if (!Config.DryRun)
                        {
                            await dest.ExecuteAsync(@"
                                INSERT INTO exam_types
                                    (exam_type_name, academic_year_id, display_order,
                                     school_id, status, created_by, created_at)
                                VALUES
                                    (@ExamTypeName, @AcademicYearId, @DisplayOrder,
                                     @SchoolId, @Status, @CreatedBy, @CreatedAt)",
                                new
                                {
                                    ExamTypeName   = examName,
                                    AcademicYearId = academicYearId,
                                    DisplayOrder   = displayOrder,
                                    SchoolId       = Config.SchoolId,
                                    Status         = MapStatus(row.examstatus),
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
                            RowIdentifier = $"{examName} (year={acdYear})",
                            Reason        = ex.Message
                        });
                    }
                }

                LogProgressDone();
            }

            return result;
        }

        private static string CompositeKey(string examName, int? academicYearId)
        {
            return $"{examName?.Trim()}|{academicYearId}";
        }

        private static string MapStatus(string legacy)
        {
            switch (legacy?.Trim())
            {
                case "A": return "Active";
                case "D": return "Inactive";
                default:  return "Active";
            }
        }

        private class AcademicYearLookup
        {
            public int    academic_year_id { get; set; }
            public string academic_year    { get; set; }
        }

        private class ExamTypeLookup
        {
            public string exam_type_name   { get; set; }
            public int?   academic_year_id { get; set; }
        }
    }
}
