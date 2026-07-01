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
    public class ExamMasterMigrator : BaseMigrator
    {
        public override string Name => "exam_master";

        public ExamMasterMigrator(MigrationConfig config) : base(config) { }

        public override async Task<MigrationResult> RunAsync()
        {
            var result = new MigrationResult { Entity = Name };

            using (var src  = OpenSource())
            using (var dest = OpenDest())
            {
                // 1. Load all legacy rows (full detail, one per class/subject/exam)
                var rows = (await src.QueryAsync<LegacyExamMaster>("SELECT * FROM SAS_ExamNam")).AsList();
                result.Total = rows.Count;
                Log($"Found {result.Total} rows in SAS_ExamNam");

                // 2. Source lookups (legacy id → legacy name)
                var legacyClassNameMap   = await LoadLegacyNameMap(src, "SAS_ClassNames", "ClassID",   "ClassNam");
                var legacySubjectNameMap = await LoadLegacyNameMap(src, "SAS_Subjects",   "SubJectID", "SubjectNam");
                var legacyGradeNameMap   = await LoadLegacyNameMap(src, "SAS_MarksGrade", "MrksGradId","GradName");

                // 3. Dest lookups (school-scoped)
                var acadYearMap        = await LoadAcadYearMap(dest);                          // academic_year → id
                var classMap           = await LoadClassMap(dest);                             // class_name → class_id
                var examTypeByNameYear = LoadExamTypeMap(dest, out var examTypeByName);  // name|yearId → id (+ name-only fallback)
                var subjByNameYear     = LoadSubjectMap(dest, out var subjByName);       // name|yearId → id (+ name-latest fallback)
                var gradeTypeMap       = await LoadGradeTypeMap(dest);                          // grade_name|subject_name → grade_type id

                Log($"Lookups — classes:{classMap.Count} examTypes:{examTypeByName.Count} subjects:{subjByName.Count} gradeTypes:{gradeTypeMap.Count}");

                // 4. Handle Truncate vs Skip
                var mode = Config.GetTableMode(Name);
                Log($"Mode: {mode}");

                if (mode == MigrationMode.Truncate && !Config.DryRun)
                {
                    var deleted = await dest.ExecuteAsync(
                        "DELETE FROM exam_master WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    Log($"Deleted {deleted} existing rows for school_id={Config.SchoolId}");
                }

                // For Skip mode: composite key exam_type_id|class_id|subject_id|academic_year_id|exam_category
                var existingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (mode == MigrationMode.Skip)
                {
                    var existing = await dest.QueryAsync<ExamMasterKey>(
                        @"SELECT ISNULL(exam_type_id,0)     AS exam_type_id,
                                 ISNULL(class_id,0)         AS class_id,
                                 ISNULL(subject_id,0)       AS subject_id,
                                 ISNULL(academic_year_id,0) AS academic_year_id,
                                 ISNULL(exam_category,'')   AS exam_category
                          FROM exam_master WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    foreach (var e in existing)
                        existingKeys.Add(MakeKey(e.exam_type_id, e.class_id, e.subject_id, e.academic_year_id, e.exam_category));
                }

                // Resolve fallback academic_year_id (config override or latest in dest)
                int defaultYearId = await ResolveDefaultAcademicYearIdAsync(dest);
                Log($"Fallback academic_year_id = {defaultYearId}");

                // 5. Migrate row by row
                int processed = 0;
                foreach (var row in rows)
                {
                    processed++;
                    LogProgress(processed, result.Total);

                    // --- academic_year_id (fallback to default on miss) ---
                    int academicYearId = defaultYearId;
                    var acdYear = row.AcdYear?.Trim();
                    if (!string.IsNullOrWhiteSpace(acdYear))
                    {
                        if (acadYearMap.TryGetValue(acdYear, out var ayId))
                            academicYearId = ayId;
                        else
                            Log($"Warning: academic_year '{acdYear}' not found — using fallback academic_year_id={defaultYearId}");
                    }

                    // --- exam_type_id: exam_type_name = ExamNam, scoped to year (fallback name-only) ---
                    int? examTypeId = null;
                    var examName = row.ExamNam?.Trim();
                    if (!string.IsNullOrWhiteSpace(examName))
                    {
                        if (examTypeByNameYear.TryGetValue($"{examName}|{academicYearId}", out var etId)
                            || examTypeByName.TryGetValue(examName, out etId))
                            examTypeId = etId;
                        else
                            AddError(result, processed, $"exam_type '{examName}' not found in exam_types — exam_type_id set to NULL");
                    }

                    // --- class_id: two-step (ClasId → ClassNam → classes.class_id) ---
                    int? classId = null;
                    var className = ResolveLegacyName(legacyClassNameMap, row.ClasId);
                    if (className != null)
                    {
                        if (classMap.TryGetValue(className, out var cId))
                            classId = cId;
                        else
                            AddError(result, processed, $"class '{className}' (ClasId='{row.ClasId?.Trim()}') not found in classes — class_id set to NULL");
                    }
                    else if (!string.IsNullOrWhiteSpace(row.ClasId?.Trim()))
                        AddError(result, processed, $"ClasId='{row.ClasId?.Trim()}' not found in SAS_ClassNames — class_id set to NULL");

                    // --- subject_id: two-step (SubjectID → SubjectNam → subjects.subject_id), year-scoped ---
                    int? subjectId = null;
                    var subjectName = ResolveLegacyName(legacySubjectNameMap, row.SubjectID);
                    if (subjectName != null)
                    {
                        if (subjByNameYear.TryGetValue($"{subjectName}|{academicYearId}", out var sId)
                            || subjByName.TryGetValue(subjectName, out sId))
                            subjectId = sId;
                        else
                            AddError(result, processed, $"subject '{subjectName}' (SubjectID='{row.SubjectID?.Trim()}') not found in subjects — subject_id set to NULL");
                    }
                    else if (!string.IsNullOrWhiteSpace(row.SubjectID?.Trim()))
                        AddError(result, processed, $"SubjectID='{row.SubjectID?.Trim()}' not found in SAS_Subjects — subject_id set to NULL");

                    // --- grade_type_id: grade_name (from GradeTyp→SAS_MarksGrade) + subject_name ---
                    int? gradeTypeId = null;
                    var gradeName = ResolveLegacyName(legacyGradeNameMap, row.GradeTyp);
                    if (gradeName != null && subjectName != null)
                    {
                        if (gradeTypeMap.TryGetValue($"{gradeName}|{subjectName}", out var gtId))
                            gradeTypeId = gtId;
                        else
                            AddError(result, processed, $"grade_type (grade='{gradeName}', subject='{subjectName}') not found in grade_types — grade_type_id set to NULL");
                    }
                    else if (!string.IsNullOrWhiteSpace(row.GradeTyp?.Trim()) && gradeName == null)
                        AddError(result, processed, $"GradeTyp='{row.GradeTyp?.Trim()}' not found in SAS_MarksGrade — grade_type_id set to NULL");

                    // --- Skip check ---
                    if (mode == MigrationMode.Skip)
                    {
                        var key = MakeKey(examTypeId ?? 0, classId ?? 0, subjectId ?? 0, academicYearId, row.ExamCatgry?.Trim() ?? "");
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
                                INSERT INTO exam_master
                                    (exam_type_id, class_id, exam_total_marks, exam_min_marks,
                                     subject_min_marks, sub_max_marks, exam_remarks, academic_year_id,
                                     subject_id, exam_status, created_by, created_date, school_id,
                                     exam_category, exam_date, grade_type_id)
                                VALUES
                                    (@ExamTypeId, @ClassId, @ExamTotalMarks, @ExamMinMarks,
                                     @SubjectMinMarks, @SubMaxMarks, @ExamRemarks, @AcademicYearId,
                                     @SubjectId, @ExamStatus, @CreatedBy, @CreatedDate, @SchoolId,
                                     @ExamCategory, @ExamDate, @GradeTypeId)",
                                new
                                {
                                    ExamTypeId      = examTypeId,
                                    ClassId         = classId,
                                    ExamTotalMarks  = row.ExamTotalMarks,
                                    ExamMinMarks    = row.ExamMinMarks,
                                    SubjectMinMarks = row.SubMinMrks,
                                    SubMaxMarks     = row.SubMaxMrks,
                                    ExamRemarks     = row.ExamRemarks?.Trim(),
                                    AcademicYearId  = academicYearId,
                                    SubjectId       = subjectId,
                                    ExamStatus      = MapStatus(row.ExamStatus),
                                    CreatedBy       = "migration",
                                    CreatedDate     = DateTime.Now,
                                    SchoolId        = Config.SchoolId,
                                    ExamCategory    = row.ExamCatgry?.Trim(),
                                    ExamDate        = row.ExamDatTim,
                                    GradeTypeId     = gradeTypeId
                                });
                        }

                        result.Migrated++;
                    }
                    catch (Exception ex)
                    {
                        AddError(result, processed, $"INSERT failed for exam='{examName}' (ExamID='{row.ExamID?.Trim()}'): {ex.Message}");
                    }
                }

                LogProgressDone();
            }

            return result;
        }

        // ---------------------------------------------------------------
        // Lookup loaders
        // ---------------------------------------------------------------

        // Generic legacy "id → name" loader (first wins on dup)
        private async Task<Dictionary<string, string>> LoadLegacyNameMap(SqlConnection src, string table, string idCol, string nameCol)
        {
            var rows = await src.QueryAsync($"SELECT {idCol} AS Id, {nameCol} AS Nam FROM {table}");
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
            {
                var key = ((string)r.Id)?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(key) && !map.ContainsKey(key))
                    map[key] = ((string)r.Nam)?.Trim() ?? "";
            }
            return map;
        }

        private static string ResolveLegacyName(Dictionary<string, string> map, string legacyId)
        {
            var key = legacyId?.Trim();
            if (string.IsNullOrWhiteSpace(key)) return null;
            return map.TryGetValue(key, out var name) && !string.IsNullOrWhiteSpace(name) ? name : null;
        }

        private async Task<Dictionary<string, int>> LoadAcadYearMap(SqlConnection dest)
        {
            var rows = await dest.QueryAsync<AcadYearRow>(
                "SELECT academic_year_id, academic_year FROM academic_years WHERE school_id = @SchoolId",
                new { Config.SchoolId });
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
            {
                var key = r.academic_year?.Trim() ?? "";
                if (!map.ContainsKey(key)) map[key] = r.academic_year_id;
                else Log($"Warning: duplicate academic_year '{key}' in dest — keeping first (id={map[key]})");
            }
            return map;
        }

        private async Task<Dictionary<string, int>> LoadClassMap(SqlConnection dest)
        {
            var rows = await dest.QueryAsync<ClassRow>(
                "SELECT class_id, class_name FROM classes WHERE school_id = @SchoolId AND status = 'Active'",
                new { Config.SchoolId });
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
            {
                var key = r.class_name?.Trim() ?? "";
                if (!map.ContainsKey(key)) map[key] = r.class_id;
                else Log($"Warning: duplicate class_name '{key}' (Active) in dest — keeping first (id={map[key]})");
            }
            return map;
        }

        // exam_types are per-year — build name|yearId map plus a name-only fallback (latest year)
        private Dictionary<string, int> LoadExamTypeMap(SqlConnection dest, out Dictionary<string, int> byName)
        {
            var rows = dest.Query<ExamTypeRow>(
                "SELECT exam_type_id, exam_type_name, academic_year_id FROM exam_types WHERE school_id = @SchoolId ORDER BY academic_year_id DESC",
                new { Config.SchoolId }).AsList();
            var byNameYear = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            byName         = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
            {
                var name = r.exam_type_name?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(name)) continue;
                var key = $"{name}|{r.academic_year_id}";
                if (!byNameYear.ContainsKey(key)) byNameYear[key] = r.exam_type_id;
                if (!byName.ContainsKey(name))    byName[name]    = r.exam_type_id;   // first = latest year (DESC)
            }
            return byNameYear;
        }

        // subjects are per-year — build name|yearId map plus a name-only fallback (latest year)
        private Dictionary<string, int> LoadSubjectMap(SqlConnection dest, out Dictionary<string, int> byName)
        {
            var rows = dest.Query<SubjectRow>(
                "SELECT subject_id, subject_name, academic_year_id FROM subjects WHERE school_id = @SchoolId ORDER BY academic_year_id DESC",
                new { Config.SchoolId }).AsList();
            var byNameYear = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            byName         = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
            {
                var name = r.subject_name?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(name)) continue;
                var key = $"{name}|{r.academic_year_id}";
                if (!byNameYear.ContainsKey(key)) byNameYear[key] = r.subject_id;
                if (!byName.ContainsKey(name))    byName[name]    = r.subject_id;     // first = latest year (DESC)
            }
            return byNameYear;
        }

        // grade_types matched by grade_name|subject_name (grade_types has no class_id;
        // subject resolved by name via JOIN to decouple from per-year subject_id)
        private async Task<Dictionary<string, int>> LoadGradeTypeMap(SqlConnection dest)
        {
            var rows = await dest.QueryAsync<GradeTypeRow>(
                @"SELECT gt.id, gt.grade_name, s.subject_name
                  FROM grade_types gt
                  LEFT JOIN subjects s ON gt.subject_id = s.subject_id
                  WHERE gt.school_id = @SchoolId",
                new { Config.SchoolId });
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
            {
                var key = $"{r.grade_name?.Trim()}|{r.subject_name?.Trim()}";
                if (!map.ContainsKey(key)) map[key] = r.id;
                else Log($"Warning: duplicate grade_type for '{key}' in dest — keeping first (id={map[key]})");
            }
            return map;
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private static string MakeKey(int examTypeId, int classId, int subjectId, int academicYearId, string examCategory)
        {
            return $"{examTypeId}|{classId}|{subjectId}|{academicYearId}|{examCategory?.Trim() ?? ""}";
        }

        private static void AddError(MigrationResult result, int rowNo, string reason)
        {
            result.Errors.Add(new MigrationError { RowIdentifier = $"row {rowNo}", Reason = reason });
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

        private class AcadYearRow  { public int academic_year_id { get; set; } public string academic_year { get; set; } }
        private class ClassRow     { public int class_id { get; set; } public string class_name { get; set; } }
        private class ExamTypeRow  { public int exam_type_id { get; set; } public string exam_type_name { get; set; } public int? academic_year_id { get; set; } }
        private class SubjectRow   { public int subject_id { get; set; } public string subject_name { get; set; } public int? academic_year_id { get; set; } }
        private class GradeTypeRow { public int id { get; set; } public string grade_name { get; set; } public string subject_name { get; set; } }

        private class ExamMasterKey
        {
            public int    exam_type_id     { get; set; }
            public int    class_id         { get; set; }
            public int    subject_id       { get; set; }
            public int    academic_year_id { get; set; }
            public string exam_category    { get; set; }
        }
    }
}
