using AscentSchools.Core.DTOs.School.Events;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    [RoutePrefix("school/events")]
    public class SchoolEventsController : BaseSchoolController
    {
        private readonly SchoolEventsRepository _repo;

        public SchoolEventsController()
        {
            _repo = new SchoolEventsRepository(new TenantConnectionFactory());
        }

        // GET school/events?classId=3
        [HttpGet, Route("")]
        public HttpResponseMessage GetEvents([FromUri] int? classId = null)
            => Ok(_repo.GetEvents(Tenant.TenantDbName, Tenant.SchoolId, classId));

        // POST school/events
        [HttpPost, Route("")]
        public HttpResponseMessage CreateEvent([FromBody] SaveSchoolEventRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Title))
                return BadRequest("Title is required.");
            if (string.IsNullOrWhiteSpace(request.EventDate))
                return BadRequest("Event date is required.");
            if (string.IsNullOrWhiteSpace(request.MediaUrl))
                return BadRequest("Media URL is required.");
            if (request.Scope == "Class" && request.ClassId == null)
                return BadRequest("Class is required for class-scoped events.");

            var id = _repo.CreateEvent(Tenant.TenantDbName, Tenant.SchoolId, Tenant.FullName, request);
            return Created(id, "Event created.");
        }

        // PUT school/events/{id}
        [HttpPut, Route("{id:int}")]
        public HttpResponseMessage UpdateEvent(int id, [FromBody] SaveSchoolEventRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Title))
                return BadRequest("Title is required.");
            if (string.IsNullOrWhiteSpace(request.MediaUrl))
                return BadRequest("Media URL is required.");
            if (request.Scope == "Class" && request.ClassId == null)
                return BadRequest("Class is required for class-scoped events.");

            _repo.UpdateEvent(Tenant.TenantDbName, Tenant.SchoolId, id, Tenant.FullName, request);
            return Ok(_repo.GetEvents(Tenant.TenantDbName, Tenant.SchoolId, null));
        }

        // DELETE school/events/{id}
        [HttpDelete, Route("{id:int}")]
        public HttpResponseMessage DeleteEvent(int id)
        {
            _repo.DeleteEvent(Tenant.TenantDbName, Tenant.SchoolId, id, Tenant.FullName);
            return Ok(true, "Event deleted.");
        }
    }
}
