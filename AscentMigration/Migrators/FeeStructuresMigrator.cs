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
    public class FeeStructuresMigrator : BaseMigrator
    {
        public override string Name => "fee_structures";

        public FeeStructuresMigrator(MigrationConfig config) : base(config) { }

        public override async Task<MigrationResult> RunAsync()
        {
            var result = new MigrationResult { Entity = Name };

            using (var src  = OpenSource())
            using (var dest = OpenDest())
            {
                // 1. Load all legacy rows
                var rows = (await src.QueryAsync<LegacyFeeMaster>("SELECT * FROM SAS_FeeMaster")).AsList();
                result.Total = rows.Count;
                Log($"Found {result.Total} rows in SAS_FeeMaster");

                // 2. Pre-load all lookup dictionaries
                // Legacy: FeaCatgID → FeaCategoryNam (from source SAS_FeeCategory)
                var legacyCatIdMap = await LoadLegacyCategoryIdMap(src);
                // Dest lookups (school-scoped)
                var acadYearMap    = await LoadAcadYearMap(dest);
                var feeCategoryMap = await LoadFeeCategoryMap(dest);   // category_name → fee_category_id
                var classMap       = await LoadClassMap(dest);
                var feeTypeMap     = await LoadFeeTypeMap(dest);
                var termMap        = await LoadTermMap(dest);           // key: "termName|academicYearId"
                var feePeriodMap   = await LoadFeePeriodMap(dest);      // key: "periodLabel|academicYearId"

                Log($"Lookups loaded — legacyCats:{legacyCatIdMap.Count} years:{acadYearMap.Count} categories:{feeCategoryMap.Count} classes:{classMap.Count} feeTypes:{feeTypeMap.Count} terms:{termMap.Count} feePeriods:{feePeriodMap.Count}");

                // 3. Handle Truncate vs Skip
                var mode = Config.GetTableMode(Name);
                Log($"Mode: {mode}");

                if (mode == MigrationMode.Truncate && !Config.DryRun)
                {
                    var deleted = await dest.ExecuteAsync(
                        "DELETE FROM fee_structures WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    Log($"Deleted {deleted} existing rows for school_id={Config.SchoolId}");
                }

                // For Skip: load composite keys of existing rows
                var existingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (mode == MigrationMode.Skip)
                {
                    var existing = await dest.QueryAsync<FeeStructureKey>(
                        @"SELECT ISNULL(class_id,0)        AS class_id,
                                 ISNULL(fee_category_id,0) AS fee_category_id,
                                 ISNULL(fee_type_id,0)     AS fee_type_id,
                                 ISNULL(term_id,0)         AS term_id,
                                 ISNULL(fee_period_id,0)   AS fee_period_id,
                                 ISNULL(academic_year_id,0)AS academic_year_id,
                                 ISNULL(admission_type,'') AS admission_type
                          FROM fee_structures WHERE school_id = @SchoolId",
                        new { Config.SchoolId });

                    foreach (var e in existing)
                        existingKeys.Add(MakeKey(e.class_id, e.fee_category_id, e.fee_type_id,
                                                  e.term_id, e.fee_period_id, e.academic_year_id, e.admission_type));
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

                    // --- academic_year_id ---
                    int academicYearId = defaultYearId;
                    var acdYear = row.AcdYear?.Trim();
                    if (!string.IsNullOrWhiteSpace(acdYear))
                    {
                        if (acadYearMap.TryGetValue(acdYear, out var ayId))
                            academicYearId = ayId;
                        else
                            Log($"Warning: academic_year '{acdYear}' not found — using fallback academic_year_id={defaultYearId}");
                    }

                    // --- fee_category_id: two-step lookup ---
                    // Step 1: FeaCatgID (legacy ID) → FeaCategoryNam via SAS_FeeCategory
                    // Step 2: FeaCategoryNam → fee_category_id via dest fee_categories
                    int? feeCategoryId = null;
                    var feeCatId = row.FeeCategory?.Trim();
                    if (!string.IsNullOrWhiteSpace(feeCatId))
                    {
                        if (legacyCatIdMap.TryGetValue(feeCatId, out var catName))
                        {
                            if (feeCategoryMap.TryGetValue(catName, out var fcId))
                                feeCategoryId = fcId;
                            else
                                AddError(result, processed, $"fee_category name '{catName}' (from FeaCatgID='{feeCatId}') not found in dest fee_categories — fee_category_id set to NULL");
                        }
                        else
                            AddError(result, processed, $"FeaCatgID='{feeCatId}' not found in SAS_FeeCategory — fee_category_id set to NULL");
                    }

                    // --- class_id (error + NULL on miss) ---
                    int? classId = null;
                    var className = row.ClassNam?.Trim();
                    if (!string.IsNullOrWhiteSpace(className))
                    {
                        if (classMap.TryGetValue(className, out var cId))
                            classId = cId;
                        else
                            AddError(result, processed, $"class '{className}' not found in classes — class_id set to NULL");
                    }

                    // --- fee_type_id + fee_type_name (error + NULL on miss) ---
                    int?   feeTypeId   = null;
                    string feeTypeName = row.FeeTyp?.Trim();
                    if (!string.IsNullOrWhiteSpace(feeTypeName))
                    {
                        if (feeTypeMap.TryGetValue(feeTypeName, out var ft))
                        {
                            feeTypeId   = ft.FeeTypeId;
                            feeTypeName = ft.FeeTypeName;   // canonical name from dest
                        }
                        else
                            AddError(result, processed, $"fee_type '{feeTypeName}' not found in fee_types — fee_type_id set to NULL");
                    }

                    // --- term_id / fee_period_id: lookup by "{label}|academic_year_id" ---
                    // Try terms first; if not found, fall back to fee_periods (Monthly rows).
                    // A match in fee_periods sets fee_period_id and leaves term_id NULL.
                    int? termId      = null;
                    int? feePeriodId = null;
                    var nofPayments  = row.NofPayments?.Trim();
                    if (!string.IsNullOrWhiteSpace(nofPayments))
                    {
                        var key = $"{nofPayments}|{academicYearId}";
                        if (termMap.TryGetValue(key, out var tId))
                            termId = tId;
                        else if (feePeriodMap.TryGetValue(key, out var pId))
                            feePeriodId = pId;
                        else
                            AddError(result, processed, $"'{nofPayments}' (academic_year_id={academicYearId}) not found in terms or fee_periods — term_id & fee_period_id set to NULL");
                    }

                    // --- Skip check ---
                    if (mode == MigrationMode.Skip)
                    {
                        var key = MakeKey(classId, feeCategoryId, feeTypeId, termId, feePeriodId, academicYearId, row.AdmsnTyp?.Trim());
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
                                INSERT INTO fee_structures
                                    (fee_category_id, class_id, fee_type_id, fee_type_name,
                                     term_id, fee_period_id, payment_type, amount, description, status,
                                     academic_year_id, admission_type,
                                     school_id, created_by, created_at)
                                VALUES
                                    (@FeeCategoryId, @ClassId, @FeeTypeId, @FeeTypeName,
                                     @TermId, @FeePeriodId, @PaymentType, @Amount, @Description, @Status,
                                     @AcademicYearId, @AdmissionType,
                                     @SchoolId, @CreatedBy, @CreatedAt)",
                                new
                                {
                                    FeeCategoryId  = feeCategoryId,
                                    ClassId        = classId,
                                    FeeTypeId      = feeTypeId,
                                    FeeTypeName    = feeTypeName,
                                    TermId         = termId,
                                    FeePeriodId    = feePeriodId,
                                    PaymentType    = row.PymntTyp?.Trim(),
                                    Amount         = row.Amt,
                                    Description    = row.Descrpt?.Trim(),
                                    Status         = MapStatus(row.FeeMasterStat),
                                    AcademicYearId = academicYearId,
                                    AdmissionType  = row.AdmsnTyp?.Trim(),
                                    SchoolId       = Config.SchoolId,
                                    CreatedBy      = "migration",
                                    CreatedAt      = DateTime.Now
                                });
                        }

                        result.Migrated++;
                    }
                    catch (Exception ex)
                    {
                        AddError(result, processed, $"INSERT failed for class='{className}' feeType='{row.FeeTyp?.Trim()}': {ex.Message}");
                    }
                }

                LogProgressDone();
            }

            return result;
        }

        // ---------------------------------------------------------------
        // Lookup loaders — each returns a case-insensitive dictionary
        // ---------------------------------------------------------------

        // Loads FeaCatgID → FeaCategoryNam from legacy source DB
        private async Task<Dictionary<string, string>> LoadLegacyCategoryIdMap(SqlConnection src)
        {
            var rows = await src.QueryAsync<LegacyCatRow>(
                "SELECT FeeCatgID, FeeCategoryNam FROM SAS_FeeCategory");
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
            {
                var key = r.FeeCatgID?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(key) && !map.ContainsKey(key))
                    map[key] = r.FeeCategoryNam?.Trim() ?? "";
            }
            return map;
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

        private async Task<Dictionary<string, int>> LoadFeeCategoryMap(SqlConnection dest)
        {
            var rows = await dest.QueryAsync<FeeCategoryRow>(
                "SELECT fee_category_id, category_name FROM fee_categories WHERE school_id = @SchoolId",
                new { Config.SchoolId });
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
            {
                var key = r.category_name?.Trim() ?? "";
                if (!map.ContainsKey(key)) map[key] = r.fee_category_id;
                else Log($"Warning: duplicate category_name '{key}' in dest — keeping first (id={map[key]})");
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

        private async Task<Dictionary<string, FeeTypeEntry>> LoadFeeTypeMap(SqlConnection dest)
        {
            var rows = await dest.QueryAsync<FeeTypeRow>(
                "SELECT fee_type_id, fee_type_name FROM fee_types WHERE school_id = @SchoolId",
                new { Config.SchoolId });
            var map = new Dictionary<string, FeeTypeEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
            {
                var key = r.fee_type_name?.Trim() ?? "";
                if (!map.ContainsKey(key)) map[key] = new FeeTypeEntry { FeeTypeId = r.fee_type_id, FeeTypeName = r.fee_type_name?.Trim() };
                else Log($"Warning: duplicate fee_type_name '{key}' in dest — keeping first (id={map[key].FeeTypeId})");
            }
            return map;
        }

        private async Task<Dictionary<string, int>> LoadTermMap(SqlConnection dest)
        {
            // Composite key: "termName|academicYearId" to handle same name across years
            var rows = await dest.QueryAsync<TermRow>(
                "SELECT term_id, term_name, academic_year_id FROM terms WHERE school_id = @SchoolId",
                new { Config.SchoolId });
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
            {
                var key = $"{r.term_name?.Trim()}|{r.academic_year_id}";
                if (!map.ContainsKey(key))
                    map[key] = r.term_id;
            }
            return map;
        }

        private async Task<Dictionary<string, int>> LoadFeePeriodMap(SqlConnection dest)
        {
            // Composite key: "periodLabel|academicYearId" — mirrors LoadTermMap so a
            // fee_structures NofPayments that isn't a term can fall back to a Monthly period.
            var rows = await dest.QueryAsync<FeePeriodRow>(
                "SELECT fee_period_id, period_label, academic_year_id FROM fee_periods WHERE school_id = @SchoolId",
                new { Config.SchoolId });
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
            {
                var key = $"{r.period_label?.Trim()}|{r.academic_year_id}";
                if (!map.ContainsKey(key))
                    map[key] = r.fee_period_id;
            }
            return map;
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private static string MakeKey(int? classId, int? feeCategoryId, int? feeTypeId,
                                       int? termId, int? feePeriodId, int? academicYearId, string admissionType)
        {
            return $"{classId ?? 0}|{feeCategoryId ?? 0}|{feeTypeId ?? 0}|{termId ?? 0}|{feePeriodId ?? 0}|{academicYearId ?? 0}|{admissionType?.Trim() ?? ""}";
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
                default:  return "Inactive";
            }
        }

        // ---------------------------------------------------------------
        // Private POCOs for typed Dapper queries
        // ---------------------------------------------------------------

        private class LegacyCatRow   { public string FeeCatgID { get; set; } public string FeeCategoryNam { get; set; } }
        private class AcadYearRow    { public int academic_year_id { get; set; } public string academic_year  { get; set; } }
        private class FeeCategoryRow { public int fee_category_id  { get; set; } public string category_name  { get; set; } }
        private class ClassRow       { public int class_id         { get; set; } public string class_name      { get; set; } }
        private class FeeTypeRow     { public int fee_type_id      { get; set; } public string fee_type_name   { get; set; } }
        private class TermRow        { public int term_id          { get; set; } public string term_name       { get; set; } public int? academic_year_id { get; set; } }
        private class FeePeriodRow   { public int fee_period_id     { get; set; } public string period_label    { get; set; } public int? academic_year_id { get; set; } }

        private class FeeTypeEntry   { public int FeeTypeId { get; set; } public string FeeTypeName { get; set; } }

        private class FeeStructureKey
        {
            public int    class_id         { get; set; }
            public int    fee_category_id  { get; set; }
            public int    fee_type_id      { get; set; }
            public int    term_id          { get; set; }
            public int    fee_period_id    { get; set; }
            public int    academic_year_id { get; set; }
            public string admission_type   { get; set; }
        }
    }
}
