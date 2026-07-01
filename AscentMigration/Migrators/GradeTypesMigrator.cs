using AscentMigration.Config;
using AscentMigration.Models.Legacy;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace AscentMigration.Migrators
{
    public class GradeTypesMigrator : BaseMigrator
    {
        public override string Name => "grade_types";

        public GradeTypesMigrator(MigrationConfig config) : base(config) { }

        public override async Task<MigrationResult> RunAsync()
        {
            var result = new MigrationResult { Entity = Name };

            using (var src  = OpenSource())
            using (var dest = OpenDest())
            {
                // 1. Load all legacy rows
                var rows = (await src.QueryAsync<LegacyMarksGrade>("SELECT * FROM SAS_MarksGrade")).AsList();
                result.Total = rows.Count;
                Log($"Found {result.Total} rows in SAS_MarksGrade");

                // 2. Lookups for the two-step subject resolution:
                //    Step 1 (source): legacy SubjID → SubjectNam via SAS_Subjects
                //    Step 2 (dest):   subject_name → subject_id via dest subjects (first match)
                var legacySubjectNameMap = await LoadLegacySubjectNameMap(src);
                var destSubjectIdMap     = await LoadDestSubjectIdMap(dest);
                Log($"Lookups loaded — legacySubjects:{legacySubjectNameMap.Count} destSubjects:{destSubjectIdMap.Count}");

                // 3. Handle Truncate vs Skip
                var mode = Config.GetTableMode(Name);
                Log($"Mode: {mode}");

                if (mode == MigrationMode.Truncate && !Config.DryRun)
                {
                    var deleted = await dest.ExecuteAsync(
                        "DELETE FROM grade_types WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    Log($"Deleted {deleted} existing rows for school_id={Config.SchoolId}");
                }

                // For Skip mode: composite key grade_name|subject_id|grade|min|max
                var existingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (mode == MigrationMode.Skip)
                {
                    var existing = await dest.QueryAsync<GradeTypeKey>(
                        @"SELECT grade_name, ISNULL(subject_id,0) AS subject_id, grade,
                                 min_marks, max_marks
                          FROM grade_types WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    foreach (var e in existing)
                        existingKeys.Add(MakeKey(e.grade_name, e.subject_id == 0 ? (int?)null : e.subject_id,
                                                  e.grade, e.min_marks, e.max_marks));
                }

                // 4. Migrate row by row
                int processed = 0;
                foreach (var row in rows)
                {
                    processed++;
                    LogProgress(processed, result.Total);

                    // --- subject_id: two-step lookup (NULL + warn on miss) ---
                    int? subjectId = null;
                    var legacySubjId = row.SubjID?.Trim();
                    if (!string.IsNullOrWhiteSpace(legacySubjId))
                    {
                        if (legacySubjectNameMap.TryGetValue(legacySubjId, out var subjName)
                            && !string.IsNullOrWhiteSpace(subjName))
                        {
                            if (destSubjectIdMap.TryGetValue(subjName, out var sId))
                                subjectId = sId;
                            else
                                AddError(result, processed, $"subject name '{subjName}' (from SubjID='{legacySubjId}') not found in dest subjects — subject_id set to NULL");
                        }
                        else
                            AddError(result, processed, $"SubjID='{legacySubjId}' not found in SAS_Subjects — subject_id set to NULL");
                    }

                    var gradeName = row.GradName?.Trim();
                    var grade     = row.Grad?.Trim();

                    if (mode == MigrationMode.Skip)
                    {
                        var key = MakeKey(gradeName, subjectId, grade, row.MinMrks, row.MaxMrks);
                        if (existingKeys.Contains(key))
                        {
                            result.Skipped++;
                            continue;
                        }
                    }

                    try
                    {
                        if (!Config.DryRun)
                        {
                            await dest.ExecuteAsync(@"
                                INSERT INTO grade_types
                                    (grade_name, subject_id, max_marks, min_marks, grade,
                                     remarks, status, created_date, created_by, school_id)
                                VALUES
                                    (@GradeName, @SubjectId, @MaxMarks, @MinMarks, @Grade,
                                     @Remarks, @Status, @CreatedDate, @CreatedBy, @SchoolId)",
                                new
                                {
                                    GradeName   = gradeName,
                                    SubjectId   = subjectId,
                                    MaxMarks    = row.MaxMrks,
                                    MinMarks    = row.MinMrks,
                                    Grade       = grade,
                                    Remarks     = row.Rrmrks?.Trim(),
                                    Status      = MapStatus(row.TraStatus),
                                    CreatedDate = DateTime.Now,
                                    CreatedBy   = "migration",
                                    SchoolId    = Config.SchoolId
                                });
                        }

                        result.Migrated++;
                    }
                    catch (Exception ex)
                    {
                        AddError(result, processed, $"INSERT failed for grade='{grade}' (gradeName='{gradeName}'): {ex.Message}");
                    }
                }

                LogProgressDone();
            }

            return result;
        }

        // ---------------------------------------------------------------
        // Lookup loaders
        // ---------------------------------------------------------------

        // Legacy SAS_Subjects: SubJectID → SubjectNam (first wins on dup)
        private async Task<Dictionary<string, string>> LoadLegacySubjectNameMap(SqlConnection src)
        {
            var rows = await src.QueryAsync<LegacySubjRow>(
                "SELECT SubJectID, SubjectNam FROM SAS_Subjects");
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
            {
                var key = r.SubJectID?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(key) && !map.ContainsKey(key))
                    map[key] = r.SubjectNam?.Trim() ?? "";
            }
            return map;
        }

        // Dest subjects: subject_name → subject_id (first match; subjects are per-year so the
        // same name may repeat across years — grade_types has no year, so first wins + warn)
        private async Task<Dictionary<string, int>> LoadDestSubjectIdMap(SqlConnection dest)
        {
            var rows = await dest.QueryAsync<DestSubjRow>(
                "SELECT subject_id, subject_name, academic_year_id FROM subjects WHERE school_id = @SchoolId ORDER BY academic_year_id DESC",
                new { Config.SchoolId });
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
            {
                var key = r.subject_name?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(key)) continue;
                if (!map.ContainsKey(key)) map[key] = r.subject_id;
                else Log($"Warning: subject_name '{key}' exists in multiple years — mapping grades to subject_id={map[key]} (latest year)");
            }
            return map;
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private static string MakeKey(string gradeName, int? subjectId, string grade, double? min, double? max)
        {
            return $"{gradeName?.Trim()}|{subjectId ?? 0}|{grade?.Trim()}|{min}|{max}";
        }

        private static void AddError(MigrationResult result, int rowNo, string reason)
        {
            result.Errors.Add(new MigrationError
            {
                RowIdentifier = $"row {rowNo}",
                Reason        = reason
            });
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

        // ---------------------------------------------------------------
        // Private POCOs for typed Dapper queries
        // ---------------------------------------------------------------

        private class LegacySubjRow { public string SubJectID { get; set; } public string SubjectNam { get; set; } }
        private class DestSubjRow   { public int subject_id { get; set; } public string subject_name { get; set; } public int? academic_year_id { get; set; } }

        private class GradeTypeKey
        {
            public string grade_name { get; set; }
            public int    subject_id { get; set; }
            public string grade      { get; set; }
            public double? min_marks { get; set; }
            public double? max_marks { get; set; }
        }
    }
}
