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
            string tenantDbName, int schoolId, int classId, int feeCategoryId, int academicYearId,
            string admissionType = null)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<FeeStructureDto>(
                    @"SELECT fs.fee_structure_id FeeStructureId,
                             fs.fee_type_id FeeTypeId, ft.fee_type_name FeeTypeName,
                             ft.sequence_no SequenceNo,
                             fs.term_id TermId, t.term_name TermName, t.order_no OrderNo,
                             fs.fee_period_id FeePeriodId, fp.period_label PeriodLabel,
                             fp.sequence_no PeriodSequenceNo,
                             fs.payment_type PaymentType, fs.admission_type AdmissionType,
                             fs.amount Amount
                      FROM fee_structures fs
                      INNER JOIN fee_types ft ON ft.fee_type_id = fs.fee_type_id
                      LEFT  JOIN terms t        ON t.term_id        = fs.term_id
                      LEFT  JOIN fee_periods fp ON fp.fee_period_id = fs.fee_period_id
                      WHERE fs.class_id         = @classId
                        AND fs.fee_category_id  = @feeCategoryId
                        AND fs.academic_year_id = @academicYearId
                        AND fs.school_id        = @schoolId
                        AND ISNULL(fs.status, 'Active') <> 'Inactive'
                        AND (@admissionType IS NULL
                             OR ISNULL(fs.admission_type, '') = @admissionType)
                      ORDER BY ISNULL(ft.sequence_no, 9999), ft.fee_type_name,
                               ISNULL(t.order_no, 9999), t.term_name,
                               ISNULL(fp.sequence_no, 9999), fp.period_label",
                    new { schoolId, classId, feeCategoryId, academicYearId, admissionType });
        }

        /// <summary>
        /// Replaces fee structure entries for a class/category/year/admissionType atomically.
        /// AdmissionType NULL = applies to all students (legacy / unscoped rows).
        /// </summary>
        public void SaveFeeStructure(string tenantDbName, int schoolId, SaveFeeStructureRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        // Delete only the rows that match this admission_type slice
                        conn.Execute(
                            @"DELETE FROM fee_structures
                              WHERE class_id = @ClassId AND fee_category_id = @FeeCategoryId
                                AND academic_year_id = @AcademicYearId AND school_id = @schoolId
                                AND ISNULL(admission_type, '') = ISNULL(@AdmissionType, '')",
                            new { r.ClassId, r.FeeCategoryId, r.AcademicYearId, r.AdmissionType, schoolId }, tx);

                        if (r.Items != null && r.Items.Count > 0)
                            conn.Execute(
                                @"INSERT INTO fee_structures
                                    (class_id, fee_category_id, academic_year_id,
                                     fee_type_id, term_id, fee_period_id,
                                     payment_type, admission_type,
                                     amount, status, school_id)
                                  VALUES
                                    (@ClassId, @FeeCategoryId, @AcademicYearId,
                                     @FeeTypeId, @TermId, @FeePeriodId,
                                     @PaymentType, @AdmissionType,
                                     @Amount, 'Active', @schoolId)",
                                r.Items.Select(item => new
                                {
                                    r.ClassId, r.FeeCategoryId, r.AcademicYearId,
                                    item.FeeTypeId, item.TermId, item.FeePeriodId,
                                    item.PaymentType, item.AdmissionType,
                                    item.Amount, schoolId
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
                             fp.fee_period_id FeePeriodId, fp.period_label PeriodLabel,
                             fs.payment_type PaymentType,
                             fs.amount StructureAmount,
                             ISNULL((
                                 SELECT SUM(ri.net_amount)
                                 FROM fee_receipt_items ri
                                 INNER JOIN fee_receipts r ON r.receipt_id = ri.receipt_id
                                 WHERE ri.fee_type_id                    = fs.fee_type_id
                                   AND ISNULL(ri.term_id, 0)           = ISNULL(fs.term_id, 0)
                                   AND ISNULL(ri.fee_period_id, 0)     = ISNULL(fs.fee_period_id, 0)
                                   AND r.student_id                     = @studentId
                                   AND r.status                         = 'Active'
                                   AND ri.school_id                     = @schoolId
                             ), 0) PaidAmount,
                             ISNULL((
                                 SELECT SUM(fc.amount)
                                 FROM fee_concessions fc
                                 WHERE fc.student_id  = @studentId
                                   AND fc.fee_type_id = fs.fee_type_id
                                   AND fc.school_id   = @schoolId
                                   AND ISNULL(fc.term_id, 0)       = ISNULL(fs.term_id, 0)
                                   AND ISNULL(fc.fee_period_id, 0) = ISNULL(fs.fee_period_id, 0)
                                   AND fc.status      = 'Active'
                             ), 0) ConcessionAmount
                      FROM fee_structures fs
                      INNER JOIN fee_types ft ON ft.fee_type_id = fs.fee_type_id
                      LEFT  JOIN terms     t  ON t.term_id        = fs.term_id
                      LEFT  JOIN fee_periods fp ON fp.fee_period_id = fs.fee_period_id
                      WHERE fs.class_id         = @classId
                        AND fs.fee_category_id  = @feeCatId
                        AND fs.academic_year_id = @effYearId
                        AND fs.school_id        = @schoolId
                        AND ISNULL(fs.status, 'Active') <> 'Inactive'
                      ORDER BY ISNULL(ft.sequence_no, 9999), ft.fee_type_name,
                               ISNULL(t.order_no, 9999), t.term_name,
                               ISNULL(fp.sequence_no, 9999), fp.period_label",
                    new { studentId, schoolId, classId, feeCatId, effYearId }).ToList();

                foreach (var li in lineItems)
                    li.Outstanding = li.StructureAmount - li.PaidAmount - li.ConcessionAmount;

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

        // ── Cross-year fee summary ────────────────────────────────────────

        public CrossYearFeeSummaryDto GetCrossYearFeeSummary(
            string tenantDbName, int schoolId, int studentUniqueId, string feeTypeCategory)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                // All student rows for this unique_id, newest first
                var rows = conn.Query<StudentYearRow>(
                    @"SELECT s.student_id StudentId, s.admission_no AdmissionNo,
                             s.student_name StudentName, s.join_type JoinType,
                             s.academic_year_id AcademicYearId, ay.academic_year AcademicYear,
                             s.class_id ClassId, c.class_name ClassName,
                             ISNULL(cg.prefix, 'S') ClassGroupPrefix,
                             s.fee_category_id FeeCategoryId, fc.category_name FeeCategoryName,
                             s.bus_route_id BusRouteId,
                             s.hostel_id HostelId
                      FROM students s
                      LEFT JOIN academic_years ay ON ay.academic_year_id = s.academic_year_id
                      LEFT JOIN classes c ON c.class_id = s.class_id
                      LEFT JOIN class_groups cg ON cg.class_group_id = c.class_group_id
                      LEFT JOIN fee_categories fc ON fc.fee_category_id = s.fee_category_id
                      WHERE s.student_unique_id = @studentUniqueId AND s.school_id = @schoolId
                        AND ISNULL(ay.status, 'Active') = 'Active'
                      ORDER BY s.academic_year_id DESC",
                    new { studentUniqueId, schoolId }).ToList();

                if (rows.Count == 0) return null;

                bool isTransport = string.Equals(feeTypeCategory, "transport", StringComparison.OrdinalIgnoreCase);
                bool isHostel    = string.Equals(feeTypeCategory, "hostel",    StringComparison.OrdinalIgnoreCase);
                var catWhere = (isTransport || isHostel) ? null : BuildCategoryFilter(feeTypeCategory);
                var yearSummaries = new List<YearFeeSummaryDto>();
                bool first = true;

                foreach (var row in rows)
                {
                    long studentId = row.StudentId;
                    int? classId   = row.ClassId;
                    int? feeCatId  = row.FeeCategoryId;
                    int  effYearId = row.AcademicYearId ?? 0;

                    List<FeeLineItemDto> lineItems;
                    string displayCategory;

                    if (isTransport)
                    {
                        // Skip years where the student has no route assigned
                        if (row.BusRouteId == null)
                            continue;

                        lineItems = GetTransportLineItems(conn, schoolId, studentId, row.BusRouteId.Value, effYearId);
                        displayCategory = lineItems.Count > 0 ? lineItems[0].FeeTypeName : "—";
                    }
                    else if (isHostel)
                    {
                        // Skip years where the student has no hostel assigned
                        if (row.HostelId == null)
                            continue;

                        lineItems = GetHostelLineItems(conn, schoolId, studentId, row.HostelId.Value, effYearId);
                        displayCategory = lineItems.Count > 0 ? lineItems[0].FeeTypeName : "—";
                    }
                    else
                    {
                        string joinType = row.JoinType;
                        lineItems = conn.Query<FeeLineItemDto>(
                            $@"SELECT ft.fee_type_id FeeTypeId, ft.fee_type_name FeeTypeName,
                                      ft.sequence_no SequenceNo,
                                      t.term_id TermId, t.term_name TermName, t.order_no OrderNo,
                                      fp.fee_period_id FeePeriodId, fp.period_label PeriodLabel,
                                      fs.payment_type PaymentType,
                                      fs.amount StructureAmount,
                                      ISNULL((
                                          SELECT SUM(ri.net_amount)
                                          FROM fee_receipt_items ri
                                          INNER JOIN fee_receipts r ON r.receipt_id = ri.receipt_id
                                          WHERE ri.fee_type_id                      = fs.fee_type_id
                                            AND ISNULL(ri.term_id, 0)             = ISNULL(fs.term_id, 0)
                                            AND ISNULL(ri.fee_period_id, 0)       = ISNULL(fs.fee_period_id, 0)
                                            AND r.student_id                       = @studentId
                                            AND r.status                           = 'Active'
                                            AND ri.school_id                       = @schoolId
                                      ), 0) PaidAmount,
                                      ISNULL((
                                          SELECT SUM(fc.amount)
                                          FROM fee_concessions fc
                                          WHERE fc.student_id  = @studentId
                                            AND fc.fee_type_id = fs.fee_type_id
                                            AND fc.school_id   = @schoolId
                                            AND ISNULL(fc.term_id, 0)       = ISNULL(fs.term_id, 0)
                                            AND ISNULL(fc.fee_period_id, 0) = ISNULL(fs.fee_period_id, 0)
                                            AND fc.status      = 'Active'
                                      ), 0) ConcessionAmount
                               FROM fee_structures fs
                               INNER JOIN fee_types ft ON ft.fee_type_id = fs.fee_type_id
                               LEFT  JOIN terms     t  ON t.term_id        = fs.term_id
                               LEFT  JOIN fee_periods fp ON fp.fee_period_id = fs.fee_period_id
                               WHERE fs.class_id         = @classId
                                 AND fs.fee_category_id  = @feeCatId
                                 AND fs.academic_year_id = @effYearId
                                 AND fs.school_id        = @schoolId
                                 AND ISNULL(fs.status, 'Active') <> 'Inactive'
                                 AND (fs.admission_type IS NULL OR fs.admission_type = @joinType)
                                 AND ({catWhere})
                               ORDER BY ISNULL(ft.sequence_no, 9999), ft.fee_type_name,
                                        ISNULL(t.order_no, 9999), t.term_name,
                                        ISNULL(fp.sequence_no, 9999), fp.period_label",
                            new { studentId, schoolId, classId, feeCatId, effYearId, joinType }).ToList();

                        foreach (var li in lineItems)
                            li.Outstanding = li.StructureAmount - li.PaidAmount - li.ConcessionAmount;

                        displayCategory = row.FeeCategoryName;
                    }

                    yearSummaries.Add(new YearFeeSummaryDto
                    {
                        StudentId        = studentId,
                        AcademicYearId   = effYearId,
                        AcademicYear     = row.AcademicYear,
                        ClassName        = row.ClassName,
                        FeeCategoryName  = displayCategory,
                        ClassGroupPrefix = row.ClassGroupPrefix,
                        IsCurrent        = first,
                        TotalStructure   = lineItems.Sum(li => li.StructureAmount),
                        TotalPaid        = lineItems.Sum(li => li.PaidAmount),
                        TotalOutstanding = lineItems.Sum(li => li.Outstanding),
                        LineItems        = lineItems,
                    });
                    first = false;
                }

                var firstRow = rows[0];
                return new CrossYearFeeSummaryDto
                {
                    StudentUniqueId = studentUniqueId,
                    StudentName     = firstRow.StudentName,
                    AdmissionNo     = firstRow.AdmissionNo,
                    JoinType        = firstRow.JoinType,
                    Years           = yearSummaries,
                };
            }
        }

        private static List<FeeLineItemDto> GetTransportLineItems(
            System.Data.IDbConnection conn, int schoolId, long studentId, int busRouteId, int academicYearId)
        {
            var items = conn.Query<FeeLineItemDto>(
                @"SELECT t.term_id TermId, t.term_name TermName, t.order_no OrderNo,
                         bfs.amount StructureAmount,
                         br.route_id BusRouteId,
                         br.route_name FeeTypeName,
                         ISNULL((
                             SELECT SUM(fri.net_amount)
                             FROM fee_receipt_items fri
                             INNER JOIN fee_receipts fr ON fr.receipt_id = fri.receipt_id
                             WHERE fri.bus_route_id = bfs.route_id
                               AND fri.term_id      = bfs.term_id
                               AND fr.student_id    = @studentId
                               AND fr.status        = 'Active'
                               AND fri.school_id    = @schoolId
                         ), 0) PaidAmount,
                         ISNULL((
                             SELECT SUM(fc.amount)
                             FROM fee_concessions fc
                             WHERE fc.student_id   = @studentId
                               AND fc.bus_route_id = bfs.route_id
                               AND fc.school_id    = @schoolId
                               AND ISNULL(fc.term_id, 0) = bfs.term_id
                               AND fc.status       = 'Active'
                         ), 0) ConcessionAmount
                  FROM bus_fee_structures bfs
                  INNER JOIN terms      t  ON t.term_id   = bfs.term_id
                  INNER JOIN bus_routes br ON br.route_id = bfs.route_id
                  WHERE bfs.route_id         = @busRouteId
                    AND bfs.academic_year_id = @academicYearId
                    AND bfs.school_id        = @schoolId
                  ORDER BY t.order_no",
                new { schoolId, studentId, busRouteId, academicYearId }).ToList();

            foreach (var li in items)
                li.Outstanding = li.StructureAmount - li.PaidAmount - li.ConcessionAmount;

            return items;
        }

        private static List<FeeLineItemDto> GetHostelLineItems(
            System.Data.IDbConnection conn, int schoolId, long studentId, int hostelId, int academicYearId)
        {
            var items = conn.Query<FeeLineItemDto>(
                @"SELECT t.term_id TermId, t.term_name TermName, t.order_no OrderNo,
                         fp.fee_period_id FeePeriodId, fp.period_label PeriodLabel,
                         fp.sequence_no PeriodSequenceNo,
                         hfs.payment_type PaymentType,
                         hfs.amount StructureAmount,
                         hfs.hostel_id HostelId,
                         h.hostel_name FeeTypeName,
                         ISNULL((
                             SELECT SUM(fri.net_amount)
                             FROM fee_receipt_items fri
                             INNER JOIN fee_receipts fr ON fr.receipt_id = fri.receipt_id
                             WHERE fri.hostel_id  = hfs.hostel_id
                               AND ISNULL(fri.term_id, 0)       = ISNULL(hfs.term_id, 0)
                               AND ISNULL(fri.fee_period_id, 0) = ISNULL(hfs.fee_period_id, 0)
                               AND fr.student_id  = @studentId
                               AND fr.status      = 'Active'
                               AND fri.school_id  = @schoolId
                         ), 0) PaidAmount,
                         ISNULL((
                             SELECT SUM(fc.amount)
                             FROM fee_concessions fc
                             WHERE fc.student_id = @studentId
                               AND fc.hostel_id  = hfs.hostel_id
                               AND fc.school_id  = @schoolId
                               AND ISNULL(fc.term_id, 0)       = ISNULL(hfs.term_id, 0)
                               AND ISNULL(fc.fee_period_id, 0) = ISNULL(hfs.fee_period_id, 0)
                               AND fc.status     = 'Active'
                         ), 0) ConcessionAmount
                  FROM hostel_fee_structures hfs
                  INNER JOIN hostels     h  ON h.hostel_id  = hfs.hostel_id
                  LEFT  JOIN terms       t  ON t.term_id    = hfs.term_id
                  LEFT  JOIN fee_periods fp ON fp.fee_period_id = hfs.fee_period_id
                  WHERE hfs.hostel_id = @hostelId
                    AND hfs.academic_year_id = @academicYearId
                    AND hfs.school_id = @schoolId
                  ORDER BY ISNULL(t.order_no, 9999), t.term_name,
                           ISNULL(fp.sequence_no, 9999), fp.period_label",
                new { schoolId, studentId, hostelId, academicYearId }).ToList();

            foreach (var li in items)
                li.Outstanding = li.StructureAmount - li.PaidAmount - li.ConcessionAmount;

            return items;
        }

        private static string BuildCategoryFilter(string category)
        {
            switch ((category ?? "").ToLower())
            {
                case "admission":
                    return "LOWER(ISNULL(ft.description,'')) LIKE '%New Joining Fees%'";
                case "transport":
                    return "LOWER(ISNULL(ft.description,'')) LIKE '%transport%' OR LOWER(ISNULL(ft.description,'')) LIKE '%bus%'";
                case "hostel":
                    return "LOWER(ISNULL(ft.description,'')) LIKE '%hostel%'";
                case "school":
                    return "LOWER(ISNULL(ft.description,'')) LIKE '%regular fee%' OR LOWER(ISNULL(ft.description,'')) LIKE '%general fees%' OR LOWER(ISNULL(ft.description,'')) LIKE '%tuition%'";
                default: // other = catch-all
                    return @"LOWER(ISNULL(ft.description,'')) NOT LIKE '%regular fee%'
                         AND LOWER(ISNULL(ft.description,'')) NOT LIKE '%transport%'
                         AND LOWER(ISNULL(ft.description,'')) NOT LIKE '%bus%'
                         AND LOWER(ISNULL(ft.description,'')) NOT LIKE '%hostel%'
                         AND LOWER(ISNULL(ft.description,'')) NOT LIKE '%general fees%'
                         AND LOWER(ISNULL(ft.description,'')) NOT LIKE '%tuition%'
                         AND LOWER(ISNULL(ft.description,'')) NOT LIKE '%new joining fees%'";
            }
        }

        private static string GetReceiptPrefix(string feeTypeCategory, string classGroupPrefix)
        {
            switch ((feeTypeCategory ?? "").ToLower())
            {
                case "transport": return "T";
                case "hostel":    return "H";
                case "other":     return "O";
                default:          return string.IsNullOrWhiteSpace(classGroupPrefix) ? "S" : classGroupPrefix;
            }
        }

        private static string FormatYr6(string academicYear)
        {
            // "2024-25" → "202425"  |  "2024-2025" → "202425"
            if (string.IsNullOrWhiteSpace(academicYear)) return DateTime.Now.Year.ToString();
            return academicYear.Substring(0, 4) + academicYear.Substring(academicYear.Length - 2);
        }

        // ── Fee Collection ────────────────────────────────────────────────

        public int CollectFee(string tenantDbName, int schoolId, string createdBy, CollectFeeRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        // Get student's academic year string and class_group prefix for receipt_no
                        var studentMeta = conn.QuerySingle<StudentCollectMeta>(
                            @"SELECT ay.academic_year AcademicYear,
                                     ISNULL(cg.prefix, 'S') ClassGroupPrefix,
                                     s.student_unique_id StudentUniqueId
                              FROM students s
                              LEFT JOIN academic_years ay ON ay.academic_year_id = s.academic_year_id
                              LEFT JOIN classes c ON c.class_id = s.class_id
                              LEFT JOIN class_groups cg ON cg.class_group_id = c.class_group_id
                              WHERE s.student_id = @StudentId AND s.school_id = @schoolId",
                            new { r.StudentId, schoolId }, tx);

                        var prefix    = GetReceiptPrefix(r.FeeTypeCategory, studentMeta.ClassGroupPrefix);
                        var yr6       = FormatYr6(studentMeta.AcademicYear);
                        var pattern   = $"{prefix}{yr6}%";
                        var maxSuffix = conn.QuerySingle<int?>(
                            @"SELECT MAX(TRY_CAST(RIGHT(receipt_no, 5) AS INT))
                              FROM fee_receipts
                              WHERE receipt_no LIKE @pattern AND school_id = @schoolId
                                AND LEN(receipt_no) = @expectedLen",
                            new { pattern, schoolId, expectedLen = prefix.Length + yr6.Length + 5 }, tx) ?? 0;
                        var receiptNo = $"{prefix}{yr6}{(maxSuffix + 1):D5}";

                        int? studentUniqueId = r.StudentUniqueId ?? studentMeta.StudentUniqueId;

                        decimal total = r.Items == null ? 0
                            : (decimal)r.Items.Sum(i => (double)(i.Amount - i.ConcessionAmount));

                        var receiptId = conn.QuerySingle<int>(
                            @"INSERT INTO fee_receipts
                                (receipt_no, student_id, student_unique_id, academic_year_id,
                                 payment_date, total_amount, payment_mode_id,
                                 cheque_no, cheque_date, bank_name, remarks,
                                 status, source, school_id, created_by)
                              VALUES
                                (@receiptNo, @StudentId, @studentUniqueId, @AcademicYearId,
                                 ISNULL(@PaymentDate, CAST(GETDATE() AS DATE)),
                                 @total, @PaymentModeId,
                                 @ChequeNo, @ChequeDate, @BankName, @Remarks,
                                 'Active', 'webapp', @schoolId, @createdBy);
                              SELECT CAST(SCOPE_IDENTITY() AS INT)",
                            new
                            {
                                receiptNo, r.StudentId, studentUniqueId, r.AcademicYearId,
                                r.PaymentDate, total, r.PaymentModeId, r.ChequeNo, r.ChequeDate,
                                r.BankName, r.Remarks, schoolId, createdBy
                            }, tx);

                        if (r.Items != null && r.Items.Count > 0)
                            conn.Execute(
                                @"INSERT INTO fee_receipt_items
                                    (receipt_id, fee_type_id, term_id, fee_period_id, bus_route_id, hostel_id,
                                     amount, concession_amount, net_amount, school_id)
                                  VALUES
                                    (@receiptId, @FeeTypeId, @TermId, @FeePeriodId, @BusRouteId, @HostelId,
                                     @Amount, @ConcessionAmount, @NetAmount, @schoolId)",
                                r.Items.Select(i => new
                                {
                                    receiptId,
                                    i.FeeTypeId,
                                    i.TermId,
                                    i.FeePeriodId,
                                    i.BusRouteId,
                                    i.HostelId,
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
            string search, DateTime? dateFrom, DateTime? dateTo, string status,
            DateTime? createdAfter = null, string source = null, DateTime? createdBefore = null)
        {
            var where = "r.school_id = @schoolId";
            if (!string.IsNullOrWhiteSpace(search))
                where += " AND (s.student_name LIKE @search OR s.admission_no LIKE @search OR r.receipt_no LIKE @search)";
            if (dateFrom.HasValue)   where += " AND r.payment_date >= @dateFrom";
            if (dateTo.HasValue)     where += " AND r.payment_date <= @dateTo";
            if (!string.IsNullOrWhiteSpace(status)) where += " AND r.status = @status";
            if (createdAfter.HasValue)  where += " AND r.created_at >= @createdAfter";
            if (createdBefore.HasValue) where += " AND r.created_at <= @createdBefore";
            if (!string.IsNullOrWhiteSpace(source)) where += " AND r.source = @source";

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
                    new { schoolId, search = $"%{search}%", dateFrom, dateTo, status, createdAfter, source, createdBefore });
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
                             ISNULL(br.route_name, '')    RouteName,
                             ISNULL(h.hostel_name, '')    HostelName,
                             ISNULL(t.term_name, '')      TermName,
                             ri.amount Amount, ri.concession_amount ConcessionAmount,
                             ri.net_amount NetAmount
                      FROM fee_receipt_items ri
                      LEFT JOIN fee_types  ft ON ft.fee_type_id = ri.fee_type_id
                      LEFT JOIN terms      t  ON t.term_id      = ri.term_id
                      LEFT JOIN bus_routes br ON br.route_id    = ri.bus_route_id
                      LEFT JOIN hostels    h  ON h.hostel_id    = ri.hostel_id
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
                var years      = conn.Query<FeeBulkLookup>("SELECT academic_year_id AS Id, academic_year AS Name FROM academic_years WHERE school_id = @schoolId", new { schoolId });
                var classes    = conn.Query<FeeBulkLookup>("SELECT class_id AS Id, class_name AS Name FROM classes WHERE school_id = @schoolId", new { schoolId });
                var cats       = conn.Query<FeeBulkLookup>("SELECT fee_category_id AS Id, category_name AS Name FROM fee_categories WHERE school_id = @schoolId", new { schoolId });
                var feeTypes   = conn.Query<FeeBulkLookup>("SELECT fee_type_id AS Id, fee_type_name AS Name FROM fee_types WHERE school_id = @schoolId", new { schoolId });
                var terms      = conn.Query<FeeBulkLookup>("SELECT term_id AS Id, term_name AS Name FROM terms WHERE school_id = @schoolId", new { schoolId });
                var feePeriods = conn.Query<FeeBulkLookup>("SELECT fee_period_id AS Id, period_label AS Name FROM fee_periods WHERE school_id = @schoolId", new { schoolId });

                var yearMap       = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var classMap      = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var catMap        = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var feeTypeMap    = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var termMap       = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var feePeriodMap  = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                foreach (var y in years)      yearMap[y.Name]      = y.Id;
                foreach (var c in classes)    classMap[c.Name]     = c.Id;
                foreach (var c in cats)       catMap[c.Name]       = c.Id;
                foreach (var f in feeTypes)   feeTypeMap[f.Name]   = f.Id;
                foreach (var t in terms)      termMap[t.Name]      = t.Id;
                foreach (var p in feePeriods) feePeriodMap[p.Name] = p.Id;

                foreach (var row in request.Rows)
                {
                    var payType    = (row.PaymentType ?? "").Trim();
                    var termLabel  = (row.Term       ?? "").Trim();
                    var periodLabel = (row.FeePeriod ?? "").Trim();
                    var identifier = $"{row.ClassName} / {row.FeeType} / {(payType == "Monthly" ? periodLabel : termLabel)}";

                    // ── Required fields ───────────────────────────────────────
                    if (string.IsNullOrWhiteSpace(row.AcademicYear) || string.IsNullOrWhiteSpace(row.ClassName) ||
                        string.IsNullOrWhiteSpace(row.FeeCategory)  || string.IsNullOrWhiteSpace(row.FeeType) ||
                        string.IsNullOrWhiteSpace(payType)          || string.IsNullOrWhiteSpace(row.Amount))
                    { AddFeeError(result, row, identifier, "AcademicYear, ClassName, FeeCategory, FeeType, PaymentType and Amount are required."); continue; }

                    if (payType != "Term" && payType != "Monthly")
                    { AddFeeError(result, row, identifier, $"PaymentType must be 'Term' or 'Monthly', got '{payType}'"); continue; }

                    // ── Resolve common lookups ────────────────────────────────
                    if (!yearMap.TryGetValue(row.AcademicYear.Trim(), out var yearId))
                    { AddFeeError(result, row, identifier, $"Academic year '{row.AcademicYear}' not found"); continue; }

                    if (!classMap.TryGetValue(row.ClassName.Trim(), out var classId))
                    { AddFeeError(result, row, identifier, $"Class '{row.ClassName}' not found"); continue; }

                    if (!catMap.TryGetValue(row.FeeCategory.Trim(), out var catId))
                    { AddFeeError(result, row, identifier, $"Fee category '{row.FeeCategory}' not found"); continue; }

                    if (!feeTypeMap.TryGetValue(row.FeeType.Trim(), out var feeTypeId))
                    { AddFeeError(result, row, identifier, $"Fee type '{row.FeeType}' not found"); continue; }

                    if (!decimal.TryParse(row.Amount.Trim(), out var amount) || amount < 0)
                    { AddFeeError(result, row, identifier, $"Invalid amount '{row.Amount}'"); continue; }

                    // ── Resolve Term or Period ────────────────────────────────
                    int? termId     = null;
                    int? periodId   = null;

                    if (payType == "Term")
                    {
                        if (string.IsNullOrWhiteSpace(termLabel))
                        { AddFeeError(result, row, identifier, "Term is required when PaymentType is 'Term'"); continue; }
                        if (!termMap.TryGetValue(termLabel, out var tid))
                        { AddFeeError(result, row, identifier, $"Term '{termLabel}' not found"); continue; }
                        termId = tid;
                    }
                    else // Monthly
                    {
                        if (string.IsNullOrWhiteSpace(periodLabel))
                        { AddFeeError(result, row, identifier, "FeePeriod is required when PaymentType is 'Monthly'"); continue; }
                        if (!feePeriodMap.TryGetValue(periodLabel, out var pid))
                        { AddFeeError(result, row, identifier, $"Fee period '{periodLabel}' not found — add it in Master Data → Fee Periods first"); continue; }
                        periodId = pid;
                    }

                    string admissionType = string.IsNullOrWhiteSpace(row.AdmissionType) ? null : row.AdmissionType.Trim();

                    // ── Upsert ───────────────────────────────────────────────
                    conn.Execute(@"
                        DELETE FROM fee_structures
                        WHERE class_id = @classId AND fee_category_id = @catId
                          AND academic_year_id = @yearId AND fee_type_id = @feeTypeId
                          AND ISNULL(term_id, 0)        = ISNULL(@termId, 0)
                          AND ISNULL(fee_period_id, 0)  = ISNULL(@periodId, 0)
                          AND ISNULL(admission_type, '') = ISNULL(@admissionType, '')
                          AND school_id = @schoolId",
                        new { classId, catId, yearId, feeTypeId, termId, periodId, admissionType, schoolId });

                    conn.Execute(@"
                        INSERT INTO fee_structures
                            (class_id, fee_category_id, academic_year_id, fee_type_id,
                             term_id, fee_period_id, payment_type, admission_type,
                             amount, status, school_id)
                        VALUES
                            (@classId, @catId, @yearId, @feeTypeId,
                             @termId, @periodId, @payType, @admissionType,
                             @amount, 'Active', @schoolId)",
                        new { classId, catId, yearId, feeTypeId, termId, periodId, payType, admissionType, amount, schoolId });

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

        // ── Bulk receipt import (legacy data) ─────────────────────────────────

        public BulkImportResult BulkImportReceipts(
            string tenantDbName, int schoolId, string createdBy, BulkReceiptImportRequest request)
        {
            var result = new BulkImportResult { Total = request.Rows.Count };

            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var yearMap    = ToCI(conn.Query<FeeBulkLookup>("SELECT academic_year_id AS Id, academic_year AS Name FROM academic_years WHERE school_id=@schoolId", new { schoolId }));
                var feeTypeMap = ToCI(conn.Query<FeeBulkLookup>("SELECT fee_type_id AS Id, fee_type_name AS Name FROM fee_types WHERE school_id=@schoolId", new { schoolId }));
                // Year-scoped: term/period names repeat across academic years, so key by name + academic_year_id.
                var termMap    = ToCIByYear(conn.Query<FeeBulkLookup>("SELECT term_id AS Id, term_name AS Name, academic_year_id AS YearId FROM terms WHERE school_id=@schoolId", new { schoolId }));
                var periodMap  = ToCIByYear(conn.Query<FeeBulkLookup>("SELECT fee_period_id AS Id, period_label AS Name, academic_year_id AS YearId FROM fee_periods WHERE school_id=@schoolId", new { schoolId }));
                var routeMap   = ToCI(conn.Query<FeeBulkLookup>("SELECT route_id AS Id, route_name AS Name FROM bus_routes WHERE school_id=@schoolId", new { schoolId }));
                var hostelMap  = ToCI(conn.Query<FeeBulkLookup>("SELECT hostel_id AS Id, hostel_name AS Name FROM hostels WHERE school_id=@schoolId", new { schoolId }));
                var payModeMap = ToCI(conn.Query<FeeBulkLookup>("SELECT payment_mode_id AS Id, mode_name AS Name FROM payment_modes WHERE school_id=@schoolId", new { schoolId }));

                // Group rows: use LegacyReceiptNo as key when present; else key on admission+year+date+mode
                var groups = new Dictionary<string, List<BulkReceiptRow>>(StringComparer.OrdinalIgnoreCase);
                foreach (var row in request.Rows)
                {
                    var leg = (row.LegacyReceiptNo ?? "").Trim();
                    var key = string.IsNullOrEmpty(leg)
                        ? $"A|{(row.AdmissionNo??"").Trim()}|{(row.AcademicYear??"").Trim()}|{(row.PaymentDate??"").Trim()}|{(row.PaymentMode??"").Trim()}"
                        : $"L|{(row.AdmissionNo??"").Trim()}|{(row.AcademicYear??"").Trim()}|{leg}";
                    if (!groups.ContainsKey(key)) groups[key] = new List<BulkReceiptRow>();
                    groups[key].Add(row);
                }

                foreach (var grp in groups)
                {
                    var rows  = grp.Value;
                    var first = rows[0];

                    // Group-level checks
                    if (!yearMap.TryGetValue((first.AcademicYear ?? "").Trim(), out int yearId))
                    {
                        BulkReceiptError(result, rows, $"Academic year not found: {first.AcademicYear}"); continue;
                    }
                    if (!payModeMap.TryGetValue((first.PaymentMode ?? "").Trim(), out int payModeId))
                    {
                        BulkReceiptError(result, rows, $"Payment mode not found: {first.PaymentMode}"); continue;
                    }
                    if (!TryParseDate((first.PaymentDate ?? "").Trim(), out DateTime payDate))
                    {
                        BulkReceiptError(result, rows, $"Invalid payment date: {first.PaymentDate}"); continue;
                    }

                    var meta = conn.QueryFirstOrDefault<ReceiptBulkStudentMeta>(
                        @"SELECT s.student_id StudentId, s.student_unique_id StudentUniqueId,
                                 s.academic_year_id AcademicYearId, ay.academic_year AcademicYear,
                                 ISNULL(cg.prefix, 'S') ClassGroupPrefix
                          FROM students s
                          LEFT JOIN academic_years ay ON ay.academic_year_id = s.academic_year_id
                          LEFT JOIN classes c ON c.class_id = s.class_id
                          LEFT JOIN class_groups cg ON cg.class_group_id = c.class_group_id
                          WHERE s.admission_no = @admNo AND s.academic_year_id = @yearId AND s.school_id = @schoolId",
                        new { admNo = (first.AdmissionNo ?? "").Trim(), yearId, schoolId });

                    if (meta == null)
                    {
                        BulkReceiptError(result, rows, $"Student '{first.AdmissionNo}' not found for {first.AcademicYear}"); continue;
                    }

                    // Per-row item validation
                    var items    = new List<BulkReceiptItemData>();
                    var rowErrs  = new List<BulkRowError>();

                    foreach (var r in rows)
                    {
                        if (!decimal.TryParse(r.PaidAmount, out decimal amount) || amount <= 0)
                        {
                            rowErrs.Add(new BulkRowError { Row = r.RowNumber, AdmissionNo = r.AdmissionNo, Reason = $"Invalid amount: {r.PaidAmount}" }); continue;
                        }

                        int? feeTypeId = null, termId = null, feePeriodId = null, busRouteId = null, hostelId = null;
                        var  cat       = (r.FeeCategory ?? "").Trim().ToLower();
                        bool ok        = true;

                        if (cat == "transport")
                        {
                            var rn = (r.RouteName ?? "").Trim();
                            if (!string.IsNullOrEmpty(rn))
                            {
                                if (!routeMap.TryGetValue(rn, out int rid)) { rowErrs.Add(new BulkRowError { Row = r.RowNumber, AdmissionNo = r.AdmissionNo, Reason = $"Route not found: {rn}" }); ok = false; }
                                else busRouteId = rid;
                            }
                        }
                        else if (cat == "hostel")
                        {
                            var hn = (r.HostelName ?? "").Trim();
                            if (!string.IsNullOrEmpty(hn))
                            {
                                if (!hostelMap.TryGetValue(hn, out int hid)) { rowErrs.Add(new BulkRowError { Row = r.RowNumber, AdmissionNo = r.AdmissionNo, Reason = $"Hostel not found: {hn}" }); ok = false; }
                                else hostelId = hid;
                            }
                        }
                        else
                        {
                            var fn = (r.FeeTypeName ?? "").Trim();
                            if (!string.IsNullOrEmpty(fn))
                            {
                                if (!feeTypeMap.TryGetValue(fn, out int ftid)) { rowErrs.Add(new BulkRowError { Row = r.RowNumber, AdmissionNo = r.AdmissionNo, Reason = $"Fee type not found: {fn}" }); ok = false; }
                                else feeTypeId = ftid;
                            }
                        }

                        if (!ok) continue;

                        var tn = (r.TermName ?? "").Trim();
                        if (!string.IsNullOrEmpty(tn))
                        {
                            if (termMap.TryGetValue(YearKey(tn, yearId), out int tid)) termId = tid;
                            else if (periodMap.TryGetValue(YearKey(tn, yearId), out int pid)) feePeriodId = pid;
                        }

                        items.Add(new BulkReceiptItemData
                        {
                            FeeTypeId = feeTypeId, TermId = termId, FeePeriodId = feePeriodId,
                            BusRouteId = busRouteId, HostelId = hostelId, Amount = amount,
                        });
                    }

                    if (rowErrs.Count > 0) { result.Errors.AddRange(rowErrs); result.Failed += rows.Count; continue; }

                    // Resolve receipt_no: use LegacyReceiptNo directly when provided; else auto-generate
                    var legacyNo = (first.LegacyReceiptNo ?? "").Trim();
                    var feeCtgry = (first.FeeCategory ?? "School").Trim();
                    string receiptNo;

                    if (!string.IsNullOrEmpty(legacyNo))
                    {
                        // Dedup: skip if already imported (idempotent re-run)
                        var alreadyExists = conn.QuerySingle<int>(
                            "SELECT COUNT(*) FROM fee_receipts WHERE receipt_no = @legacyNo AND school_id = @schoolId",
                            new { legacyNo, schoolId });
                        if (alreadyExists > 0)
                        {
                            result.Skipped += rows.Count;
                            continue;
                        }
                        receiptNo = legacyNo;
                    }
                    else
                    {
                        var prefix = GetReceiptPrefix(feeCtgry, meta.ClassGroupPrefix);
                        var yr6    = FormatYr6(meta.AcademicYear);
                        var maxSfx = conn.QuerySingle<int?>(
                            @"SELECT MAX(TRY_CAST(RIGHT(receipt_no, 5) AS INT))
                              FROM fee_receipts
                              WHERE receipt_no LIKE @pattern AND school_id = @schoolId
                                AND LEN(receipt_no) = @expectedLen",
                            new { pattern = $"{prefix}{yr6}%", schoolId, expectedLen = prefix.Length + yr6.Length + 5 }) ?? 0;
                        receiptNo = $"{prefix}{yr6}{(maxSfx + 1):D5}";
                    }

                    // Resolve status and cancel fields
                    var statusRaw    = (first.Status ?? "").Trim().ToUpper();
                    var receiptStatus = (statusRaw == "D" || statusRaw == "CANCELLED") ? "Cancelled" : "Active";
                    var cancelReason  = receiptStatus == "Cancelled"
                        ? (string.IsNullOrWhiteSpace(first.CancelReason) ? "Cancelled in legacy" : first.CancelReason.Trim())
                        : (string)null;

                    var remarks  = LegacyRemarks(first);

                    using (var tx = conn.BeginTransaction())
                    {
                        try
                        {
                            var total    = items.Sum(i => i.Amount);
                            var chequeNo = string.IsNullOrWhiteSpace(first.ChequeNo) ? null : first.ChequeNo.Trim();
                            var bankName = string.IsNullOrWhiteSpace(first.BankName)  ? null : first.BankName.Trim();

                            var rid = conn.QuerySingle<int>(
                                @"INSERT INTO fee_receipts
                                    (receipt_no, student_id, student_unique_id, academic_year_id,
                                     payment_date, total_amount, payment_mode_id,
                                     cheque_no, bank_name, remarks,
                                     status, cancel_reason, cancelled_by, cancelled_at,
                                     source, school_id, created_by)
                                  VALUES
                                    (@receiptNo, @StudentId, @StudentUniqueId, @yearId,
                                     @payDate, @total, @payModeId,
                                     @chequeNo, @bankName, @remarks,
                                     @receiptStatus, @cancelReason,
                                     CASE WHEN @receiptStatus = 'Cancelled' THEN 'legacy' ELSE NULL END,
                                     CASE WHEN @receiptStatus = 'Cancelled' THEN GETDATE() ELSE NULL END,
                                     'legacy', @schoolId, @createdBy);
                                  SELECT CAST(SCOPE_IDENTITY() AS INT)",
                                new { receiptNo, meta.StudentId, meta.StudentUniqueId, yearId,
                                      payDate, total, payModeId, chequeNo, bankName,
                                      remarks, receiptStatus, cancelReason,
                                      schoolId, createdBy }, tx);

                            conn.Execute(
                                @"INSERT INTO fee_receipt_items
                                    (receipt_id, fee_type_id, term_id, fee_period_id, bus_route_id, hostel_id,
                                     amount, concession_amount, net_amount, school_id)
                                  VALUES
                                    (@rid, @FeeTypeId, @TermId, @FeePeriodId, @BusRouteId, @HostelId,
                                     @Amount, 0, @Amount, @schoolId)",
                                items.Select(i => new { rid, i.FeeTypeId, i.TermId, i.FeePeriodId,
                                    i.BusRouteId, i.HostelId, i.Amount, schoolId }), tx);

                            tx.Commit();
                            result.Imported += rows.Count;
                        }
                        catch (Exception ex)
                        {
                            tx.Rollback();
                            BulkReceiptError(result, rows, $"DB error: {ex.Message}");
                        }
                    }
                }
            }

            result.Failed = result.Total - result.Imported - result.Skipped;
            return result;
        }

        private static Dictionary<string, int> ToCI(IEnumerable<FeeBulkLookup> src)
        {
            var d = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var i in src) if (!string.IsNullOrEmpty(i.Name) && !d.ContainsKey(i.Name)) d[i.Name] = i.Id;
            return d;
        }

        // Year-scoped lookup — keyed "name|academicYearId". Used for fee types, terms and
        // fee periods, whose names repeat across academic years (e.g. "Term 1" every year).
        private static Dictionary<string, int> ToCIByYear(IEnumerable<FeeBulkLookup> src)
        {
            var d = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var i in src)
            {
                if (string.IsNullOrEmpty(i.Name)) continue;
                var key = $"{i.Name}|{i.YearId ?? 0}";
                if (!d.ContainsKey(key)) d[key] = i.Id;
            }
            return d;
        }

        private static string YearKey(string name, int yearId) => $"{(name ?? "").Trim()}|{yearId}";

        private static void BulkReceiptError(BulkImportResult result, List<BulkReceiptRow> rows, string reason)
        {
            foreach (var r in rows)
                result.Errors.Add(new BulkRowError { Row = r.RowNumber, AdmissionNo = r.AdmissionNo, Reason = reason });
            result.Failed += rows.Count;
        }

        private static bool TryParseDate(string s, out DateTime d)
        {
            return DateTime.TryParseExact(s, new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd" },
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out d)
                || DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out d);
        }

        private static string LegacyRemarks(BulkReceiptRow r)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(r.LegacyReceiptNo)) parts.Add($"Legacy: {r.LegacyReceiptNo.Trim()}");
            if (!string.IsNullOrWhiteSpace(r.Remarks))         parts.Add(r.Remarks.Trim());
            return parts.Count > 0 ? string.Join("; ", parts) : (string)null;
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
        public int    Id     { get; set; }
        public string Name   { get; set; }
        public int?   YearId { get; set; }   // set for year-scoped lookups (fee types, terms, fee periods)
    }

    internal class StudentYearRow
    {
        public long   StudentId       { get; set; }
        public string AdmissionNo     { get; set; }
        public string StudentName     { get; set; }
        public string JoinType        { get; set; }
        public int?   AcademicYearId  { get; set; }
        public string AcademicYear    { get; set; }
        public int?   ClassId         { get; set; }
        public string ClassName       { get; set; }
        public string ClassGroupPrefix { get; set; }
        public int?   FeeCategoryId   { get; set; }
        public string FeeCategoryName { get; set; }
        public int?   BusRouteId      { get; set; }
        public int?   HostelId        { get; set; }
    }

    internal class StudentCollectMeta
    {
        public string AcademicYear    { get; set; }
        public string ClassGroupPrefix { get; set; }
        public int?   StudentUniqueId { get; set; }
    }

    internal class ReceiptBulkStudentMeta
    {
        public long   StudentId        { get; set; }
        public int?   StudentUniqueId  { get; set; }
        public int?   AcademicYearId   { get; set; }
        public string AcademicYear     { get; set; }
        public string ClassGroupPrefix { get; set; }
    }

    internal class BulkReceiptItemData
    {
        public int?    FeeTypeId   { get; set; }
        public int?    TermId      { get; set; }
        public int?    FeePeriodId { get; set; }
        public int?    BusRouteId  { get; set; }
        public int?    HostelId    { get; set; }
        public decimal Amount      { get; set; }
    }
}
