using AscentSchools.Core.DTOs.School.Fee;
using AscentSchools.Core.DTOs.School.Students;
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

        // ── Legacy import (sync tool — SAS_ConcessionDet) ────────────────────
        // Each row is one legacy key already summed by the sync tool, so the import
        // REPLACES the new-app amount rather than adding to it — re-running a year is
        // idempotent. Amount 0 (every legacy row deleted) cancels the active concession.
        // Transport rows carry the legacy route name(s); the student's own assigned
        // route wins when it is one of them (outstanding only uses that route).
        public BulkImportResult ImportLegacyConcessions(
            string tenantDbName, int schoolId, string createdBy, List<LegacyConcessionRow> rows)
        {
            var result = new BulkImportResult { Total = rows.Count };

            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var years = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var y in conn.Query<ConcessionLookup>(
                    "SELECT academic_year_id Id, academic_year Name FROM academic_years WHERE school_id = @schoolId",
                    new { schoolId }))
                    if (!string.IsNullOrWhiteSpace(y.Name) && !years.ContainsKey(y.Name.Trim())) years[y.Name.Trim()] = y.Id;

                // Fee types are per academic year: prefer the same year, else any year (latest first).
                var feeTypes = conn.Query<ConcessionLookup>(
                    @"SELECT fee_type_id Id, fee_type_name Name, academic_year_id YearId FROM fee_types
                      WHERE school_id = @schoolId ORDER BY academic_year_id DESC, fee_type_id", new { schoolId }).AsList();
                var feeTypeByYear = ByYear(feeTypes);
                var feeTypeAny    = ByName(feeTypes);

                var termByYear   = ByYear(conn.Query<ConcessionLookup>(
                    "SELECT term_id Id, term_name Name, academic_year_id YearId FROM terms WHERE school_id = @schoolId",
                    new { schoolId }));
                var periodByYear = ByYear(conn.Query<ConcessionLookup>(
                    "SELECT fee_period_id Id, period_label Name, academic_year_id YearId FROM fee_periods WHERE school_id = @schoolId",
                    new { schoolId }));

                // Active routes win on a duplicate name.
                var routes = conn.Query<ConcessionLookup>(
                    @"SELECT route_id Id, route_name Name FROM bus_routes WHERE school_id = @schoolId
                      ORDER BY CASE WHEN status IN ('Active','Y') THEN 0 ELSE 1 END, route_id", new { schoolId }).AsList();
                var routeByName = ByName(routes);
                var routeName   = routes.ToDictionary(r => r.Id, r => (r.Name ?? "").Trim());

                foreach (var r in rows)
                {
                    var id = $"{r.AdmissionNo} / {r.AcademicYear} / {(string.IsNullOrWhiteSpace(r.RouteNames) ? r.FeeTypeName : "Bus: " + r.RouteNames)} / {r.TermName}";
                    void Fail(string reason)
                    {
                        result.Failed++;
                        result.Errors.Add(new BulkRowError { Row = r.RowNumber, AdmissionNo = r.AdmissionNo, Identifier = id, Reason = reason });
                    }

                    try
                    {
                        if (!years.TryGetValue((r.AcademicYear ?? "").Trim(), out int yearId))
                        { Fail($"Academic year not found: {r.AcademicYear}"); continue; }

                        var student = conn.QueryFirstOrDefault<ConcessionStudent>(
                            @"SELECT TOP 1 student_id StudentId, student_unique_id StudentUniqueId, bus_route_id BusRouteId
                              FROM students
                              WHERE admission_no = @adm AND academic_year_id = @yearId AND school_id = @schoolId
                              ORDER BY student_id DESC",
                            new { adm = (r.AdmissionNo ?? "").Trim(), yearId, schoolId });
                        if (student == null)
                        { Fail("Student not found for this academic year"); continue; }

                        int? feeTypeId = null, busRouteId = null, termId = null, feePeriodId = null;

                        if (!string.IsNullOrWhiteSpace(r.RouteNames))
                        {
                            var names = r.RouteNames.Split('|').Select(n => n.Trim()).Where(n => n.Length > 0).ToList();
                            if (student.BusRouteId.HasValue
                                && routeName.TryGetValue(student.BusRouteId.Value, out var assigned)
                                && names.Any(n => string.Equals(n, assigned, StringComparison.OrdinalIgnoreCase)))
                                busRouteId = student.BusRouteId;
                            else
                                foreach (var n in names)
                                    if (routeByName.TryGetValue(n, out int rid)) { busRouteId = rid; break; }
                            if (busRouteId == null) { Fail($"Bus route not found: {r.RouteNames}"); continue; }
                        }
                        else
                        {
                            var fn = (r.FeeTypeName ?? "").Trim();
                            if (feeTypeByYear.TryGetValue($"{fn}|{yearId}", out int ft)) feeTypeId = ft;
                            else if (feeTypeAny.TryGetValue(fn, out int ft2)) feeTypeId = ft2;
                            else { Fail($"Fee type not found: {r.FeeTypeName}"); continue; }
                        }

                        var tn = (r.TermName ?? "").Trim();
                        if (termByYear.TryGetValue($"{tn}|{yearId}", out int tid)) termId = tid;
                        else if (periodByYear.TryGetValue($"{tn}|{yearId}", out int pid)) feePeriodId = pid;
                        else { Fail($"Term / month not found for this year: {r.TermName}"); continue; }

                        var existing = conn.QueryFirstOrDefault<ConcessionExisting>(
                            @"SELECT TOP 1 concession_id ConcessionId, amount Amount, concession_type ConcessionType
                              FROM fee_concessions
                              WHERE student_id = @sid AND school_id = @schoolId AND status = 'Active'
                                AND ISNULL(fee_type_id,   0) = ISNULL(@feeTypeId,   0)
                                AND ISNULL(bus_route_id,  0) = ISNULL(@busRouteId,  0)
                                AND hostel_id IS NULL
                                AND ISNULL(term_id,       0) = ISNULL(@termId,      0)
                                AND ISNULL(fee_period_id, 0) = ISNULL(@feePeriodId, 0)",
                            new { sid = student.StudentId, schoolId, feeTypeId, busRouteId, termId, feePeriodId });

                        var type    = Trunc(string.IsNullOrWhiteSpace(r.ConcessionType) ? "Concession" : r.ConcessionType.Trim(), 30);
                        var remarks = Trunc(r.Remarks, 500);

                        if (r.Amount <= 0)
                        {
                            // Every legacy row for this key is deleted → cancel ours too.
                            if (existing == null) { result.Skipped++; continue; }
                            conn.Execute("UPDATE fee_concessions SET status = 'Cancelled' WHERE concession_id = @id",
                                new { id = existing.ConcessionId });
                            result.Updated++;
                            continue;
                        }

                        if (existing != null)
                        {
                            if (existing.Amount == r.Amount && existing.ConcessionType == type)
                            { result.Skipped++; continue; }

                            conn.Execute(
                                $@"UPDATE fee_concessions
                                   SET amount = @Amount, concession_type = @type, remarks = @remarks,
                                       created_by = @createdBy, created_at = {IstNow}
                                   WHERE concession_id = @id",
                                new { r.Amount, type, remarks, createdBy, id = existing.ConcessionId });
                            result.Updated++;
                            continue;
                        }

                        // Keep the legacy receipt number when it fits and isn't taken; it is
                        // shorter than the generated CFR{yr8}{D5}, so it never collides with
                        // the generator's MAX(suffix) scan (that filters on exact length).
                        var receiptNo = (r.LegacyNo ?? "").Trim();
                        if (receiptNo.Length == 0 || receiptNo.Length > 25
                            || conn.ExecuteScalar<int>(
                                "SELECT COUNT(1) FROM fee_concessions WHERE receipt_no = @receiptNo AND school_id = @schoolId",
                                new { receiptNo, schoolId }) > 0)
                            receiptNo = GenerateReceiptNo(conn, schoolId, yearId);

                        conn.Execute(
                            $@"INSERT INTO fee_concessions
                                (school_id, academic_year_id, student_id, student_unique_id,
                                 fee_type_id, bus_route_id, hostel_id, term_id, fee_period_id,
                                 concession_type, amount, remarks, receipt_no, status, created_by, created_at)
                               VALUES
                                (@schoolId, @yearId, @sid, @uid,
                                 @feeTypeId, @busRouteId, NULL, @termId, @feePeriodId,
                                 @type, @Amount, @remarks, @receiptNo, 'Active', @createdBy, {IstNow})",
                            new
                            {
                                schoolId, yearId, sid = student.StudentId, uid = student.StudentUniqueId,
                                feeTypeId, busRouteId, termId, feePeriodId,
                                type, r.Amount, remarks, receiptNo, createdBy
                            });
                        result.Imported++;
                    }
                    catch (Exception ex)
                    {
                        Fail(ex.Message);
                    }
                }
            }
            return result;
        }

        private const string IstNow = "CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'India Standard Time' AS DATETIME)";

        private static string Trunc(string s, int max)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim();
            return s.Length > max ? s.Substring(0, max) : s;
        }

        private static Dictionary<string, int> ByName(IEnumerable<ConcessionLookup> src)
        {
            var d = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var i in src)
                if (!string.IsNullOrWhiteSpace(i.Name) && !d.ContainsKey(i.Name.Trim())) d[i.Name.Trim()] = i.Id;
            return d;
        }

        private static Dictionary<string, int> ByYear(IEnumerable<ConcessionLookup> src)
        {
            var d = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var i in src)
            {
                if (string.IsNullOrWhiteSpace(i.Name)) continue;
                var key = $"{i.Name.Trim()}|{i.YearId ?? 0}";
                if (!d.ContainsKey(key)) d[key] = i.Id;
            }
            return d;
        }

        private class ConcessionLookup
        {
            public int    Id     { get; set; }
            public string Name   { get; set; }
            public int?   YearId { get; set; }
        }

        private class ConcessionStudent
        {
            public long StudentId       { get; set; }
            public int? StudentUniqueId { get; set; }
            public int? BusRouteId      { get; set; }
        }

        private class ConcessionExisting
        {
            public int     ConcessionId   { get; set; }
            public decimal Amount         { get; set; }
            public string  ConcessionType { get; set; }
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
