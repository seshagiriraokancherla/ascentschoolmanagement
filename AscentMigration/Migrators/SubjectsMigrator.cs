using AscentMigration.Config;
using AscentMigration.Models.Legacy;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AscentMigration.Migrators
{
    public class SubjectsMigrator : BaseMigrator
    {
        public override string Name => "subjects";

        public SubjectsMigrator(MigrationConfig config) : base(config) { }

        public override async Task<MigrationResult> RunAsync()
        {
            var result = new MigrationResult { Entity = Name };

            using (var src  = OpenSource())
            using (var dest = OpenDest())
            {
                // 1. Load all legacy rows
                var rows = (await src.QueryAsync<LegacySubject>("SELECT * FROM SAS_Subjects")).AsList();
                result.Total = rows.Count;
                Log($"Found {result.Total} rows in SAS_Subjects");

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
                        "DELETE FROM subjects WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    Log($"Deleted {deleted} existing rows for school_id={Config.SchoolId}");
                }

                // For Skip mode: composite key subject_name|academic_year_id (same subject
                // name can repeat across academic years)
                var existingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (mode == MigrationMode.Skip)
                {
                    var existing = await dest.QueryAsync<SubjectLookup>(
                        "SELECT subject_name, academic_year_id FROM subjects WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    foreach (var s in existing)
                        existingKeys.Add(CompositeKey(s.subject_name, s.academic_year_id));
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

                    var subjectName = row.SubjectNam?.Trim();
                    if (string.IsNullOrWhiteSpace(subjectName))
                    {
                        result.Errors.Add(new MigrationError
                        {
                            RowIdentifier = $"SubJectID={row.SubJectID?.Trim() ?? "null"}",
                            Reason        = "SubjectNam is empty — skipped"
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
                            Log($"Warning: academic_year '{acdYear}' not found for subject '{subjectName}' — using fallback academic_year_id={defaultYearId}");
                    }

                    if (mode == MigrationMode.Skip && existingKeys.Contains(CompositeKey(subjectName, academicYearId)))
                    {
                        result.Skipped++;
                        continue;
                    }

                    try
                    {
                        if (!Config.DryRun)
                        {
                            await dest.ExecuteAsync(@"
                                INSERT INTO subjects
                                    (subject_name, short_name, subject_type, description,
                                     remarks, academic_year_id, status, school_id,
                                     created_by, created_at, machine_id)
                                VALUES
                                    (@SubjectName, @ShortName, @SubjectType, @Description,
                                     @Remarks, @AcademicYearId, @Status, @SchoolId,
                                     @CreatedBy, @CreatedAt, @MachineId)",
                                new
                                {
                                    SubjectName    = subjectName,
                                    ShortName      = row.ShortNam?.Trim(),
                                    SubjectType    = row.SubjectTyp?.Trim(),
                                    Description    = row.Descrpt?.Trim(),
                                    Remarks        = row.RemarksDet?.Trim(),
                                    AcademicYearId = academicYearId,
                                    Status         = MapStatus(row.SubjectStatus),
                                    SchoolId       = Config.SchoolId,
                                    CreatedBy      = "migration",
                                    CreatedAt      = DateTime.Now,
                                    MachineId      = row.MachID?.Trim()
                                });
                        }

                        result.Migrated++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(new MigrationError
                        {
                            RowIdentifier = $"{subjectName} (year={acdYear})",
                            Reason        = ex.Message
                        });
                    }
                }

                LogProgressDone();
            }

            return result;
        }

        private static string CompositeKey(string subjectName, int? academicYearId)
        {
            return $"{subjectName?.Trim()}|{academicYearId}";
        }

        private static string MapStatus(string legacy)
        {
            switch (legacy?.Trim())
            {
                case "A": return "Active";
                case "D": return "Deleted";
                default:  return "Active";
            }
        }

        private class AcademicYearLookup
        {
            public int    academic_year_id { get; set; }
            public string academic_year    { get; set; }
        }

        private class SubjectLookup
        {
            public string subject_name     { get; set; }
            public int?   academic_year_id { get; set; }
        }
    }
}
