using AscentSchools.Core.DTOs.School.Transport;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System.Linq;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    [RoutePrefix("school/transport")]
    public class TransportController : BaseSchoolController
    {
        private readonly TransportRepository _repo;

        public TransportController()
        {
            _repo = new TransportRepository(new TenantConnectionFactory());
        }

        // ── Buses ─────────────────────────────────────────────────────────────

        [HttpGet, Route("buses")]
        public HttpResponseMessage GetBuses()
            => Ok(_repo.GetBuses(Tenant.TenantDbName, Tenant.SchoolId));

        [HttpPost, Route("buses")]
        public HttpResponseMessage CreateBus([FromBody] SaveBusRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.BusName))
                return BadRequest("Bus name is required.");
            var id = _repo.CreateBus(Tenant.TenantDbName, Tenant.SchoolId, Tenant.FullName, request);
            return Created(id, "Bus created.");
        }

        [HttpPut, Route("buses/{id:int}")]
        public HttpResponseMessage UpdateBus(int id, [FromBody] SaveBusRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.BusName))
                return BadRequest("Bus name is required.");
            _repo.UpdateBus(Tenant.TenantDbName, Tenant.SchoolId, id, request);
            return Ok(_repo.GetBuses(Tenant.TenantDbName, Tenant.SchoolId));
        }

        // ── Routes ────────────────────────────────────────────────────────────

        [HttpGet, Route("routes")]
        public HttpResponseMessage GetRoutes()
            => Ok(_repo.GetRoutes(Tenant.TenantDbName, Tenant.SchoolId));

        [HttpPost, Route("routes")]
        public HttpResponseMessage CreateRoute([FromBody] SaveBusRouteRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RouteName))
                return BadRequest("Route name is required.");
            var id = _repo.CreateRoute(Tenant.TenantDbName, Tenant.SchoolId, Tenant.FullName, request);
            return Created(id, "Route created.");
        }

        [HttpPut, Route("routes/{id:int}")]
        public HttpResponseMessage UpdateRoute(int id, [FromBody] SaveBusRouteRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RouteName))
                return BadRequest("Route name is required.");
            _repo.UpdateRoute(Tenant.TenantDbName, Tenant.SchoolId, id, request);
            return Ok(_repo.GetRoutes(Tenant.TenantDbName, Tenant.SchoolId));
        }

        // ── Bus Fee Structure ─────────────────────────────────────────────────

        [HttpGet, Route("fee-structure")]
        public HttpResponseMessage GetFeeStructure([FromUri] int routeId, [FromUri] int academicYearId)
        {
            if (routeId <= 0 || academicYearId <= 0)
                return BadRequest("routeId and academicYearId are required.");
            return Ok(_repo.GetBusFeeStructure(Tenant.TenantDbName, Tenant.SchoolId, routeId, academicYearId));
        }

        [HttpPost, Route("fee-structure")]
        public HttpResponseMessage SaveFeeStructure([FromBody] SaveBusFeeStructureRequest request)
        {
            if (request == null || request.RouteId <= 0 || request.AcademicYearId <= 0)
                return BadRequest("routeId and academicYearId are required.");
            if (request.Items == null || !request.Items.Any())
                return BadRequest("No fee items provided.");
            _repo.SaveBusFeeStructure(Tenant.TenantDbName, Tenant.SchoolId, Tenant.FullName, request);
            return Ok(_repo.GetBusFeeStructure(Tenant.TenantDbName, Tenant.SchoolId, request.RouteId, request.AcademicYearId));
        }

        // ── Student Transport ─────────────────────────────────────────────────

        [HttpGet, Route("students")]
        public HttpResponseMessage GetStudentTransport([FromUri] int? routeId = null)
            => Ok(_repo.GetStudentTransport(Tenant.TenantDbName, Tenant.SchoolId, routeId));

        [HttpPut, Route("students/{studentId:long}")]
        public HttpResponseMessage UpdateStudentTransport(long studentId, [FromBody] UpdateStudentTransportRequest request)
        {
            if (request == null)
                return BadRequest("Request body is required.");
            _repo.UpdateStudentTransport(Tenant.TenantDbName, Tenant.SchoolId, studentId, request);
            return Ok(true, "Student transport updated.");
        }
    }
}
