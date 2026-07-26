using AscentSchools.Core.DTOs.School.ClassTeachers;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    /// <summary>
    /// Class -> teacher assignment (Master Data → Class Teachers).
    /// Drives parent -> teacher message routing.
    /// </summary>
    [RoutePrefix("school/class-teachers")]
    public class ClassTeachersController : BaseSchoolController
    {
        private readonly ClassTeacherRepository _repo;

        public ClassTeachersController()
        {
            _repo = new ClassTeacherRepository(new TenantConnectionFactory());
        }

        // GET school/class-teachers?academicYearId=&classId=
        [HttpGet, Route("")]
        public HttpResponseMessage GetAssignments([FromUri] int? academicYearId = null, [FromUri] int? classId = null)
            => Ok(_repo.GetAssignments(Tenant.TenantDbName, Tenant.SchoolId, academicYearId, classId));

        // GET school/class-teachers/teachers — active logins (assign-teacher dropdown)
        [HttpGet, Route("teachers")]
        public HttpResponseMessage GetAssignableTeachers()
            => Ok(_repo.GetAssignableTeachers(Tenant.TenantDbName, Tenant.SchoolId));

        [HttpPost, Route("")]
        public HttpResponseMessage CreateAssignment([FromBody] SaveClassTeacherRequest request)
        {
            if (request == null)
                return BadRequest("Request body is required.");
            if (request.AcademicYearId <= 0)
                return BadRequest("academicYearId is required.");
            if (request.ClassId <= 0)
                return BadRequest("classId is required.");
            if (request.UserId <= 0)
                return BadRequest("A teacher is required.");

            // Treat 0 as "whole class" so the client can send either 0 or null.
            request.SectionId = request.SectionId > 0 ? request.SectionId : (int?)null;

            if (_repo.AssignmentExists(Tenant.TenantDbName, Tenant.SchoolId, request))
                return BadRequest("That teacher is already assigned to this class and section.");

            _repo.CreateAssignment(Tenant.TenantDbName, Tenant.SchoolId, Tenant.FullName, request);
            return Created(
                _repo.GetAssignments(Tenant.TenantDbName, Tenant.SchoolId, request.AcademicYearId, null),
                "Class teacher assigned.");
        }

        [HttpDelete, Route("{id:int}")]
        public HttpResponseMessage DeleteAssignment(int id)
        {
            var removed = _repo.DeleteAssignment(Tenant.TenantDbName, Tenant.SchoolId, id);
            if (removed == 0)
                return NotFound("Assignment not found.");
            return Ok(true, "Class teacher unassigned.");
        }
    }
}
