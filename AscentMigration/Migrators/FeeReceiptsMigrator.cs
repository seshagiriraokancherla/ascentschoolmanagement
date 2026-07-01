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
    public class FeeReceiptsMigrator : BaseMigrator
    {
        public override string Name => "fee_receipts";

        public FeeReceiptsMigrator(MigrationConfig config) : base(config) { }

        public override async Task<MigrationResult> RunAsync()
        {
            var result = new MigrationResult { Entity = Name };

            using (var src  = OpenSource())
            using (var dest = OpenDest())
            {
                // 1. Resolve academic year scope from App.config:
                //    "latest" (default/empty) → SELECT TOP 1 AcademicYear FROM SAS_AcdYear ORDER BY AcademicYear DESC
                //    "all"                    → no year filter; migrate all records
                //    "<year string>"          → migrate only that year (e.g. "2024-25")
                var setting = Config.GetOptionalSetting("fee_receipts.academicYear")?.ToLower() ?? "latest";
                bool migrateAll    = setting == "all";
                string targetAcdYear = null;

                if (!migrateAll)
                {
                    if (setting == "latest" || string.IsNullOrWhiteSpace(setting))
                    {
                        targetAcdYear = await src.QueryFirstOrDefaultAsync<string>(
                            "SELECT TOP 1 AcademicYear FROM SAS_AcdYear ORDER BY AcademicYear DESC");
                        Log($"fee_receipts.academicYear=latest — resolved to: '{targetAcdYear}'");
                    }
                    else
                    {
                        targetAcdYear = setting;
                        Log($"fee_receipts.academicYear set to specific year: '{targetAcdYear}'");
                    }

                    if (string.IsNullOrWhiteSpace(targetAcdYear))
                    {
                        Log("Could not resolve target academic year from SAS_AcdYear — nothing to migrate.");
                        return result;
                    }
                }
                else
                {
                    Log("fee_receipts.academicYear=all — migrating all academic years");
                }

                // 2. Load rows (filtered by year or all), grouped by FeeReciptID
                List<LegacyFeeReceipt> allRows;
                if (migrateAll)
                {
                    allRows = (await src.QueryAsync<LegacyFeeReceipt>(
                        "SELECT * FROM SAS_FeeReceipts ORDER BY FeeReciptID, Id_new")).AsList();
                    Log($"Found {allRows.Count} total rows in SAS_FeeReceipts (all years)");
                }
                else
                {
                    allRows = (await src.QueryAsync<LegacyFeeReceipt>(
                        "SELECT * FROM SAS_FeeReceipts WHERE LTRIM(RTRIM(AcdYear)) = @targetAcdYear ORDER BY FeeReciptID, Id_new",
                        new { targetAcdYear })).AsList();
                    Log($"Found {allRows.Count} rows in SAS_FeeReceipts for academic year '{targetAcdYear}'");
                }

                var groups = new Dictionary<string, List<LegacyFeeReceipt>>(StringComparer.OrdinalIgnoreCase);
                foreach (var row in allRows)
                {
                    var key = row.FeeReciptID?.Trim() ?? "";
                    if (string.IsNullOrWhiteSpace(key)) continue;
                    if (!groups.ContainsKey(key)) groups[key] = new List<LegacyFeeReceipt>();
                    groups[key].Add(row);
                }

                result.Total = groups.Count;
                Log($"Distinct receipt groups: {result.Total}");

                // 2. Pre-load all lookup dictionaries
                var acadYearMap     = await LoadAcadYearMap(dest);      // academic_year            → academic_year_id
                var studentMap      = await LoadStudentMap(dest);        // "admission_no|acad_yr_id" → StudentMeta
                var paymentModeMap  = await LoadPaymentModeMap(dest);    // mode_name                → payment_mode_id
                var feeTypeMap      = await LoadFeeTypeMap(dest);        // fee_type_name            → fee_type_id
                var termMap         = await LoadTermMap(dest);           // "term_name|acad_yr_id"   → term_id

                Log($"Lookups loaded — years:{acadYearMap.Count} students:{studentMap.Count} payModes:{paymentModeMap.Count} feeTypes:{feeTypeMap.Count} terms:{termMap.Count}");

                // 3. Handle Truncate vs Skip
                var mode = Config.GetTableMode(Name);
                Log($"Mode: {mode}");

                if (mode == MigrationMode.Truncate && !Config.DryRun)
                {
                    // Items must be deleted before receipts (FK)
                    var deletedItems = await dest.ExecuteAsync(
                        "DELETE fri FROM fee_receipt_items fri JOIN fee_receipts fr ON fr.receipt_id = fri.receipt_id WHERE fr.school_id = @SchoolId",
                        new { Config.SchoolId });
                    var deletedReceipts = await dest.ExecuteAsync(
                        "DELETE FROM fee_receipts WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    Log($"Deleted {deletedItems} items and {deletedReceipts} receipts for school_id={Config.SchoolId}");
                }

                // For Skip mode: load existing receipt_no values
                var existingReceiptNos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (mode == MigrationMode.Skip)
                {
                    var nos = await dest.QueryAsync<string>(
                        "SELECT receipt_no FROM fee_receipts WHERE school_id = @SchoolId AND receipt_no IS NOT NULL",
                        new { Config.SchoolId });
                    foreach (var n in nos) existingReceiptNos.Add(n);
                }

                // 4. Migrate one receipt group at a time
                int processed = 0;
                foreach (var kvp in groups)
                {
                    processed++;
                    LogProgress(processed, result.Total);

                    var receiptNo = kvp.Key;
                    var items     = kvp.Value;
                    var first     = items[0];   // header fields taken from first row

                    if (mode == MigrationMode.Skip && existingReceiptNos.Contains(receiptNo))
                    {
                        result.Skipped++;
                        continue;
                    }

                    // --- academic_year_id ---
                    int? academicYearId = null;
                    var acdYear = first.AcdYear?.Trim();
                    if (!string.IsNullOrWhiteSpace(acdYear))
                    {
                        if (acadYearMap.TryGetValue(acdYear, out var ayId))
                            academicYearId = ayId;
                        else
                            Log($"Warning: receipt '{receiptNo}' — academic_year '{acdYear}' not found — academic_year_id set to NULL");
                    }

                    // --- student_id + student_unique_id ---
                    long?  studentId       = null;
                    int?   studentUniqueId = null;
                    var    admNo           = first.StuAdmnNo?.Trim();
                    if (!string.IsNullOrWhiteSpace(admNo) && academicYearId.HasValue)
                    {
                        var stuKey = $"{admNo}|{academicYearId}";
                        if (studentMap.TryGetValue(stuKey, out var meta))
                        {
                            studentId       = meta.StudentId;
                            studentUniqueId = meta.StudentUniqueId;
                        }
                        else
                            Log($"Warning: receipt '{receiptNo}' — student admission_no='{admNo}' academic_year_id={academicYearId} not found — student_id set to NULL");
                    }

                    // Legacy RefNo carries the student's unique id (SAS_StudentMaster.StuUnqID) — authoritative.
                    // Use it for student_unique_id even when the admission_no+year lookup misses (e.g. that
                    // year's student row wasn't migrated), so cross-year fee linkage stays intact.
                    if (!string.IsNullOrWhiteSpace(first.RefNo) && int.TryParse(first.RefNo.Trim(), out var refUnique))
                        studentUniqueId = refUnique;

                    // --- payment_mode_id ---
                    int? paymentModeId = null;
                    var pymntTyp = first.PymntTyp?.Trim();
                    if (!string.IsNullOrWhiteSpace(pymntTyp))
                    {
                        if (paymentModeMap.TryGetValue(pymntTyp, out var pmId))
                            paymentModeId = pmId;
                        else
                            Log($"Warning: receipt '{receiptNo}' — payment mode '{pymntTyp}' not found — payment_mode_id set to NULL");
                    }

                    // --- total_amount: sum all line item amounts ---
                    var totalAmount = items.Sum(i => i.FeeAmt ?? 0);

                    // --- status ---
                    var status = MapStatus(first.FeeReceiptStat);

                    try
                    {
                        if (!Config.DryRun)
                        {
                            using (var tx = dest.BeginTransaction())
                            {
                                try
                                {
                                    // Insert fee_receipts header
                                    var receiptId = await dest.QuerySingleAsync<int>(@"
                                        INSERT INTO fee_receipts
                                            (receipt_no, student_id, student_unique_id, academic_year_id,
                                             payment_date, total_amount, payment_mode_id,
                                             cheque_no, status, school_id, created_by, created_at)
                                        VALUES
                                            (@ReceiptNo, @StudentId, @StudentUniqueId, @AcademicYearId,
                                             @PaymentDate, @TotalAmount, @PaymentModeId,
                                             @ChequeNo, @Status, @SchoolId, @CreatedBy, @CreatedAt);
                                        SELECT CAST(SCOPE_IDENTITY() AS INT)",
                                        new
                                        {
                                            ReceiptNo       = receiptNo,
                                            StudentId       = studentId,
                                            StudentUniqueId = studentUniqueId,
                                            AcademicYearId  = academicYearId,
                                            PaymentDate     = first.FeeDat,
                                            TotalAmount     = totalAmount,
                                            PaymentModeId   = paymentModeId,
                                            ChequeNo        = (string)null,  // legacy RefNo is the student unique id, not a cheque no
                                            Status          = status,
                                            SchoolId        = Config.SchoolId,
                                            CreatedBy       = "migration",
                                            CreatedAt       = DateTime.Now
                                        }, tx);

                                    // Insert fee_receipt_items — one row per line item
                                    foreach (var item in items)
                                    {
                                        // --- fee_type_id ---
                                        int? feeTypeId = null;
                                        var feeTyp = item.FeeTyp?.Trim();
                                        if (!string.IsNullOrWhiteSpace(feeTyp))
                                        {
                                            if (!feeTypeMap.TryGetValue(feeTyp, out var ftId))
                                                Log($"Warning: receipt '{receiptNo}' item — fee_type '{feeTyp}' not found — fee_type_id set to NULL");
                                            else
                                                feeTypeId = ftId;
                                        }

                                        // --- term_id: PaymentTyp + academic_year_id ---
                                        int? termId = null;
                                        var paymentTyp = item.PaymentTyp?.Trim();
                                        if (!string.IsNullOrWhiteSpace(paymentTyp) && academicYearId.HasValue)
                                        {
                                            var termKey = $"{paymentTyp}|{academicYearId}";
                                            if (!termMap.TryGetValue(termKey, out var tId))
                                                Log($"Warning: receipt '{receiptNo}' item — term '{paymentTyp}' (academic_year_id={academicYearId}) not found — term_id set to NULL");
                                            else
                                                termId = tId;
                                        }

                                        await dest.ExecuteAsync(@"
                                            INSERT INTO fee_receipt_items
                                                (receipt_id, fee_type_id, term_id, fee_period_id,
                                                 bus_route_id, hostel_id,
                                                 amount, concession_amount, net_amount, school_id)
                                            VALUES
                                                (@ReceiptId, @FeeTypeId, @TermId, NULL,
                                                 NULL, NULL,
                                                 @Amount, 0, @Amount, @SchoolId)",
                                            new
                                            {
                                                ReceiptId  = receiptId,
                                                FeeTypeId  = feeTypeId,
                                                TermId     = termId,
                                                Amount     = item.FeeAmt ?? 0,
                                                SchoolId   = Config.SchoolId
                                            }, tx);
                                    }

                                    tx.Commit();
                                    result.Migrated++;
                                }
                                catch
                                {
                                    tx.Rollback();
                                    throw;
                                }
                            }
                        }
                        else
                        {
                            result.Migrated++;
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(new MigrationError
                        {
                            RowIdentifier = $"receipt_no={receiptNo}",
                            Reason        = ex.Message
                        });
                    }
                }

                LogProgressDone();
            }

            return result;
        }

        // ---------------------------------------------------------------
        // Lookup loaders
        // ---------------------------------------------------------------

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
            }
            return map;
        }

        // Composite key: "admission_no|academic_year_id" → StudentMeta
        private async Task<Dictionary<string, StudentMeta>> LoadStudentMap(SqlConnection dest)
        {
            var rows = await dest.QueryAsync<StudentRow>(
                @"SELECT student_id, student_unique_id, admission_no, academic_year_id
                  FROM students
                  WHERE school_id = @SchoolId AND admission_no IS NOT NULL",
                new { Config.SchoolId });
            var map = new Dictionary<string, StudentMeta>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
            {
                var key = $"{r.admission_no?.Trim()}|{r.academic_year_id}";
                if (!map.ContainsKey(key))
                    map[key] = new StudentMeta { StudentId = r.student_id, StudentUniqueId = r.student_unique_id };
            }
            return map;
        }

        private async Task<Dictionary<string, int>> LoadPaymentModeMap(SqlConnection dest)
        {
            var rows = await dest.QueryAsync<PaymentModeRow>(
                "SELECT payment_mode_id, mode_name FROM payment_modes WHERE school_id = @SchoolId",
                new { Config.SchoolId });
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
            {
                var key = r.mode_name?.Trim() ?? "";
                if (!map.ContainsKey(key)) map[key] = r.payment_mode_id;
            }
            return map;
        }

        private async Task<Dictionary<string, int>> LoadFeeTypeMap(SqlConnection dest)
        {
            var rows = await dest.QueryAsync<FeeTypeRow>(
                "SELECT fee_type_id, fee_type_name FROM fee_types WHERE school_id = @SchoolId",
                new { Config.SchoolId });
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
            {
                var key = r.fee_type_name?.Trim() ?? "";
                if (!map.ContainsKey(key)) map[key] = r.fee_type_id;
                else Log($"Warning: duplicate fee_type_name '{key}' — keeping first (id={map[key]})");
            }
            return map;
        }

        // Composite key: "term_name|academic_year_id"
        private async Task<Dictionary<string, int>> LoadTermMap(SqlConnection dest)
        {
            var rows = await dest.QueryAsync<TermRow>(
                "SELECT term_id, term_name, academic_year_id FROM terms WHERE school_id = @SchoolId",
                new { Config.SchoolId });
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
            {
                var key = $"{r.term_name?.Trim()}|{r.academic_year_id}";
                if (!map.ContainsKey(key)) map[key] = r.term_id;
            }
            return map;
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

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
        // Private POCOs
        // ---------------------------------------------------------------

        private class AcadYearRow   { public int    academic_year_id { get; set; } public string academic_year  { get; set; } }
        private class StudentRow    { public long   student_id       { get; set; } public int?   student_unique_id { get; set; } public string admission_no { get; set; } public int? academic_year_id { get; set; } }
        private class PaymentModeRow{ public int    payment_mode_id  { get; set; } public string mode_name      { get; set; } }
        private class FeeTypeRow    { public int    fee_type_id      { get; set; } public string fee_type_name  { get; set; } }
        private class TermRow       { public int    term_id          { get; set; } public string term_name      { get; set; } public int?   academic_year_id { get; set; } }

        private class StudentMeta   { public long StudentId { get; set; } public int? StudentUniqueId { get; set; } }
    }
}
