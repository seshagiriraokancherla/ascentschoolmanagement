using AscentSchools.Core.DTOs.School.Staff;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Results;

namespace AscentSchools.API.Controllers.School
{
    [RoutePrefix("school/staff")]
    public class StaffController : BaseSchoolController
    {
        private readonly StaffRepository _repo;

        public StaffController()
        {
            _repo = new StaffRepository(new TenantConnectionFactory());
        }

        // GET school/staff?search=&status=Active
        [HttpGet, Route("")]
        public HttpResponseMessage GetAll(
            [FromUri] string search = null,
            [FromUri] string status = null)
        {
            return Ok(_repo.GetAll(Tenant.TenantDbName, Tenant.SchoolId, search, status));
        }

        // POST school/staff
        [HttpPost, Route("")]
        public HttpResponseMessage Create([FromBody] SaveStaffRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.StaffName))
                return BadRequest("Staff name is required.");
            var id = _repo.Create(Tenant.TenantDbName, Tenant.SchoolId, req);
            return Ok(id, "Staff member created.");
        }

        // PUT school/staff/{id}
        [HttpPut, Route("{id:int}")]
        public HttpResponseMessage Update(int id, [FromBody] SaveStaffRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.StaffName))
                return BadRequest("Staff name is required.");
            _repo.Update(Tenant.TenantDbName, Tenant.SchoolId, id, req);
            return Ok(true, "Staff member updated.");
        }

        // PUT school/staff/{id}/status
        [HttpPut, Route("{id:int}/status")]
        public HttpResponseMessage SetStatus(int id, [FromBody] SetStaffStatusRequest req)
        {
            var valid = new[] { "Active", "Inactive" };
            if (req == null || !valid.Contains(req.Status))
                return BadRequest("Status must be Active or Inactive.");
            _repo.SetStatus(Tenant.TenantDbName, Tenant.SchoolId, id, req.Status);
            return Ok(true, $"Staff member {req.Status.ToLower()}.");
        }

        // GET school/staff/attendance?date=2025-06-15
        [HttpGet, Route("attendance")]
        public HttpResponseMessage GetAttendanceGrid([FromUri] string date)
        {
            if (string.IsNullOrWhiteSpace(date) || !DateTime.TryParse(date, out _))
                return BadRequest("A valid date (yyyy-MM-dd) is required.");
            return Ok(_repo.GetAttendanceGrid(Tenant.TenantDbName, Tenant.SchoolId, date));
        }

        // POST school/staff/attendance
        [HttpPost, Route("attendance")]
        public HttpResponseMessage SaveAttendance([FromBody] SaveStaffAttendanceRequest req)
        {
            if (req == null)
                return BadRequest("Request body is required.");
            if (string.IsNullOrWhiteSpace(req.Date) || !DateTime.TryParse(req.Date, out _))
                return BadRequest("A valid date (yyyy-MM-dd) is required.");
            if (req.Entries == null || !req.Entries.Any())
                return BadRequest("No entries provided.");

            var valid = new[] { "Present", "Absent", "Late", "HalfDay", "OnLeave" };
            if (req.Entries.Any(e => !valid.Contains(e.Status)))
                return BadRequest("Status must be Present, Absent, Late, HalfDay or OnLeave.");

            _repo.SaveAttendance(Tenant.TenantDbName, Tenant.SchoolId, Tenant.FullName, req);
            return Ok(true, "Staff attendance saved.");
        }

        // GET school/staff/attendance/summary?month=6&year=2025
        [HttpGet, Route("attendance/summary")]
        public HttpResponseMessage GetMonthlySummary([FromUri] int month, [FromUri] int year)
        {
            if (month < 1 || month > 12 || year < 2000)
                return BadRequest("Valid month (1-12) and year are required.");
            return Ok(_repo.GetMonthlySummary(Tenant.TenantDbName, Tenant.SchoolId, month, year));
        }

        // ── Advances ──────────────────────────────────────────────────────────

        // GET school/staff/advances?staffId=&dateFrom=&dateTo=&status=Active
        [HttpGet, Route("advances")]
        public HttpResponseMessage GetAdvances(
            [FromUri] int?     staffId  = null,
            [FromUri] DateTime? dateFrom = null,
            [FromUri] DateTime? dateTo   = null,
            [FromUri] string   status   = null)
        {
            return Ok(_repo.GetAdvances(
                Tenant.TenantDbName, Tenant.SchoolId,
                staffId, dateFrom, dateTo, status));
        }

        // POST school/staff/advances
        [HttpPost, Route("advances")]
        public HttpResponseMessage CreateAdvance([FromBody] CreateAdvanceRequest req)
        {
            if (req == null || req.StaffId <= 0)
                return BadRequest("staffId is required.");
            if (req.Amount <= 0)
                return BadRequest("Amount must be greater than zero.");
            if (string.IsNullOrWhiteSpace(req.AdvanceDate) || !DateTime.TryParse(req.AdvanceDate, out _))
                return BadRequest("A valid advance date is required.");

            var id = _repo.CreateAdvance(
                Tenant.TenantDbName, Tenant.SchoolId, Tenant.FullName, req);
            return Ok(id, "Advance recorded.");
        }

        // PUT school/staff/advances/{id}/cancel
        [HttpPut, Route("advances/{id:int}/cancel")]
        public HttpResponseMessage CancelAdvance(int id)
        {
            _repo.CancelAdvance(Tenant.TenantDbName, Tenant.SchoolId, id);
            return Ok(true, "Advance cancelled.");
        }

        // GET school/staff/advances/{id}/repayments
        [HttpGet, Route("advances/{id:int}/repayments")]
        public HttpResponseMessage GetRepayments(int id)
        {
            return Ok(_repo.GetRepayments(Tenant.TenantDbName, Tenant.SchoolId, id));
        }

        // POST school/staff/advances/{id}/repayments
        [HttpPost, Route("advances/{id:int}/repayments")]
        public HttpResponseMessage AddRepayment(int id, [FromBody] AddRepaymentRequest req)
        {
            if (req == null || req.Amount <= 0)
                return BadRequest("Amount must be greater than zero.");
            if (string.IsNullOrWhiteSpace(req.RepaymentDate) || !DateTime.TryParse(req.RepaymentDate, out _))
                return BadRequest("A valid repayment date is required.");

            var error = _repo.AddRepayment(
                Tenant.TenantDbName, Tenant.SchoolId, Tenant.FullName, id, req);
            if (error != null) return BadRequest(error);
            return Ok(true, "Repayment recorded.");
        }

        // GET school/staff/advances/summary
        [HttpGet, Route("advances/summary")]
        public HttpResponseMessage GetAdvanceSummary()
        {
            return Ok(_repo.GetAdvanceSummary(Tenant.TenantDbName, Tenant.SchoolId));
        }

        // ── Salary components ─────────────────────────────────────────────────

        // GET school/staff/salary-components?staffId=
        [HttpGet, Route("salary-components")]
        public HttpResponseMessage GetSalaryComponents([FromUri] int staffId)
        {
            if (staffId <= 0) return BadRequest("staffId is required.");
            return Ok(_repo.GetSalaryComponents(Tenant.TenantDbName, Tenant.SchoolId, staffId));
        }

        // POST school/staff/salary-components
        [HttpPost, Route("salary-components")]
        public HttpResponseMessage SaveSalaryComponents([FromBody] SaveSalaryComponentsRequest req)
        {
            if (req == null || req.StaffId <= 0)
                return BadRequest("staffId is required.");

            if (req.Components != null)
            {
                var validTypes = new[] { "Earning", "Deduction" };
                foreach (var c in req.Components)
                {
                    if (string.IsNullOrWhiteSpace(c.ComponentName))
                        return BadRequest("Each component must have a name.");
                    if (!validTypes.Contains(c.ComponentType))
                        return BadRequest("Component type must be 'Earning' or 'Deduction'.");
                    if (c.Amount <= 0)
                        return BadRequest($"Amount for '{c.ComponentName}' must be greater than zero.");
                }
            }

            _repo.SaveSalaryComponents(Tenant.TenantDbName, Tenant.SchoolId, req);
            return Ok(true, "Salary structure saved.");
        }

        // ── Monthly salaries ──────────────────────────────────────────────────

        // GET school/staff/salaries?month=&year=&staffId=&status=
        [HttpGet, Route("salaries")]
        public HttpResponseMessage GetSalaries(
            [FromUri] int?   month   = null,
            [FromUri] int?   year    = null,
            [FromUri] int?   staffId = null,
            [FromUri] string status  = null)
        {
            return Ok(_repo.GetSalaries(
                Tenant.TenantDbName, Tenant.SchoolId, month, year, staffId, status));
        }

        // POST school/staff/salaries/process
        [HttpPost, Route("salaries/process")]
        public HttpResponseMessage ProcessSalaries([FromBody] ProcessSalariesRequest req)
        {
            if (req == null)
                return BadRequest("Request body is required.");
            if (req.Month < 1 || req.Month > 12)
                return BadRequest("Month must be between 1 and 12.");
            if (req.Year < 2000)
                return BadRequest("Year must be 2000 or later.");

            int total = _repo.GetSalaries(
                Tenant.TenantDbName, Tenant.SchoolId, req.Month, req.Year, null, null).Count();

            int created = _repo.ProcessSalaries(
                Tenant.TenantDbName, Tenant.SchoolId, Tenant.FullName, req);

            int skipped = total; // already-existing records for this month/year
            return Ok(created, $"{created} salary record(s) created. {skipped} already existed.");
        }

        // PUT school/staff/salaries/{id}
        [HttpPut, Route("salaries/{id:int}")]
        public HttpResponseMessage UpdateSalary(int id, [FromBody] UpdateSalaryRequest req)
        {
            if (req == null) return BadRequest("Request body is required.");
            if (req.AdvanceDeducted < 0) return BadRequest("Advance deducted cannot be negative.");
            _repo.UpdateSalary(Tenant.TenantDbName, Tenant.SchoolId, id, req);
            return Ok(true, "Salary updated.");
        }

        // PUT school/staff/salaries/{id}/mark-paid?paidDate=YYYY-MM-DD
        [HttpPut, Route("salaries/{id:int}/mark-paid")]
        public HttpResponseMessage MarkSalaryPaid(int id, [FromUri] string paidDate = null)
        {
            DateTime paid;
            if (string.IsNullOrWhiteSpace(paidDate))
                paid = DateTime.Today;
            else if (!DateTime.TryParse(paidDate, out paid))
                return BadRequest("Invalid paidDate format (expected YYYY-MM-DD).");

            var error = _repo.MarkSalaryPaid(Tenant.TenantDbName, Tenant.SchoolId, id, paid);
            if (error != null) return BadRequest(error);
            return Ok(true, "Salary marked as Paid.");
        }

        // PUT school/staff/salaries/{id}/cancel
        [HttpPut, Route("salaries/{id:int}/cancel")]
        public HttpResponseMessage CancelSalary(int id)
        {
            var error = _repo.CancelSalary(Tenant.TenantDbName, Tenant.SchoolId, id);
            if (error != null) return BadRequest(error);
            return Ok(true, "Salary cancelled.");
        }

        // GET school/staff/salaries/{id}/slip
        [HttpGet, Route("salaries/{id:int}/slip")]
        public HttpResponseMessage GetSalarySlip(int id)
        {
            var slip = _repo.GetSalarySlip(Tenant.TenantDbName, Tenant.SchoolId, id);
            if (slip == null) return NotFound("Salary record not found.");
            return Ok(slip);
        }
    }
}
