using AscentSchools.Core.DTOs.School.Hostel;
using AscentSchools.Core.DTOs.School.Students;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System.Collections.Generic;
using System.Linq;

namespace AscentSchools.Data.Repositories.School
{
    public class HostelRepository
    {
        private readonly IConnectionFactory _db;
        public HostelRepository(IConnectionFactory db) { _db = db; }

        // ── Hostels CRUD ─────────────────────────────────────────────────────

        public IEnumerable<HostelDto> GetHostels(string tenantDbName, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<HostelDto>(
                    @"SELECT hostel_id HostelId, hostel_name HostelName, description Description,
                             capacity Capacity, contact_no ContactNo, address Address,
                             no_of_rooms NoOfRooms, school_id SchoolId,
                             created_by CreatedBy, created_at CreatedAt
                      FROM hostels
                      WHERE school_id = @schoolId
                      ORDER BY hostel_name",
                    new { schoolId });
        }

        public HostelDto CreateHostel(string tenantDbName, int schoolId, string createdBy, SaveHostelRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var id = conn.QuerySingle<int>(
                    @"INSERT INTO hostels (hostel_name, description, capacity, contact_no, address, no_of_rooms, school_id, created_by)
                      VALUES (@HostelName, @Description, @Capacity, @ContactNo, @Address, @NoOfRooms, @schoolId, @createdBy);
                      SELECT CAST(SCOPE_IDENTITY() AS INT)",
                    new { r.HostelName, r.Description, r.Capacity, r.ContactNo, r.Address, r.NoOfRooms, schoolId, createdBy });

                return conn.QueryFirst<HostelDto>(
                    @"SELECT hostel_id HostelId, hostel_name HostelName, description Description,
                             capacity Capacity, contact_no ContactNo, address Address,
                             no_of_rooms NoOfRooms, school_id SchoolId,
                             created_by CreatedBy, created_at CreatedAt
                      FROM hostels WHERE hostel_id = @id",
                    new { id });
            }
        }

