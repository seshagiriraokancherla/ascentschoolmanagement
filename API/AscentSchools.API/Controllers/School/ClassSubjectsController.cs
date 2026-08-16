using AscentSchools.Core.DTOs.School.ClassSubjects;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    /// <summary>
    /// Class -> subject mapping (Master Data → Class Subjects).
    /// Which subjects a class studies in an academic year — drives the marks grid.
    /// </summary>
    [RoutePrefix("school/class-subjects")]
    public class ClassSubjectsController : BaseSchoolController
    {
        private readonly ClassSubjectRepository _repo;

        public ClassSubjectsController()
        {
            _repo = new ClassSubjectRepository(new TenantConnectionFactory());
        }

        // GET school/class-subjects?academicYearId=&classId=
        [HttpGet, Route("")]
        public HttpResponseMessage GetMapping([FromUri] int academicYearId, [FromUri] int classId)
        {
            if (academicYearId <= 0 || classId <= 0)
                return BadRequest("academicYearId and classId are required.");
            return Ok(_repo.GetMapping(Tenant.TenantDbName, Tenant.SchoolId, academicYearId, classId));
        }

        // GET school/class-subjects/available-subjects?academicYearId=
        [HttpGet, Route("available-subjects")]
        public HttpResponseMessage GetAvailableSubjects([FromUri] int academicYearId)
        {
            if (academicYearId <= 0)
                return BadRequest("academicYearId is required.");
            return Ok(_repo.GetAvailableSubjects(Tenant.TenantDbName, Tenant.SchoolId, academicYearId));
        }

        // GET school/class-subjects/for-class?classId=
        // Subjects mapped to a class across ALL years (no academic-year filter) — exam setup.
        [HttpGet, Route("for-class")]
        public HttpResponseMessage GetSubjectsForClass([FromUri] int classId)
        {
            if (classId <= 0)
                return BadRequest("classId is required.");
            return Ok(_repo.GetSubjectsForClass(Tenant.TenantDbName, Tenant.SchoolId, classId));
        }

        // POST school/class-subjects — replaces the class's whole subject set for the year
        [HttpPost, Route("")]
        public HttpResponseMessage SaveMapping([FromBody] SaveClassSubjectsRequest request)
        {
            if (request == null)
                return BadRequest("Request body is required.");
            if (request.AcademicYearId <= 0)
                return BadRequest("academicYearId is required.");
            if (request.ClassId <= 0)
                return BadRequest("classId is required.");

            _repo.SaveMapping(Tenant.TenantDbName, Tenant.SchoolId, Tenant.FullName, request);
            return Ok(_repo.GetMapping(Tenant.TenantDbName, Tenant.SchoolId, request.AcademicYearId, request.ClassId),
                "Class subjects saved.");
        }
    }
}
