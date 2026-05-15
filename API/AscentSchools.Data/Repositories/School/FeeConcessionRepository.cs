using AscentSchools.Core.DTOs.School.Fee;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace AscentSchools.Data.Repositories.School
{
    public class FeeConcessionRepository
    {
        private readonly IConnectionFactory _db;
        public FeeConcessionRepository(IConnectionFactory db) { _db = db; }

        // ── List ─────────────────────────────────────────────────────────────

        public IEnumerable<FeeConcessionDto> GetConcessions(
            string tenantDbName, int schoolId, int? academicYearId, int? classId, int? sectionId)
        {
            var where = "fc.school_id = @schoolId AND fc.status = 'Active'";
            if (academicYearId.HasValue) where += " AND fc.academic_year_id = @academicYearId";
            if (classId.HasValue)        where += " AND s.class_id = @classId";
            if (sectionId.HasValue)      where += " AND s.section_id = @sectionId";

            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<FeeConcessionDto>(
                    $@"SELECT fc.concession_id ConcessionId, fc.student_id StudentId,
                              s.student_name StudentName, s.admission_no AdmissionNo,
                              c.class_name ClassName,
                              fc.fee_type_id FeeTypeId, ft.fee_type_name FeeTypeName,
                              fc.bus_route_id BusRouteId, br.route_name RouteName,
                              fc.hostel_id HostelId, h.hostel_name HostelName,
                              fc.term_id TermId, t.term_name TermName,
                              fc.fee_period_id FeePeriodId, fp.period_label PeriodLabel,
                              fc.concession_type ConcessionType, fc.amount Amount,
                              fc.remarks Remarks, fc.receipt_no ReceiptNo,
                              fc.academic_year_id AcademicYearId, ay.academic_year AcademicYear,
                              fc.created_by CreatedBy, fc.created_at CreatedAt
                       FROM fee_concessions fc
                       INNER JOIN students       s  ON s.student_id       = fc.student_id
                       LEFT  JOIN classes        c  ON c.class_id         = s.class_id
                       LEFT  JOIN fee_types      ft ON ft.fee_type_id     = fc.fee_type_id
                       LEFT  JOIN bus_routes     br ON br.route_id        = fc.bus_route_id
                       LEFT  JOIN hostels        h  ON h.hostel_id        = fc.hostel_id
                       LEFT  JOIN terms          t  ON t.term_id          = fc.term_id
                       LEFT  JOIN fee_periods    fp ON fp.fee_period_id   = fc.fee_period_id
                       LEFT  JOIN academic_years ay ON ay.academic_year_id= fc.academic_year_id
                       WHERE {where}
                       ORDER BY s.student_name, ISNULL(ft.fee_type_name, ISNULL(br.route_name, h.hostel_name))",
                    new { schoolId, academicYearId, classId, sectionId });
        }

        // ── Individual save (upsert) ──────────────────────────────────────────

        public FeeConcessionDto SaveConcession(
            string tenantDbName, int schoolId, string createdBy, SaveFeeConcessionRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var existingId = conn.QueryFirstOrDefault<int?>(
                    @"SELECT concession_id FROM fee_concessions
                      WHERE student_id    = @StudentId
                        AND school_id     = @schoolId
                        AND ISNULL(fee_type_id,  0) = ISNULL(@FeeTypeId,  0)
                        AND ISNULL(bus_route_id, 0) = ISNULL(@BusRouteId, 0)
                        AND ISNULL(hostel_id,    0) = ISNULL(@HostelId,   0)
                        AND ISNULL(term_id, 0)        = ISNULL(@TermId, 0)
                        AND ISNULL(fee_period_id, 0)  = ISNULL(@FeePeriodId, 0)
                        AND status = 'Active'",
                    new { r.StudentId, r.FeeTypeId, r.BusRouteId, r.HostelId, schoolId, r.TermId, r.FeePeriodId });

                int concessionId;
                if (existingId.HasValue)
                {
                    conn.Execute(
                        @"UPDATE fee_concessions
                          SET concession_type = @ConcessionType, amount = @Amount,
                              remarks = @Remarks, created_by = @createdBy, created_at = GETDATE()
                          WHERE concession_id = @existingId",
                        new { r.ConcessionType, r.Amount, r.Remarks, createdBy, existingId = existingId.Value });
                    concessionId = existingId.Value;
                }
                else
                {
                    var receiptNo  = GenerateReceiptNo(conn, schoolId, r.AcademicYearId);
                    var uniqueId   = conn.QueryFirstOrDefault<int?>(
                        "SELECT student_unique_id FROM students WHERE student_id = @StudentId",
                        new { r.StudentId });

                    concessionId = conn.QuerySingle<int>(
                        @"INSERT INTO fee_concessions
                            (school_id, academic_year_id, student_id, student_unique_id,
                             fee_type_id, bus_route_id, hostel_id, term_id, fee_period_id,
                             concession_type, amount, remarks,
                             receipt_no, status, created_by)
                          VALUES
                            (@schoolId, @AcademicYearId, @StudentId, @uniqueId,
                             @FeeTypeId, @BusRouteId, @HostelId, @TermId, @FeePeriodId,
                             @ConcessionType, @Amount, @Remarks,
                             @receiptNo, 'Active', @createdBy);
                          SELECT CAST(SCOPE_IDENTITY() AS INT)",
                        new
                        {
                            schoolId, r.AcademicYearId, r.StudentId, uniqueId,
                            r.FeeTypeId, r.BusRouteId, r.HostelId, r.TermId, r.FeePeriodId,
                            r.ConcessionType, r.Amount, r.Remarks,
                            receiptNo, createdBy
                        });
                }

                return conn.QueryFirst<FeeConcessionDto>(
                    @"SELECT fc.concession_id ConcessionId, fc.student_id StudentId,
                             s.student_name StudentName, s.admission_no AdmissionNo,
                             c.class_name ClassName,
                             fc.fee_type_id FeeTypeId, ft.fee_type_name FeeTypeName,
                             fc.bus_route_id BusRouteId, br.route_name RouteName,
                             fc.hostel_id HostelId, h.hostel_name HostelName,
                             fc.term_id TermId, t.term_name TermName,
                             fc.fee_period_id FeePeriodId, fp.period_label PeriodLabel,
                             fc.concession_type ConcessionType, fc.amount Amount,
                             fc.remarks Remarks, fc.receipt_no ReceiptNo,
                             fc.academic_year_id AcademicYearId, ay.academic_year AcademicYear,
                             fc.created_by CreatedBy, fc.created_at CreatedAt
                      FROM fee_concessions fc
                      INNER JOIN students       s  ON s.student_id        = fc.student_id
                      LEFT  JOIN classes        c  ON c.class_id          = s.class_id
                      LEFT  JOIN fee_types      ft ON ft.fee_type_id      = fc.fee_type_id
                      LEFT  JOIN bus_routes     br ON br.route_id         = fc.bus_route_id
                      LEFT  JOIN hostels        h  ON h.hostel_id         = fc.hostel_id
                      LEFT  JOIN terms          t  ON t.term_id           = fc.term_id
                      LEFT  JOIN fee_periods    fp ON fp.fee_period_id    = fc.fee_period_id
                      LEFT  JOIN academic_years ay ON ay.academic_year_id = fc.academic_year_id
                      WHERE fc.concession_id = @concessionId",
                    new { concessionId });
            }
        }

        // ── Bulk save ─────────────────────────────────────────────────────────

        public int BulkSaveConcessions(
            string tenantDbName, int schoolId, string createdBy, BulkSaveFeeConcessionRequest r)
        {
            int saved = 0;
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                // Pre-load academic year label once
                var academicYear = conn.QueryFirstOrDefault<string>(
                    "SELECT academic_year FROM academic_years WHERE academic_year_id = @id",
                    new { id = r.AcademicYearId });
                var yr8 = FormatYr8(academicYear);

                foreach (var row in r.Rows)
                {
                    var existingId = conn.QueryFirstOrDefault<int?>(
                        @"SELECT concession_id FROM fee_concessions
                          WHERE student_id    = @sid
                            AND school_id     = @schoolId
                            AND ISNULL(fee_type_id,  0) = ISNULL(@FeeTypeId,  0)
                            AND ISNULL(bus_route_id, 0) = ISNULL(@BusRouteId, 0)
                            AND ISNULL(hostel_id,    0) = ISNULL(@HostelId,   0)
                            AND ISNULL(term_id, 0)        = ISNULL(@TermId, 0)
                            AND ISNULL(fee_period_id, 0)  = ISNULL(@FeePeriodId, 0)
                            AND status = 'Active'",
                        new { sid = row.StudentId, r.FeeTypeId, r.BusRouteId, r.HostelId, schoolId, r.TermId, r.FeePeriodId });

                    if (existingId.HasValue)
                    {
                        conn.Execute(
                            @"UPDATE fee_concessions
                              SET concession_type = @ConcessionType, amount = @Amount,
                                  remarks = @Remarks, created_by = @createdBy, created_at = GETDATE()
                              WHERE concession_id = @existingId",
                            new { r.ConcessionType, row.Amount, row.Remarks, createdBy, existingId = existingId.Value });
                    }
                    else
                    {
                        var receiptNo = GenerateReceiptNoFromYr8(conn, schoolId, yr8);
                        var uniqueId  = conn.QueryFirstOrDefault<int?>(
                            "SELECT student_unique_id FROM students WHERE student_id = @sid",
                            new { sid = row.StudentId });

                        conn.Execute(
                            @"INSERT INTO fee_concessions
                                (school_id, academic_year_id, student_id, student_unique_id,
                                 fee_type_id, bus_route_id, hostel_id, term_id, fee_period_id,
                                 concession_type, amount, remarks,
                                 receipt_no, status, created_by)
                              VALUES
                                (@schoolId, @AcademicYearId, @sid, @uniqueId,
                                 @FeeTypeId, @BusRouteId, @HostelId, @TermId, @FeePeriodId,
                                 @ConcessionType, @Amount, @Remarks,
                                 @receiptNo, 'Active', @createdBy)",
                            new
                            {
                                schoolId, r.AcademicYearId, sid = row.StudentId, uniqueId,
                                r.FeeTypeId, r.BusRouteId, r.HostelId, r.TermId, r.FeePeriodId,
                                r.ConcessionType, row.Amount, row.Remarks,
                                receiptNo, createdBy
                            });
                    }

                    saved++;
                }
            }
            return saved;
        }

        // ── Delete (soft) ─────────────────────────────────────────────────────

        public bool DeleteConcession(string tenantDbName, int schoolId, int concessionId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var affected = conn.Execute(
                    @"UPDATE fee_concessions
                      SET status = 'Cancelled'
                      WHERE concession_id = @concessionId AND school_id = @schoolId
                        AND status = 'Active'",
                    new { concessionId, schoolId });
                return affected > 0;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string GenerateReceiptNo(IDbConnection conn, int schoolId, int academicYearId)
        {
            var ay  = conn.QueryFirstOrDefault<string>(
                "SELECT academic_year FROM academic_years WHERE academic_year_id = @academicYearId",
                new { academicYearId });
            var yr8 = FormatYr8(ay);
            return GenerateReceiptNoFromYr8(conn, schoolId, yr8);
        }

        private static string GenerateReceiptNoFromYr8(IDbConnection conn, int schoolId, string yr8)
        {
            var pattern     = $"CFR{yr8}%";
            var expectedLen = 3 + yr8.Length + 5;
            var maxSuffix   = conn.QuerySingle<int?>(
                @"SELECT MAX(TRY_CAST(RIGHT(receipt_no, 5) AS INT))
                  FROM fee_concessions
                  WHERE receipt_no LIKE @pattern AND school_id = @schoolId
                    AND LEN(receipt_no) = @expectedLen",
                new { pattern, schoolId, expectedLen }) ?? 0;
            return $"CFR{yr8}{(maxSuffix + 1):D5}";
        }

        private static string FormatYr8(string academicYear)
        {
            // "2026-2027" → "20262027"  |  "2026-27" → "202627"
            if (string.IsNullOrWhiteSpace(academicYear)) return DateTime.Now.Year.ToString();
            var parts = academicYear.Split('-');
            return parts.Length >= 2 ? parts[0].Trim() + parts[1].Trim()
                                     : academicYear.Replace("-", "");
        }
    }
}