        public HostelDto UpdateHostel(string tenantDbName, int schoolId, int hostelId, SaveHostelRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                conn.Execute(
                    @"UPDATE hostels SET hostel_name = @HostelName, description = @Description,
                        capacity = @Capacity, contact_no = @ContactNo, address = @Address,
                        no_of_rooms = @NoOfRooms
                      WHERE hostel_id = @hostelId AND school_id = @schoolId",
                    new { r.HostelName, r.Description, r.Capacity, r.ContactNo, r.Address, r.NoOfRooms, hostelId, schoolId });

                return conn.QueryFirst<HostelDto>(
                    @"SELECT hostel_id HostelId, hostel_name HostelName, description Description,
                             capacity Capacity, contact_no ContactNo, address Address,
                             no_of_rooms NoOfRooms, school_id SchoolId,
                             created_by CreatedBy, created_at CreatedAt
                      FROM hostels WHERE hostel_id = @hostelId",
                    new { hostelId });
            }
        }

        // ── Fee Structure ─────────────────────────────────────────────────────

        public IEnumerable<HostelFeeStructureDto> GetFeeStructure(
            string tenantDbName, int schoolId, int hostelId, int academicYearId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<HostelFeeStructureDto>(
                    @"SELECT hfs.hostel_fee_structure_id HostelFeeStructureId,
                             hfs.hostel_id HostelId, h.hostel_name HostelName,
                             hfs.academic_year_id AcademicYearId, ay.academic_year AcademicYear,
                             hfs.term_id TermId, t.term_name TermName, t.order_no OrderNo,
                             hfs.fee_period_id FeePeriodId, fp.period_label PeriodLabel,
                             fp.sequence_no PeriodSequenceNo,
                             hfs.payment_type PaymentType,
                             hfs.amount Amount
                      FROM hostel_fee_structures hfs
                      INNER JOIN hostels       h  ON h.hostel_id         = hfs.hostel_id
                      LEFT  JOIN terms         t  ON t.term_id           = hfs.term_id
                      LEFT  JOIN fee_periods   fp ON fp.fee_period_id    = hfs.fee_period_id
                      LEFT  JOIN academic_years ay ON ay.academic_year_id = hfs.academic_year_id
                      WHERE hfs.hostel_id = @hostelId AND hfs.academic_year_id = @academicYearId
                        AND hfs.school_id = @schoolId
                      ORDER BY ISNULL(t.order_no, 9999), t.term_name,
                               ISNULL(fp.sequence_no, 9999), fp.period_label",
                    new { hostelId, academicYearId, schoolId });
        }

        public void SaveFeeStructure(string tenantDbName, int schoolId, SaveHostelFeeStructureRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    conn.Execute(
                        @"DELETE FROM hostel_fee_structures
                          WHERE hostel_id = @HostelId AND academic_year_id = @AcademicYearId
                            AND school_id = @schoolId",
                        new { r.HostelId, r.AcademicYearId, schoolId }, tx);

                    if (r.Items != null && r.Items.Count > 0)
                        conn.Execute(
                            @"INSERT INTO hostel_fee_structures
                                (hostel_id, academic_year_id, term_id, fee_period_id, payment_type, amount, school_id)
                              VALUES
                                (@HostelId, @AcademicYearId, @TermId, @FeePeriodId, @PaymentType, @Amount, @schoolId)",
                            r.Items.Select(i => new
                            {
                                r.HostelId, r.AcademicYearId,
                                i.TermId, i.FeePeriodId, r.PaymentType,
                                i.Amount, schoolId
                            }), tx);

                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }

        // ── Bulk Fee Structure Import ─────────────────────────────────────────

        public BulkImportResult BulkSaveFeeStructure(
            string tenantDbName, int schoolId, BulkHostelFeeStructureRequest request)
        {
            var result = new BulkImportResult { Total = request.Rows.Count };

            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var hostels     = conn.Query<HostelBulkLookup>("SELECT hostel_id AS Id, hostel_name AS Name FROM hostels WHERE school_id = @schoolId", new { schoolId });
                var years       = conn.Query<HostelBulkLookup>("SELECT academic_year_id AS Id, academic_year AS Name FROM academic_years WHERE school_id = @schoolId", new { schoolId });
                var terms       = conn.Query<HostelBulkLookup>("SELECT term_id AS Id, term_name AS Name FROM terms WHERE school_id = @schoolId", new { schoolId });
                var feePeriods  = conn.Query<HostelBulkLookup>("SELECT fee_period_id AS Id, period_label AS Name FROM fee_periods WHERE school_id = @schoolId", new { schoolId });

                var hostelMap    = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
                var yearMap      = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
                var termMap      = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
                var periodMap    = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);

                foreach (var h in hostels)    hostelMap[h.Name]  = h.Id;
                foreach (var y in years)      yearMap[y.Name]    = y.Id;
                foreach (var t in terms)      termMap[t.Name]    = t.Id;
                foreach (var p in feePeriods) periodMap[p.Name]  = p.Id;

                foreach (var row in request.Rows)
                {
                    var payType = (row.PaymentType ?? "").Trim();
                    var identifier = $"{row.HostelName} / {row.AcademicYear} / {(payType == "Monthly" ? row.PeriodLabel : row.TermName)}";

                    if (string.IsNullOrWhiteSpace(row.HostelName) || string.IsNullOrWhiteSpace(row.AcademicYear) ||
                        string.IsNullOrWhiteSpace(payType)        || string.IsNullOrWhiteSpace(row.Amount))
                    { AddError(result, row, identifier, "HostelName, AcademicYear, PaymentType and Amount are required."); continue; }

                    if (payType != "Term" && payType != "Monthly")
                    { AddError(result, row, identifier, $"PaymentType must be 'Term' or 'Monthly'"); continue; }

                    if (!hostelMap.TryGetValue(row.HostelName.Trim(), out var hostelId))
                    { AddError(result, row, identifier, $"Hostel '{row.HostelName}' not found"); continue; }

                    if (!yearMap.TryGetValue(row.AcademicYear.Trim(), out var yearId))
                    { AddError(result, row, identifier, $"Academic year '{row.AcademicYear}' not found"); continue; }

                    if (!decimal.TryParse(row.Amount.Trim(), out var amount) || amount < 0)
                    { AddError(result, row, identifier, $"Invalid amount '{row.Amount}'"); continue; }

                    int? termId = null; int? periodId = null;
                    if (payType == "Term")
                    {
                        if (string.IsNullOrWhiteSpace(row.TermName))
                        { AddError(result, row, identifier, "TermName is required when PaymentType is 'Term'"); continue; }
                        if (!termMap.TryGetValue(row.TermName.Trim(), out var tid))
                        { AddError(result, row, identifier, $"Term '{row.TermName}' not found"); continue; }
                        termId = tid;
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(row.PeriodLabel))
                        { AddError(result, row, identifier, "PeriodLabel is required when PaymentType is 'Monthly'"); continue; }
                        if (!periodMap.TryGetValue(row.PeriodLabel.Trim(), out var pid))
                        { AddError(result, row, identifier, $"Fee period '{row.PeriodLabel}' not found"); continue; }
                        periodId = pid;
                    }

                    conn.Execute(
                        @"DELETE FROM hostel_fee_structures
                          WHERE hostel_id = @hostelId AND academic_year_id = @yearId
                            AND ISNULL(term_id, 0) = ISNULL(@termId, 0)
                            AND ISNULL(fee_period_id, 0) = ISNULL(@periodId, 0)
                            AND school_id = @schoolId",
                        new { hostelId, yearId, termId, periodId, schoolId });

                    conn.Execute(
                        @"INSERT INTO hostel_fee_structures
                            (hostel_id, academic_year_id, term_id, fee_period_id, payment_type, amount, school_id)
                          VALUES
                            (@hostelId, @yearId, @termId, @periodId, @payType, @amount, @schoolId)",
                        new { hostelId, yearId, termId, periodId, payType, amount, schoolId });

                    result.Imported++;
                }
            }

            result.Failed = result.Total - result.Imported;
            return result;
        }

        // ── Student Assignment ────────────────────────────────────────────────

        public IEnumerable<StudentHostelDto> GetStudentHostels(
            string tenantDbName, int schoolId, int? academicYearId,
            int? classId, int? sectionId, int? hostelId)
        {
            var where = "s.school_id = @schoolId AND s.status IN ('Active', 'Y')";
            if (academicYearId.HasValue) where += " AND s.academic_year_id = @academicYearId";
            if (classId.HasValue)        where += " AND s.class_id = @classId";
            if (sectionId.HasValue)      where += " AND s.section_id = @sectionId";
            if (hostelId.HasValue)       where += " AND s.hostel_id = @hostelId";

            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<StudentHostelDto>(
                    $@"SELECT s.student_id StudentId, s.student_unique_id StudentUniqueId,
                              s.student_name StudentName, s.admission_no AdmissionNo,
                              c.class_name ClassName, sec.section_name SectionName,
                              s.academic_year_id AcademicYearId, ay.academic_year AcademicYear,
                              s.hostel_id HostelId, h.hostel_name HostelName
                       FROM students s
                       LEFT JOIN classes       c   ON c.class_id         = s.class_id
                       LEFT JOIN sections      sec ON sec.section_id     = s.section_id
                       LEFT JOIN academic_years ay  ON ay.academic_year_id = s.academic_year_id
                       LEFT JOIN hostels       h   ON h.hostel_id        = s.hostel_id
                       WHERE {where}
                       ORDER BY s.student_name",
                    new { schoolId, academicYearId, classId, sectionId, hostelId });
        }

        public void UpdateStudentHostel(
            string tenantDbName, int schoolId, int studentUniqueId, UpdateStudentHostelRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE students SET hostel_id = @HostelId
                      WHERE student_unique_id = @studentUniqueId
                        AND academic_year_id  = @AcademicYearId
                        AND school_id         = @schoolId",
                    new { r.HostelId, studentUniqueId, r.AcademicYearId, schoolId });
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void AddError(BulkImportResult result, BulkHostelFeeStructureRow row, string identifier, string reason)
        {
            result.Errors.Add(new BulkRowError { Row = row.RowNumber, Identifier = identifier, Reason = reason });
        }
    }

    internal class HostelBulkLookup
    {
        public int    Id   { get; set; }
        public string Name { get; set; }
    }
}
