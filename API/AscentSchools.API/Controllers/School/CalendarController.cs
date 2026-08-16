using AscentSchools.Core.DTOs.School.Calendar;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System.Linq;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    [RoutePrefix("school/calendar")]
    public class CalendarController : BaseSchoolController
    {
        private static readonly string[] Categories = { "Holiday", "Exam", "Celebration", "Event" };

        private readonly CalendarRepository _repo;

        public CalendarController()
        {
            _repo = new CalendarRepository(new TenantConnectionFactory());
        }

        // GET school/calendar?month=&year=&academicYearId=
        // month/year omitted (or 0) → all active entries for the school.
        [HttpGet, Route("")]
        public HttpResponseMessage GetEvents(
            [FromUri] int month = 0, [FromUri] int year = 0, [FromUri] int? academicYearId = null)
            => Ok(_repo.GetEvents(Tenant.TenantDbName, Tenant.SchoolId, month, year, academicYearId));

        [HttpPost, Route("")]
        public HttpResponseMessage CreateEvent([FromBody] SaveCalendarEventRequest request)
        {
            var error = Validate(request);
            if (error != null) return BadRequest(error);

            Normalize(request);
            var id = _repo.CreateEvent(Tenant.TenantDbName, Tenant.SchoolId, Tenant.FullName, request);
            return Created(id, "Calendar entry created.");
        }

        [HttpPut, Route("{id:int}")]
        public HttpResponseMessage UpdateEvent(int id, [FromBody] SaveCalendarEventRequest request)
        {
            var error = Validate(request);
            if (error != null) return BadRequest(error);

            Normalize(request);
            _repo.UpdateEvent(Tenant.TenantDbName, Tenant.SchoolId, id, Tenant.FullName, request);
            return Ok(true, "Calendar entry updated.");
        }

        [HttpDelete, Route("{id:int}")]
        public HttpResponseMessage DeleteEvent(int id)
        {
            _repo.DeleteEvent(Tenant.TenantDbName, Tenant.SchoolId, id, Tenant.FullName);
            return Ok(true, "Calendar entry deleted.");
        }

        // ── Helpers ───────────────────────────────────────────────────────
        private static string Validate(SaveCalendarEventRequest r)
        {
            if (r == null || string.IsNullOrWhiteSpace(r.Title))
                return "Title is required.";
            if (string.IsNullOrWhiteSpace(r.Category) || !Categories.Contains(r.Category))
                return "Category must be one of Holiday, Exam, Celebration, Event.";
            if (r.StartDate == null)
                return "Start date is required.";
            if (r.EndDate != null && r.EndDate.Value.Date < r.StartDate.Value.Date)
                return "End date cannot be before start date.";
            return null;
        }

        // Single-day entries may omit EndDate — default it to StartDate.
        private static void Normalize(SaveCalendarEventRequest r)
        {
            if (r.EndDate == null) r.EndDate = r.StartDate;
        }
    }
}
