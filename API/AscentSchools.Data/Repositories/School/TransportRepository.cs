using AscentSchools.Core.DTOs.School.Transport;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System.Collections.Generic;
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

        public BusFeeStructureDto GetBusFeeStructure(string tenantDbName, int schoolId, int routeId, int academicYearId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var routeName = conn.QueryFirstOrDefault<string>(
                    "SELECT route_name FROM bus_routes WHERE route_id = @routeId AND school_id = @schoolId",
                    new { routeId, schoolId }) ?? "";

                var terms = conn.Query<TermRow>(
                    @"SELECT t.term_id TermId, t.term_name TermName,
                             bfs.amount Amount
                      FROM terms t
                      LEFT JOIN bus_fee_structures bfs
                             ON bfs.term_id          = t.term_id
                            AND bfs.route_id         = @routeId
                            AND bfs.academic_year_id = @academicYearId
                            AND bfs.school_id        = @schoolId
                      WHERE t.academic_year_id = @academicYearId AND t.school_id = @schoolId
                      ORDER BY t.term_id",
                    new { routeId, academicYearId, schoolId });

                return new BusFeeStructureDto
                {
                    RouteId        = routeId,
                    RouteName      = routeName,
                    AcademicYearId = academicYearId,
                    Terms          = terms.Select(t => new BusFeeTermDto
                    {
                        TermId   = t.TermId,
                        TermName = t.TermName,
                        Amount   = t.Amount,
                    }),
                };
            }
        }

        public void SaveBusFeeStructure(string tenantDbName, int schoolId, string createdBy, SaveBusFeeStructureRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                // Delete existing entries for this route + academic year, then re-insert
                conn.Execute(
                    "DELETE FROM bus_fee_structures WHERE route_id = @routeId AND academic_year_id = @academicYearId AND school_id = @schoolId",
                    new { req.RouteId, req.AcademicYearId, schoolId });

                foreach (var item in req.Items)
                {
                    if (item.Amount > 0)
                        conn.Execute(
                            @"INSERT INTO bus_fee_structures (route_id, term_id, amount, academic_year_id, school_id, status, created_by)
                              VALUES (@routeId, @termId, @amount, @academicYearId, @schoolId, 'Active', @createdBy)",
                            new { req.RouteId, item.TermId, item.Amount, req.AcademicYearId, schoolId, createdBy });
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

        // ── Internal row types ────────────────────────────────────────────────

        private class TermRow
        {
            public int      TermId   { get; set; }
            public string   TermName { get; set; }
            public decimal? Amount   { get; set; }
        }
    }
}
