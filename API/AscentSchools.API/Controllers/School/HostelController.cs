using AscentSchools.Core.DTOs.School.Hostel;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    [RoutePrefix("school/hostel")]
    public class HostelController : BaseSchoolController
    {
        private readonly HostelRepository _repo;

        public HostelController()
        {
            _repo = new HostelRepository(new TenantConnectionFactory());
        }

        // GET school/hostel
        [HttpGet, Route("")]
        public System.Net.Http.HttpResponseMessage GetHostels()
        {
            var list = _repo.GetHostels(Tenant.TenantDbName, Tenant.SchoolId);
            return Ok(list);
        }

        // POST school/hostel
        [HttpPost, Route("")]
        public System.Net.Http.HttpResponseMessage CreateHostel([FromBody] SaveHostelRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.HostelName))
                return BadRequest("Hostel name is required.");

            var hostel = _repo.CreateHostel(Tenant.TenantDbName, Tenant.SchoolId, Tenant.FullName, request);
            return Created(hostel, "Hostel created.");
        }

        // PUT school/hostel/{id}
        [HttpPut, Route("{id:int}")]
        public System.Net.Http.HttpResponseMessage UpdateHostel(int id, [FromBody] SaveHostelRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.HostelName))
                return BadRequest("Hostel name is required.");

            var hostel = _repo.UpdateHostel(Tenant.TenantDbName, Tenant.SchoolId, id, request);
            return Ok(hostel, "Hostel updated.");
        }

        // GET school/hostel/fee-structure?hostelId=&academicYearId=
        [HttpGet, Route("fee-structure")]
        public System.Net.Http.HttpResponseMessage GetFeeStructure(int hostelId, int academicYearId)
        {
            var list = _repo.GetFeeStructure(Tenant.TenantDbName, Tenant.SchoolId, hostelId, academicYearId);
            return Ok(list);
        }

        // POST school/hostel/fee-structure
        [HttpPost, Route("fee-structure")]
        public System.Net.Http.HttpResponseMessage SaveFeeStructure([FromBody] SaveHostelFeeStructureRequest request)
        {
            if (request == null || request.HostelId <= 0 || request.AcademicYearId <= 0)
                return BadRequest("Hostel and academic year are required.");

            _repo.SaveFeeStructure(Tenant.TenantDbName, Tenant.SchoolId, request);
            return Ok<object>(null, "Fee structure saved.");
        }

        // POST school/hostel/fee-structure/bulk
        [HttpPost, Route("fee-structure/bulk")]
        public System.Net.Http.HttpResponseMessage BulkSaveFeeStructure([FromBody] BulkHostelFeeStructureRequest request)
        {
            if (request == null || request.Rows == null || request.Rows.Count == 0)
                return BadRequest("No rows provided.");

            var result = _repo.BulkSaveFeeStructure(Tenant.TenantDbName, Tenant.SchoolId, request);
            return Ok(result, $"{result.Imported} of {result.Total} rows imported.");
        }

        // GET school/hostel/students?academicYearId=&classId=&sectionId=&hostelId=
        [HttpGet, Route("students")]
        public System.Net.Http.HttpResponseMessage GetStudents(
            int? academicYearId = null, int? classId = null, int? sectionId = null, int? hostelId = null)
        {
            var list = _repo.GetStudentHostels(
                Tenant.TenantDbName, Tenant.SchoolId, academicYearId, classId, sectionId, hostelId);
            return Ok(list);
        }

        // PUT school/hostel/students/{studentUniqueId}
        [HttpPut, Route("students/{studentUniqueId:int}")]
        public System.Net.Http.HttpResponseMessage UpdateStudentHostel(
            int studentUniqueId, [FromBody] UpdateStudentHostelRequest request)
        {
            if (request == null || request.AcademicYearId <= 0)
                return BadRequest("Academic year is required.");

            _repo.UpdateStudentHostel(Tenant.TenantDbName, Tenant.SchoolId, studentUniqueId, request);
            return Ok<object>(null, "Student hostel assignment updated.");
        }
    }
}
