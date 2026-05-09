using AscentSchools.Core.DTOs.School.Fee;
using AscentSchools.Core.DTOs.School.Students;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AscentSchools.Data.Repositories.School
{
    public class FeeRepository
    {
        private readonly IConnectionFactory _db;
        public FeeRepository(IConnectionFactory db) { _db = db; }

        // ── Fee Structure ─────────────────────────────────────────────────

        public IEnumerable<FeeStructureDto> GetFeeStructure(
            string tenantDbName, int schoolId, int classId, int feeCategoryId, int academicYearId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<FeeStructureDto>(
                    @"SELECT fs.fee_structure_id FeeStructureId,
                             fs.fee_type_id FeeTypeId, ft.fee_type_name FeeTypeName,
                             ft.sequence_no SequenceNo,
                             fs.term_id TermId, t.term_name TermName, t.order_no OrderNo,
                             fs.amount Amount
                      FROM fee_structures fs
                      INNER JOIN fee_types ft ON ft.fee_type_id = fs.fee_type_id
                      LEFT  JOIN terms t      ON t.term_id      = fs.term_id
                      WHERE fs.class_id        = @classId
                        AND fs.fee_category_id = @feeCategoryId
                        AND fs.academic_year_id= @academicYearId
                        AND fs.school_id       = @schoolId
                        AND ISNULL(fs.status, 'Active') <> 'Inactive'
                      ORDER BY ISNULL(ft.sequence_no, 9999), ft.fee_type_name,
                               ISNULL(t.order_no, 9999), t.term_name",
                    new { schoolId, classId, feeCategoryId, academicYearId });
        }

        /// <summary>
        /// Replaces all fee structure entries for a class/category/year/school atomically.
        /// </summary>
        public void SaveFeeStructure(string tenantDbName, int schoolId, SaveFeeStructureRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        conn.Execute(
                            @"DELETE FROM fee_structures
                              WHERE class_id = @ClassId AND fee_category_id = @FeeCategoryId
                                AND academic_year_id = @AcademicYearId AND school_id = @schoolId",
                            new { r.ClassId, r.FeeCategoryId, r.AcademicYearId, schoolId }, tx);

                        if (r.Items != null && r.Items.Count > 0)
                            conn.Execute(
                                @"INSERT INTO fee_structures
                                    (class_id, fee_category_id, academic_year_id,
                                     fee_type_id, term_id, amount, status, school_id)
                                  VALUES
                                    (@ClassId, @FeeCategoryId, @AcademicYearId,
                                     @FeeTypeId, @TermId, @Amount, 'Active', @schoolId)",
                                r.Items.Select(item => new
                                {
                                    r.ClassId, r.FeeCategoryId, r.AcademicYearId,
                                    item.FeeTypeId, item.TermId, item.Amount, schoolId
                                }), tx);

                        tx.Commit();
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        // ── Student Fee Summary ───────────────────────────────────────────

        /// <summary>
        /// Loads fee line items from the fee_structures for the student's class/category/year,
        /// then calculates how much has been paid and what remains outstanding.
        /// </summary>
        public StudentFeeSummaryDto GetStudentFeeSummary(
            string tenantDbName, int schoolId, long studentId, int academicYearId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                // Load student header
                var student = conn.QueryFirstOrDefault<StudentFeeHeader>(
                    @"SELECT s.student_id    StudentId,
                             s.student_name  StudentName,
                             s.admission_no  AdmissionNo,
                             s.class_id      ClassId,
                             c.class_name    ClassName,
                             s.fee_category_id   FeeCategoryId,
                             fc.category_name    CategoryName,
                             s.academic_year_id  AcademicYearId,
                             ay.academic_year    AcademicYear
                      FROM students s
                      LEFT JOIN classes         c  ON c.class_id         = s.class_id
                      LEFT JOIN fee_categories  fc ON fc.fee_category_id = s.fee_category_id
                      LEFT JOIN academic_years  ay ON ay.academic_year_id = s.academic_year_id
                      WHERE s.student_id = @studentId AND s.school_id = @schoolId",
                    new { studentId, schoolId });

                if (student == null) return null;

                int?   classId       = student.ClassId;
                int?   feeCatId      = student.FeeCategoryId;
                int    effYearId     = academicYearId > 0 ? academicYearId
                                      : student.AcademicYearId ?? 0;

                // Load line items with paid amounts
                var lineItems = conn.Query<FeeLineItemDto>(
                    @"SELECT ft.fee_type_id FeeTypeId, ft.fee_type_name FeeTypeName,
                             ft.sequence_no SequenceNo,
                             t.term_id TermId, t.term_name TermName, t.order_no OrderNo,
                             fs.amount StructureAmount,
                             ISNULL((
                                 SELECT SUM(ri.net_amount)
                                 FROM fee_receipt_items ri
                                 INNER JOIN fee_receipts r ON r.receipt_id = ri.receipt_id
                                 WHERE ri.fee_type_id         = fs.fee_type_id
                                   AND ISNULL(ri.term_id, 0) = ISNULL(fs.term_id, 0)
                                   AND r.student_id           = @studentId
                                   AND r.status               = 'Active'
                                   AND ri.school_id           = @schoolId
                             ), 0) PaidAmount
                      FROM fee_structures fs
                      INNER JOIN fee_types ft ON ft.fee_type_id = fs.fee_type_id
                      LEFT  JOIN terms     t  ON t.term_id      = fs.term_id
                      WHERE fs.class_id         = @classId
                        AND fs.fee_category_id  = @feeCatId
                        AND fs.academic_year_id = @effYearId
                        AND fs.school_id        = @schoolId
                        AND ISNULL(fs.status, 'Active') <> 'Inactive'
                      ORDER BY ISNULL(ft.sequence_no, 9999), ft.fee_type_name,
                               ISNULL(t.order_no, 9999), t.term_name",
                    new { studentId, schoolId, classId, feeCatId, effYearId }).ToList();

                foreach (var li in lineItems)
                    li.Outstanding = li.StructureAmount - li.PaidAmount;

                return new StudentFeeSummaryDto
                {
                    StudentId       = student.StudentId,
                    StudentName     = student.StudentName,
                    AdmissionNo     = student.AdmissionNo,
                    ClassId         = classId,
                    ClassName       = student.ClassName,
                    FeeCategoryId   = feeCatId,
                    FeeCategoryName = student.CategoryName,
                    AcademicYearId  = effYearId,
                    AcademicYear    = student.AcademicYear,
                    LineItems       = lineItems,
                };
            }
        }

        // ── Fee Collection ────────────────────────────────────────────────

        public int CollectFee(string tenantDbName, int schoolId, string createdBy, CollectFeeRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        decimal total = r.Items == null ? 0
                            : (decimal)r.Items.Sum(i => (double)(i.Amount - i.ConcessionAmount));

                        var receiptId = conn.QuerySingle<int>(
                            @"INSERT INTO fee_receipts
                                (receipt_no, student_id, academic_year_id,
                                 payment_date, total_amount, payment_mode_id,
                                 cheque_no, cheque_date, bank_name, remarks,
                                 status, school_id, created_by)
                              VALUES
                                ('TMP', @StudentId, @AcademicYearId,
                                 ISNULL(@PaymentDate, CAST(GETDATE() AS DATE)),
                                 @total, @PaymentModeId,
                                 @ChequeNo, @ChequeDate, @BankName, @Remarks,
                                 'Active', @schoolId, @createdBy);
                              SELECT CAST(SCOPE_IDENTITY() AS INT)",
                            new
                            {
                                r.StudentId, r.AcademicYearId, r.PaymentDate,
                                total, r.PaymentModeId, r.ChequeNo, r.ChequeDate,
                                r.BankName, r.Remarks, schoolId, createdBy
                            }, tx);

                        // Format receipt number after getting identity
                        var receiptNo = $"{DateTime.Now.Year}-{receiptId:D5}";
                        conn.Execute(
                            "UPDATE fee_receipts SET receipt_no = @receiptNo WHERE receipt_id = @receiptId",
                            new { receiptNo, receiptId }, tx);

                        if (r.Items != null && r.Items.Count > 0)
                            conn.Execute(
                                @"INSERT INTO fee_receipt_items
                                    (receipt_id, fee_type_id, term_id,
                                     amount, concession_amount, net_amount, school_id)
                                  VALUES
                                    (@receiptId, @FeeTypeId, @TermId,
                                     @Amount, @ConcessionAmount, @NetAmount, @schoolId)",
                                r.Items.Select(i => new
                                {
                                    receiptId,
                                    i.FeeTypeId,
                                    i.TermId,
                                    i.Amount,
                                    i.ConcessionAmount,
                                    NetAmount = i.Amount - i.ConcessionAmount,
                                    schoolId
                                }), tx);

                        tx.Commit();
                        return receiptId;
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        // ── Receipt Queries ───────────────────────────────────────────────

        public IEnumerable<FeeReceiptListDto> GetReceipts(
            string tenantDbName, int schoolId,
            string search, DateTime? dateFrom, DateTime? dateTo, string status)
        {
            var where = "r.school_id = @schoolId";
            if (!string.IsNullOrWhiteSpace(search))
                where += " AND (s.student_name LIKE @search OR s.admission_no LIKE @search OR r.receipt_no LIKE @search)";
            if (dateFrom.HasValue) where += " AND r.payment_date >= @dateFrom";
            if (dateTo.HasValue)   where += " AND r.payment_date <= @dateTo";
            if (!string.IsNullOrWhiteSpace(status)) where += " AND r.status = @status";

            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<FeeReceiptListDto>(
                    $@"SELECT r.receipt_id ReceiptId, r.receipt_no ReceiptNo,
                              r.student_id StudentId,
                              s.student_name StudentName, s.admission_no AdmissionNo,
                              c.class_name ClassName,
                              r.payment_date PaymentDate, r.total_amount TotalAmount,
                              pm.mode_name PaymentModeName,
                              r.status Status, r.school_id SchoolId
                       FROM fee_receipts r
                       INNER JOIN students      s  ON s.student_id      = r.student_id
                       LEFT  JOIN classes       c  ON c.class_id        = s.class_id
                       LEFT  JOIN payment_modes pm ON pm.payment_mode_id= r.payment_mode_id
                       WHERE {where}
                       ORDER BY r.receipt_id DESC",
                    new { schoolId, search = $"%{search}%", dateFrom, dateTo, status });
        }

        public FeeReceiptDto GetReceiptById(string tenantDbName, int schoolId, int receiptId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var receipt = conn.QueryFirstOrDefault<FeeReceiptDto>(
                    @"SELECT r.receipt_id ReceiptId, r.receipt_no ReceiptNo,
                             r.student_id StudentId,
                             s.student_name StudentName, s.admission_no AdmissionNo,
                             s.father_name FatherName,
                             c.class_name ClassName,
                             ay.academic_year AcademicYear,
                             r.payment_date PaymentDate, r.total_amount TotalAmount,
                             pm.mode_name PaymentModeName,
                             r.cheque_no ChequeNo, r.cheque_date ChequeDate,
                             r.bank_name BankName, r.status Status,
                             r.cancel_reason CancelReason,
                             r.remarks Remarks, r.created_by CreatedBy,
                             r.created_at CreatedAt, r.school_id SchoolId
                      FROM fee_receipts r
                      INNER JOIN students      s  ON s.student_id       = r.student_id
                      LEFT  JOIN classes       c  ON c.class_id         = s.class_id
                      LEFT  JOIN academic_years ay ON ay.academic_year_id = r.academic_year_id
                      LEFT  JOIN payment_modes pm ON pm.payment_mode_id = r.payment_mode_id
                      WHERE r.receipt_id = @receiptId AND r.school_id = @schoolId",
                    new { receiptId, schoolId });

                if (receipt == null) return null;

                receipt.Items = conn.Query<FeeReceiptItemDto>(
                    @"SELECT ri.item_id ItemId,
                             ISNULL(ft.fee_type_name, '') FeeTypeName,
                             ISNULL(t.term_name, '') TermName,
                             ri.amount Amount, ri.concession_amount ConcessionAmount,
                             ri.net_amount NetAmount
                      FROM fee_receipt_items ri
                      LEFT JOIN fee_types ft ON ft.fee_type_id = ri.fee_type_id
                      LEFT JOIN terms     t  ON t.term_id      = ri.term_id
                      WHERE ri.receipt_id = @receiptId",
                    new { receiptId }).ToList();

                return receipt;
            }
        }

        public void CancelReceipt(
            string tenantDbName, int schoolId, int receiptId, string cancelledBy, string reason)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE fee_receipts
                      SET status = 'Cancelled', cancelled_by = @cancelledBy,
                          cancelled_at = GETDATE(), cancel_reason = @reason
                      WHERE receipt_id = @receiptId AND school_id = @schoolId",
                    new { cancelledBy, reason, receiptId, schoolId });
        }

        // ── Bulk fee structure import ─────────────────────────────────────────

        public BulkImportResult BulkSaveFeeStructure(string tenantDbName, int schoolId, BulkFeeStructureImportRequest request)
        {
            var result = new BulkImportResult { Total = request.Rows.Count };

            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                // Build lookup maps once
                var years    = conn.Query<FeeBulkLookup>("SELECT academic_year_id AS Id, academic_year AS Name FROM academic_years WHERE school_id = @schoolId", new { schoolId });
                var classes  = conn.Query<FeeBulkLookup>("SELECT class_id AS Id, class_name AS Name FROM classes WHERE school_id = @schoolId", new { schoolId });
                var cats     = conn.Query<FeeBulkLookup>("SELECT fee_category_id AS Id, category_name AS Name FROM fee_categories WHERE school_id = @schoolId", new { schoolId });
                var feeTypes = conn.Query<FeeBulkLookup>("SELECT fee_type_id AS Id, fee_type_name AS Name FROM fee_types WHERE school_id = @schoolId", new { schoolId });
                var terms    = conn.Query<FeeBulkLookup>("SELECT term_id AS Id, term_name AS Name FROM terms WHERE school_id = @schoolId", new { schoolId });

                var yearMap     = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var classMap    = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var catMap      = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var feeTypeMap  = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var termMap     = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                foreach (var y in years)   yearMap[y.Name]    = y.Id;
                foreach (var c in classes)  classMap[c.Name]   = c.Id;
                foreach (var c in cats)     catMap[c.Name]     = c.Id;
                foreach (var f in feeTypes) feeTypeMap[f.Name] = f.Id;
                foreach (var t in terms)    termMap[t.Name]    = t.Id;

                foreach (var row in request.Rows)
                {
                    var identifier = $"{row.ClassName} / {row.FeeType} / {row.Term}";

                    // ── Required fields ───────────────────────────────────────
                    if (string.IsNullOrWhiteSpace(row.AcademicYear) || string.IsNullOrWhiteSpace(row.ClassName) ||
                        string.IsNullOrWhiteSpace(row.FeeCategory)  || string.IsNullOrWhiteSpace(row.FeeType) ||
                        string.IsNullOrWhiteSpace(row.Term)         || string.IsNullOrWhiteSpace(row.Amount))
                    { AddFeeError(result, row, identifier, "All columns are required (AcademicYear, ClassName, FeeCategory, FeeType, Term, Amount)"); continue; }

                    // ── Resolve lookups ───────────────────────────────────────
                    if (!yearMap.TryGetValue(row.AcademicYear.Trim(), out var yearId))
                    { AddFeeError(result, row, identifier, $"Academic year '{row.AcademicYear}' not found"); continue; }

                    if (!classMap.TryGetValue(row.ClassName.Trim(), out var classId))
                    { AddFeeError(result, row, identifier, $"Class '{row.ClassName}' not found"); continue; }

                    if (!catMap.TryGetValue(row.FeeCategory.Trim(), out var catId))
                    { AddFeeError(result, row, identifier, $"Fee category '{row.FeeCategory}' not found"); continue; }

                    if (!feeTypeMap.TryGetValue(row.FeeType.Trim(), out var feeTypeId))
                    { AddFeeError(result, row, identifier, $"Fee type '{row.FeeType}' not found"); continue; }

                    if (!termMap.TryGetValue(row.Term.Trim(), out var termId))
                    { AddFeeError(result, row, identifier, $"Term '{row.Term}' not found"); continue; }

                    if (!decimal.TryParse(row.Amount.Trim(), out var amount) || amount < 0)
                    { AddFeeError(result, row, identifier, $"Invalid amount '{row.Amount}'"); continue; }

                    // ── Upsert (delete existing row for same combo, then insert) ─
                    conn.Execute(@"
                        DELETE FROM fee_structures
                        WHERE class_id = @classId AND fee_category_id = @catId
                          AND academic_year_id = @yearId AND fee_type_id = @feeTypeId
                          AND term_id = @termId AND school_id = @schoolId",
                        new { classId, catId, yearId, feeTypeId, termId, schoolId });

                    conn.Execute(@"
                        INSERT INTO fee_structures
                            (class_id, fee_category_id, academic_year_id, fee_type_id, term_id, amount, status, school_id)
                        VALUES
                            (@classId, @catId, @yearId, @feeTypeId, @termId, @amount, 'Active', @schoolId)",
                        new { classId, catId, yearId, feeTypeId, termId, amount, schoolId });

                    result.Imported++;
                }
            }

            result.Failed = result.Total - result.Imported;
            return result;
        }

        private static void AddFeeError(BulkImportResult result, FeeStructureBulkRow row, string identifier, string reason)
        {
            result.Errors.Add(new BulkRowError { Row = row.RowNumber, Identifier = identifier, Reason = reason });
        }
    }

    /// <summary>
    /// Internal projection used by GetStudentFeeSummary to avoid dynamic.
    /// Not a public DTO — scoped to this repository only.
    /// </summary>
    internal class StudentFeeHeader
    {
        public long   StudentId       { get; set; }
        public string StudentName     { get; set; }
        public string AdmissionNo     { get; set; }
        public int?   ClassId         { get; set; }
        public string ClassName       { get; set; }
        public int?   FeeCategoryId   { get; set; }
        public string CategoryName    { get; set; }
        public int?   AcademicYearId  { get; set; }
        public string AcademicYear    { get; set; }
    }

    internal class FeeBulkLookup
    {
        public int    Id   { get; set; }
        public string Name { get; set; }
    }
}
