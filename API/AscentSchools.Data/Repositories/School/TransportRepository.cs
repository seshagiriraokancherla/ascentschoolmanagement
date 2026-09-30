using AscentSchools.Core.DTOs.School.Transport;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace AscentSchools.Data.Repositories.School
{
    public class TransportRepository
    {
        private readonly IConnectionFactory _db;
        public TransportRepository(IConnectionFactory db) { _db = db; }

        // ── Buses ─────────────────────────────────────────────────────────────

        public IEnumerable<BusDto> GetBuses(string tenantDbName, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<BusDto>(
                    @"SELECT bus_id BusId, bus_name BusName, model Model, capacity Capacity,
                             registration_no RegistrationNo, description Description, status Status
                      FROM buses
                      WHERE school_id = @schoolId
                      ORDER BY bus_name",
                    new { schoolId });
        }

        public int CreateBus(string tenantDbName, int schoolId, string createdBy, SaveBusRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QuerySingle<int>(
                    @"INSERT INTO buses (bus_name, model, capacity, registration_no, description, status, school_id, created_by)
                      VALUES (@busName, @model, @capacity, @registrationNo, @description, @status, @schoolId, @createdBy);
                      SELECT SCOPE_IDENTITY();",
                    new { req.BusName, req.Model, req.Capacity, req.RegistrationNo, req.Description,
                          status = req.Status ?? "Active", schoolId, createdBy });
        }

        public void UpdateBus(string tenantDbName, int schoolId, int id, SaveBusRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE buses SET bus_name = @busName, model = @model, capacity = @capacity,
                        registration_no = @registrationNo, description = @description, status = @status
                      WHERE bus_id = @id AND school_id = @schoolId",
                    new { req.BusName, req.Model, req.Capacity, req.RegistrationNo, req.Description,
                          status = req.Status ?? "Active", id, schoolId });
        }

        // ── Routes ────────────────────────────────────────────────────────────

        public IEnumerable<BusRouteDto> GetRoutes(string tenantDbName, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<BusRouteDto>(
                    @"SELECT route_id RouteId, route_name RouteName, route_code RouteCode,
                             route_category RouteCategory, description Description, status Status
                      FROM bus_routes
                      WHERE school_id = @schoolId
                      ORDER BY route_name",
                    new { schoolId });
        }

        public int CreateRoute(string tenantDbName, int schoolId, string createdBy, SaveBusRouteRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QuerySingle<int>(
                    @"INSERT INTO bus_routes (route_name, route_code, route_category, description, status, school_id, created_by)
                      VALUES (@routeName, @routeCode, @routeCategory, @description, @status, @schoolId, @createdBy);
                      SELECT SCOPE_IDENTITY();",
                    new { req.RouteName, req.RouteCode, req.RouteCategory, req.Description,
                          status = req.Status ?? "Active", schoolId, createdBy });
        }

        public void UpdateRoute(string tenantDbName, int schoolId, int id, SaveBusRouteRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE bus_routes SET route_name = @routeName, route_code = @routeCode,
                        route_category = @routeCategory, description = @description, status = @status
                      WHERE route_id = @id AND school_id = @schoolId",
                    new { req.RouteName, req.RouteCode, req.RouteCategory, req.Description,
                          status = req.Status ?? "Active", id, schoolId });
        }

        // ── Bus Fee Structure ─────────────────────────────────────────────────

        // paymentType: "Term" or "Monthly". When null/blank, resolves to the saved
        // payment_type for this route+year (defaulting to 'Term'), so the UI can
        // auto-detect how a route was set up on first load.
        public BusFeeStructureDto GetBusFeeStructure(string tenantDbName, int schoolId, int routeId, int academicYearId, string paymentType)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var routeName = conn.QueryFirstOrDefault<string>(
                    "SELECT route_name FROM bus_routes WHERE route_id = @routeId AND school_id = @schoolId",
                    new { routeId, schoolId }) ?? "";

                var payType = string.IsNullOrWhiteSpace(paymentType)
                    ? (conn.QueryFirstOrDefault<string>(
                           "SELECT TOP 1 payment_type FROM bus_fee_structures WHERE route_id = @routeId AND academic_year_id = @academicYearId AND school_id = @schoolId",
                           new { routeId, academicYearId, schoolId }) ?? "Term")
                    : paymentType.Trim();
                if (payType != "Monthly") payType = "Term";

                IEnumerable<BusFeeTermDto> rows;
                if (payType == "Monthly")
                {
                    // One row per fee period. OUTER APPLY (not a plain LEFT JOIN) deliberately
                    // picks at most ONE matching bus_fee_structures row (the latest, by id) —
                    // a plain join would return one result row per MATCHING bfs row, so if
                    // stray/duplicate bfs rows exist for the same period+route+year (e.g. from
                    // a pre-fix save race), the period would appear multiple times in the grid.
                    rows = conn.Query<BusFeeTermDto>(
                        @"SELECT fp.fee_period_id FeePeriodId, fp.period_label PeriodLabel,
                                 fp.sequence_no SequenceNo, bfs.amount Amount
                          FROM fee_periods fp
                          OUTER APPLY (
                              SELECT TOP 1 b.amount
                              FROM bus_fee_structures b
                              WHERE b.fee_period_id    = fp.fee_period_id
                                AND b.route_id         = @routeId
                                AND b.academic_year_id = @academicYearId
                                AND b.school_id        = @schoolId
                              ORDER BY b.bus_fee_structure_id DESC
                          ) bfs
                          WHERE fp.academic_year_id = @academicYearId AND fp.school_id = @schoolId
                          ORDER BY ISNULL(fp.sequence_no, 9999), fp.period_label",
                        new { routeId, academicYearId, schoolId });
                }
                else
                {
                    // Same OUTER APPLY reasoning as above, keyed on term_id instead.
                    rows = conn.Query<BusFeeTermDto>(
                        @"SELECT t.term_id TermId, t.term_name TermName, t.order_no OrderNo,
                                 bfs.amount Amount
                          FROM terms t
                          OUTER APPLY (
                              SELECT TOP 1 b.amount
                              FROM bus_fee_structures b
                              WHERE b.term_id          = t.term_id
                                AND b.route_id         = @routeId
                                AND b.academic_year_id = @academicYearId
                                AND b.school_id        = @schoolId
                              ORDER BY b.bus_fee_structure_id DESC
                          ) bfs
                          WHERE t.academic_year_id = @academicYearId AND t.school_id = @schoolId
                          ORDER BY ISNULL(t.order_no, 9999), t.term_id",
                        new { routeId, academicYearId, schoolId });
                }

                return new BusFeeStructureDto
                {
                    RouteId        = routeId,
                    RouteName      = routeName,
                    AcademicYearId = academicYearId,
                    PaymentType    = payType,
                    Terms          = rows.ToList(),
                };
            }
        }

        public void SaveBusFeeStructure(string tenantDbName, int schoolId, string createdBy, SaveBusFeeStructureRequest req)
        {
            var payType = string.IsNullOrWhiteSpace(req.PaymentType) ? "Term" : req.PaymentType.Trim();
            if (payType != "Monthly") payType = "Term";

            // Dedupe by (termId, feePeriodId) — keep the last occurrence. Without this, a
            // caller sending the same term/period twice (e.g. a stale grid built from an
            // already-duplicated load) would insert two rows for it below.
            var items = (req.Items ?? new List<BusFeeTermEntry>())
                .Where(i => i.Amount > 0)
                .GroupBy(i => (i.TermId ?? 0, i.FeePeriodId ?? 0))
                .Select(g => g.Last())
                .ToList();

            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                if (conn.State != ConnectionState.Open) conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    // Delete + re-insert in ONE transaction so a concurrent save (e.g. a
                    // double-click) can't interleave its own delete/insert into this window
                    // and leave duplicate rows for the same route+term/period+year.
                    conn.Execute(
                        "DELETE FROM bus_fee_structures WHERE route_id = @routeId AND academic_year_id = @academicYearId AND school_id = @schoolId",
                        new { req.RouteId, req.AcademicYearId, schoolId }, tx);

                    foreach (var item in items)
                    {
                        conn.Execute(
                            @"INSERT INTO bus_fee_structures
                                (route_id, term_id, fee_period_id, payment_type, amount, academic_year_id, school_id, status, created_by)
                              VALUES
                                (@routeId, @termId, @feePeriodId, @payType, @amount, @academicYearId, @schoolId, 'Active', @createdBy)",
                            new { req.RouteId, item.TermId, item.FeePeriodId, payType, item.Amount, req.AcademicYearId, schoolId, createdBy }, tx);
                    }

                    tx.Commit();
                }
            }
        }

        // ── Student Transport ─────────────────────────────────────────────────

        public IEnumerable<StudentTransportDto> GetStudentTransport(
            string tenantDbName, int schoolId,
            int? routeId, int? academicYearId, int? classId, int? sectionId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<StudentTransportDto>(
                    @"SELECT s.student_id StudentId, s.student_unique_id StudentUniqueId,
                             s.academic_year_id AcademicYearId,
                             s.student_name StudentName, s.admission_no AdmissionNo,
                             c.class_name ClassName, s.transport_type TransportType,
                             s.bus_route_id BusRouteId, r.route_name RouteName,
                             s.bus_id BusId, b.bus_name BusName, s.bus_trip BusTrip
                      FROM students s
                      LEFT JOIN classes    c ON c.class_id   = s.class_id
                      LEFT JOIN bus_routes r ON r.route_id   = s.bus_route_id
                      LEFT JOIN buses      b ON b.bus_id     = s.bus_id
                      WHERE s.school_id = @schoolId
                        AND s.status IN ('Active', 'Y')
                        AND (@routeId      IS NULL OR s.bus_route_id    = @routeId)
                        AND (@academicYearId IS NULL OR s.academic_year_id = @academicYearId)
                        AND (@classId      IS NULL OR s.class_id        = @classId)
                        AND (@sectionId    IS NULL OR s.section_id      = @sectionId)
                      ORDER BY c.class_name, s.student_name",
                    new { schoolId, routeId, academicYearId, classId, sectionId });
        }

        public void UpdateStudentTransport(string tenantDbName, int schoolId, int studentUniqueId, UpdateStudentTransportRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE students
                      SET transport_type = @transportType,
                          bus_route_id   = @busRouteId,
                          bus_id         = @busId,
                          bus_trip       = @busTrip
                      WHERE student_unique_id = @studentUniqueId
                        AND academic_year_id  = @academicYearId
                        AND school_id         = @schoolId",
                    new { req.TransportType, req.BusRouteId, req.BusId, req.BusTrip,
                          studentUniqueId, academicYearId = req.AcademicYearId, schoolId });
        }

    }
}
