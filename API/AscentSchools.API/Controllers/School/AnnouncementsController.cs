using AscentSchools.Core.DTOs.School.Announcements;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    [RoutePrefix("school/announcements")]
    public class AnnouncementsController : BaseSchoolController
    {
        private readonly AnnouncementsRepository _repo;

        public AnnouncementsController()
        {
            _repo = new AnnouncementsRepository(new TenantConnectionFactory());
        }

        [HttpGet, Route("")]
        public HttpResponseMessage GetAnnouncements([FromUri] int? classId = null)
            => Ok(_repo.GetAnnouncements(Tenant.TenantDbName, Tenant.SchoolId, classId));

        [HttpPost, Route("")]
        public HttpResponseMessage CreateAnnouncement([FromBody] SaveAnnouncementRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Title))
                return BadRequest("Title is required.");
            if (string.IsNullOrWhiteSpace(request.Scope) ||
                (request.Scope != "School" && request.Scope != "Class"))
                return BadRequest("Scope must be 'School' or 'Class'.");
            if (request.Scope == "Class" && (request.ClassId == null || request.ClassId <= 0))
                return BadRequest("Class is required when scope is Class.");

            var id = _repo.CreateAnnouncement(Tenant.TenantDbName, Tenant.SchoolId, Tenant.FullName, request);
            return Created(id, "Announcement created.");
        }

        [HttpPut, Route("{id:int}")]
        public HttpResponseMessage UpdateAnnouncement(int id, [FromBody] SaveAnnouncementRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Title))
                return BadRequest("Title is required.");
            if (string.IsNullOrWhiteSpace(request.Scope) ||
                (request.Scope != "School" && request.Scope != "Class"))
                return BadRequest("Scope must be 'School' or 'Class'.");
            if (request.Scope == "Class" && (request.ClassId == null || request.ClassId <= 0))
                return BadRequest("Class is required when scope is Class.");

            _repo.UpdateAnnouncement(Tenant.TenantDbName, Tenant.SchoolId, id, Tenant.FullName, request);
            return Ok(_repo.GetAnnouncements(Tenant.TenantDbName, Tenant.SchoolId, null));
        }

        [HttpDelete, Route("{id:int}")]
        public HttpResponseMessage DeleteAnnouncement(int id)
        {
            _repo.DeleteAnnouncement(Tenant.TenantDbName, Tenant.SchoolId, id, Tenant.FullName);
            return Ok(true, "Announcement deleted.");
        }
    }
}
