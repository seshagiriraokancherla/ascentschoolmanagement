using AscentSchools.Core.DTOs.School.Attendance;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System;
using System.Linq;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    [RoutePrefix("school/attendance")]
    public class AttendanceController : BaseSchoolController
    {
        private readonly AttendanceRepository _repo;

        public AttendanceController()
        {
            _repo = new AttendanceRepository(new TenantConnectionFactory());
        }

        // GET /school/attendance?classId=5&sectionId=2&date=2025-01-15
        [HttpGet, Route("")]
        public HttpResponseMessage GetAttendance([FromUri] int classId, [FromUri] int sectionId, [FromUri] string date)
        {
            if (classId <= 0 || sectionId <= 0)
                return BadRequest("classId and sectionId are required.");
            if (string.IsNullOrWhiteSpace(date) || !DateTime.TryParse(date, out _))
                return BadRequest("A valid date (yyyy-MM-dd) is required.");

            var grid = _repo.GetAttendanceGrid(Tenant.TenantDbName, Tenant.SchoolId, classId, sectionId, date);
            return Ok(grid);
        }

        // POST /school/attendance
        [HttpPost, Route("")]
        public HttpResponseMessage SaveAttendance([FromBody] SaveAttendanceRequest request)
        {
            if (request == null)
                return BadRequest("Request body is required.");
            if (request.ClassId <= 0)
                return BadRequest("classId is required.");
            if (string.IsNullOrWhiteSpace(request.Date) || !DateTime.TryParse(request.Date, out _))
                return BadRequest("A valid date (yyyy-MM-dd) is required.");
            if (request.Entries == null || !request.Entries.Any())
                return BadRequest("No entries provided.");

            var validStatuses = new[] { "Present", "Absent", "Late", "Holiday" };
            if (request.Entries.Any(e => !validStatuses.Contains(e.Status)))
                return BadRequest("Status must be Present, Absent, Late or Holiday.");

            _repo.SaveAttendance(Tenant.TenantDbName, Tenant.SchoolId, Tenant.FullName, request);
            return Ok(true, "Attendance saved.");
        }

        // GET /school/attendance/summary?classId=5&sectionId=2&month=3&year=2025
        [HttpGet, Route("summary")]
        public HttpResponseMessage GetMonthlySummary(
            [FromUri] int classId, [FromUri] int sectionId, [FromUri] int month, [FromUri] int year)
        {
            if (classId <= 0 || sectionId <= 0 || month < 1 || month > 12 || year < 2000)
                return BadRequest("Valid classId, sectionId, month (1-12) and year are required.");

            var summary = _repo.GetMonthlySummary(Tenant.TenantDbName, Tenant.SchoolId, classId, sectionId, month, year);
            return Ok(summary);
        }
    }
}
