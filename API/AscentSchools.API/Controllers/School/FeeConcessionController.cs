using AscentSchools.Core.DTOs.School.Fee;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    [RoutePrefix("school/fees/concessions")]
    public class FeeConcessionController : BaseSchoolController
    {
        private readonly FeeConcessionRepository _repo;

        public FeeConcessionController()
        {
            _repo = new FeeConcessionRepository(new TenantConnectionFactory());
        }

        // GET school/fees/concessions?academicYearId=&classId=&sectionId=
        [HttpGet, Route("")]
        public System.Net.Http.HttpResponseMessage GetConcessions(
            int? academicYearId = null, int? classId = null, int? sectionId = null)
        {
            var list = _repo.GetConcessions(
                Tenant.TenantDbName, Tenant.SchoolId, academicYearId, classId, sectionId);
            return Ok(list);
        }

        // POST school/fees/concessions  (individual save — upsert)
        [HttpPost, Route("")]
        public System.Net.Http.HttpResponseMessage SaveConcession([FromBody] SaveFeeConcessionRequest request)
        {
            if (request == null || request.StudentId <= 0)
                return BadRequest("Student is required.");
            if (request.FeeTypeId == null && request.BusRouteId == null && request.HostelId == null)
                return BadRequest("Fee type, bus route, or hostel is required.");
            if (request.AcademicYearId <= 0)
                return BadRequest("Academic year is required.");
            if (string.IsNullOrWhiteSpace(request.ConcessionType))
                return BadRequest("Concession type is required.");
            if (request.Amount <= 0)
                return BadRequest("Amount must be greater than zero.");

            var result = _repo.SaveConcession(
                Tenant.TenantDbName, Tenant.SchoolId, Tenant.FullName, request);
            return Ok(result, "Concession saved.");
        }

        // DELETE school/fees/concessions/{id}
        [HttpDelete, Route("{id:int}")]
        public System.Net.Http.HttpResponseMessage DeleteConcession(int id)
        {
            var deleted = _repo.DeleteConcession(Tenant.TenantDbName, Tenant.SchoolId, id);
            if (!deleted)
                return NotFound("Concession not found or already cancelled.");
            return Ok<object>(null, "Concession cancelled.");
        }

        // POST school/fees/concessions/bulk
        [HttpPost, Route("bulk")]
        public System.Net.Http.HttpResponseMessage BulkSaveConcessions(
            [FromBody] BulkSaveFeeConcessionRequest request)
        {
            if (request == null || request.Rows == null || request.Rows.Count == 0)
                return BadRequest("No rows provided.");
            if (request.FeeTypeId == null && request.BusRouteId == null && request.HostelId == null)
                return BadRequest("Fee type, bus route, or hostel is required.");
            if (request.AcademicYearId <= 0)
                return BadRequest("Academic year is required.");
            if (string.IsNullOrWhiteSpace(request.ConcessionType))
                return BadRequest("Concession type is required.");

            var saved = _repo.BulkSaveConcessions(
                Tenant.TenantDbName, Tenant.SchoolId, Tenant.FullName, request);
            return Ok(new { saved }, $"{saved} concession(s) saved.");
        }
    }
}
