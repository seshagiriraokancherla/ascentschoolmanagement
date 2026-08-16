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
        // sectionId is optional — omit it to load the whole class (all sections).
        [HttpGet, Route("")]
        public HttpResponseMessage GetAttendance([FromUri] int classId, [FromUri] string date, [FromUri] int? sectionId = null)
        {
            if (classId <= 0)
                return BadRequest("classId is required.");
            if (string.IsNullOrWhiteSpace(date) || !DateTime.TryParse(date, out _))
                return BadRequest("A valid date (yyyy-MM-dd) is required.");

            var grid = _repo.GetAttendanceGrid(Tenant.TenantDbName, Tenant.SchoolId, classId,
                                               sectionId > 0 ? sectionId : null, date);
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

            var validStatuses = new[] { "Present", "Absent", "Late", "HalfDay", "Holiday" };
            if (request.Entries.Any(e => !validStatuses.Contains(e.Status)))
                return BadRequest("Status must be Present, Absent, Late, HalfDay or Holiday.");

            _repo.SaveAttendance(Tenant.TenantDbName, Tenant.SchoolId, Tenant.FullName, request);
            return Ok(true, "Attendance saved.");
        }

        // DELETE /school/attendance?classId=5&sectionId=2&date=2025-01-15
        // Hard-deletes attendance for a class on a date (so it can be re-marked).
        // sectionId is optional — omit it to delete the whole class (all sections).
        [HttpDelete, Route("")]
        public HttpResponseMessage DeleteAttendance([FromUri] int classId, [FromUri] string date, [FromUri] int? sectionId = null)
        {
            if (classId <= 0)
                return BadRequest("classId is required.");
            if (string.IsNullOrWhiteSpace(date) || !DateTime.TryParse(date, out _))
                return BadRequest("A valid date (yyyy-MM-dd) is required.");

            var count = _repo.DeleteAttendance(Tenant.TenantDbName, Tenant.SchoolId, classId,
                                               sectionId > 0 ? sectionId : null, date);
            return Ok(count, $"Deleted attendance for {count} student(s).");
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

        // GET /school/attendance/monthly-export?month=7&year=2026
        // Present-day counts for every student in the month — read by the sync tool
        // (X-Api-Key) and pushed into the legacy SAS_BulkAttendance table.
        [HttpGet, Route("monthly-export")]
        public HttpResponseMessage GetMonthlyExport([FromUri] int month, [FromUri] int year)
        {
            if (month < 1 || month > 12 || year < 2000)
                return BadRequest("Valid month (1-12) and year are required.");

            var rows = _repo.GetMonthlyPresentExport(Tenant.TenantDbName, Tenant.SchoolId, month, year);
            return Ok(rows);
        }
    }
}
