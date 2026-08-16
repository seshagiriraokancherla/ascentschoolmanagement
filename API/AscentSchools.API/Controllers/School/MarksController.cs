using AscentSchools.Core.DTOs.School.Marks;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System.Linq;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    [RoutePrefix("school/marks")]
    public class MarksController : BaseSchoolController
    {
        private readonly MarksRepository _repo;

        public MarksController()
        {
            _repo = new MarksRepository(new TenantConnectionFactory());
        }

        // ── Exam Types ────────────────────────────────────────────────────────

        [HttpGet, Route("exam-types")]
        public HttpResponseMessage GetExamTypes([FromUri] int? academicYearId = null)
            => Ok(_repo.GetExamTypes(Tenant.TenantDbName, Tenant.SchoolId, academicYearId));

        [HttpPost, Route("exam-types")]
        public HttpResponseMessage CreateExamType([FromBody] SaveExamTypeRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ExamTypeName))
                return BadRequest("Exam type name is required.");
            var id = _repo.CreateExamType(Tenant.TenantDbName, Tenant.SchoolId, Tenant.FullName, request);
            return Created(id, "Exam type created.");
        }

        [HttpPut, Route("exam-types/{id:int}")]
        public HttpResponseMessage UpdateExamType(int id, [FromBody] SaveExamTypeRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ExamTypeName))
                return BadRequest("Exam type name is required.");
            _repo.UpdateExamType(Tenant.TenantDbName, Tenant.SchoolId, id, request);
            return Ok(_repo.GetExamTypes(Tenant.TenantDbName, Tenant.SchoolId, request.AcademicYearId));
        }

        // ── Marks Grid ────────────────────────────────────────────────────────

        [HttpGet, Route("")]
        public HttpResponseMessage GetMarksGrid(
            [FromUri] int classId, [FromUri] int sectionId, [FromUri] int examTypeId, [FromUri] int academicYearId)
        {
            if (classId <= 0 || sectionId <= 0 || examTypeId <= 0 || academicYearId <= 0)
                return BadRequest("classId, sectionId, examTypeId and academicYearId are required.");
            var grid = _repo.GetMarksGrid(Tenant.TenantDbName, Tenant.SchoolId, classId, sectionId, examTypeId, academicYearId);
            return Ok(grid);
        }

        // ── Save Marks ────────────────────────────────────────────────────────

        [HttpPost, Route("")]
        public HttpResponseMessage SaveMarks([FromBody] SaveMarksRequest request)
        {
            if (request == null)
                return BadRequest("Request body is required.");
            if (request.ClassId <= 0 || request.ExamTypeId <= 0 || request.AcademicYearId <= 0)
                return BadRequest("classId, examTypeId and academicYearId are required.");
            if (request.Entries == null || !request.Entries.Any())
                return BadRequest("No mark entries provided.");

            _repo.SaveMarks(Tenant.TenantDbName, Tenant.SchoolId, Tenant.FullName, request);
            return Ok(true, "Marks saved.");
        }
    }
}
