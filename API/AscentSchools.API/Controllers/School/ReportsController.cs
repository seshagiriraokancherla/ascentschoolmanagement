using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    [RoutePrefix("school/reports")]
    public class ReportsController : BaseSchoolController
    {
        private readonly ReportsRepository _repo;

        public ReportsController()
        {
            _repo = new ReportsRepository(new TenantConnectionFactory());
        }

        // GET school/reports/strength?academicYearId=1
        [HttpGet, Route("strength")]
        public HttpResponseMessage GetStrength([FromUri] int academicYearId)
        {
            if (academicYearId <= 0)
                return BadRequest("academicYearId is required.");
            return Ok(_repo.GetStrength(Tenant.TenantDbName, Tenant.SchoolId, academicYearId));
        }

        // GET school/reports/transport-students?academicYearId=1&busRouteId=
        [HttpGet, Route("transport-students")]
        public HttpResponseMessage GetTransportStudents(
            [FromUri] int  academicYearId,
            [FromUri] int? busRouteId = null)
        {
            if (academicYearId <= 0)
                return BadRequest("academicYearId is required.");
            return Ok(_repo.GetTransportStudents(
                Tenant.TenantDbName, Tenant.SchoolId, academicYearId, busRouteId));
        }

        // GET school/reports/attendance-sheet?classId=1&sectionId=1&month=6&year=2025
        [HttpGet, Route("attendance-sheet")]
        public HttpResponseMessage GetAttendanceSheet(
            [FromUri] int classId, [FromUri] int sectionId,
            [FromUri] int month,   [FromUri] int year)
        {
            if (classId <= 0 || sectionId <= 0 || month <= 0 || year <= 0)
                return BadRequest("classId, sectionId, month and year are required.");
            return Ok(_repo.GetAttendanceSheet(
                Tenant.TenantDbName, Tenant.SchoolId, classId, sectionId, month, year));
        }

        // GET school/reports/absents?dateFrom=2025-06-01&dateTo=2025-06-30&classId=&sectionId=
        [HttpGet, Route("absents")]
        public HttpResponseMessage GetAbsents(
            [FromUri] DateTime dateFrom,
            [FromUri] DateTime dateTo,
            [FromUri] int?     classId   = null,
            [FromUri] int?     sectionId = null)
        {
            if (dateFrom == default(DateTime) || dateTo == default(DateTime))
                return BadRequest("dateFrom and dateTo are required.");
            if (dateFrom > dateTo)
                return BadRequest("dateFrom must be on or before dateTo.");
            return Ok(_repo.GetAbsents(
                Tenant.TenantDbName, Tenant.SchoolId,
                dateFrom, dateTo, classId, sectionId));
        }

        // GET school/reports/class-toppers?academicYearId=1&classId=1&sectionId=1&topN=5
        [HttpGet, Route("class-toppers")]
        public HttpResponseMessage GetClassToppers(
            [FromUri] int  academicYearId,
            [FromUri] int  classId,
            [FromUri] int  sectionId,
            [FromUri] int  topN = 5)
        {
            if (academicYearId <= 0 || classId <= 0 || sectionId <= 0)
                return BadRequest("academicYearId, classId and sectionId are required.");
            return Ok(_repo.GetClassToppers(
                Tenant.TenantDbName, Tenant.SchoolId, academicYearId, classId, sectionId, topN));
        }

        // GET school/reports/exam-toppers?academicYearId=1&examTypeId=1&classId=&topN=10
        [HttpGet, Route("exam-toppers")]
        public HttpResponseMessage GetExamToppers(
            [FromUri] int  academicYearId,
            [FromUri] int  examTypeId,
            [FromUri] int? classId = null,
            [FromUri] int  topN    = 10)
        {
            if (academicYearId <= 0 || examTypeId <= 0)
                return BadRequest("academicYearId and examTypeId are required.");
            return Ok(_repo.GetExamToppers(
                Tenant.TenantDbName, Tenant.SchoolId, academicYearId, examTypeId, classId, topN));
        }

        // GET school/reports/attendance-register?academicYearId=1&classId=1&sectionId=1&dateFrom=2025-06-01&dateTo=2025-06-30
        [HttpGet, Route("attendance-register")]
        public HttpResponseMessage GetAttendanceRegister(
            [FromUri] int      academicYearId,
            [FromUri] int      classId,
            [FromUri] int      sectionId,
            [FromUri] DateTime dateFrom,
            [FromUri] DateTime dateTo)
        {
            if (academicYearId <= 0 || classId <= 0 || sectionId <= 0)
                return BadRequest("academicYearId, classId and sectionId are required.");
            if (dateFrom == default(DateTime) || dateTo == default(DateTime))
                return BadRequest("dateFrom and dateTo are required.");
            if (dateFrom > dateTo)
                return BadRequest("dateFrom must be on or before dateTo.");
            if ((dateTo - dateFrom).TotalDays > 180)
                return BadRequest("Date range cannot exceed 180 days.");
            return Ok(_repo.GetAttendanceRegister(
                Tenant.TenantDbName, Tenant.SchoolId,
                academicYearId, classId, sectionId, dateFrom, dateTo));
        }

        // GET school/reports/monthly-attendance-sheet?academicYearId=1&classId=1&sectionId=1
        [HttpGet, Route("monthly-attendance-sheet")]
        public HttpResponseMessage GetMonthlyAttendanceSheet(
            [FromUri] int academicYearId,
            [FromUri] int classId,
            [FromUri] int sectionId)
        {
            if (academicYearId <= 0 || classId <= 0 || sectionId <= 0)
                return BadRequest("academicYearId, classId and sectionId are required.");
            return Ok(_repo.GetMonthlyAttendanceSheet(
                Tenant.TenantDbName, Tenant.SchoolId, academicYearId, classId, sectionId));
        }

        // GET school/reports/homework-statement?dateFrom=2025-06-01&dateTo=2025-06-30&classId=&subjectId=
        [HttpGet, Route("homework-statement")]
        public HttpResponseMessage GetHomeworkStatement(
            [FromUri] DateTime dateFrom,
            [FromUri] DateTime dateTo,
            [FromUri] int?     classId   = null,
            [FromUri] int?     subjectId = null)
        {
            if (dateFrom == default(DateTime) || dateTo == default(DateTime))
                return BadRequest("dateFrom and dateTo are required.");
            if (dateFrom > dateTo)
                return BadRequest("dateFrom must be on or before dateTo.");
            return Ok(_repo.GetHomeworkStatement(
                Tenant.TenantDbName, Tenant.SchoolId,
                dateFrom, dateTo, classId, subjectId));
        }

        // GET school/reports/academic-year-toppers?academicYearId=1&classId=&sectionId=&topN=10
        [HttpGet, Route("academic-year-toppers")]
        public HttpResponseMessage GetAcademicYearToppers(
            [FromUri] int  academicYearId,
            [FromUri] int? classId   = null,
            [FromUri] int? sectionId = null,
            [FromUri] int  topN      = 10)
        {
            if (academicYearId <= 0)
                return BadRequest("academicYearId is required.");
            return Ok(_repo.GetAcademicYearToppers(
                Tenant.TenantDbName, Tenant.SchoolId,
                academicYearId, classId, sectionId, topN));
        }

        // GET school/reports/failed-students?academicYearId=1&examTypeId=1&classId=&sectionId=&passMarkPct=35
        [HttpGet, Route("failed-students")]
        public HttpResponseMessage GetFailedStudents(
            [FromUri] int  academicYearId,
            [FromUri] int  examTypeId,
            [FromUri] int? classId     = null,
            [FromUri] int? sectionId   = null,
            [FromUri] int  passMarkPct = 35)
        {
            if (academicYearId <= 0 || examTypeId <= 0)
                return BadRequest("academicYearId and examTypeId are required.");
            if (passMarkPct < 1 || passMarkPct > 100)
                return BadRequest("passMarkPct must be between 1 and 100.");
            return Ok(_repo.GetFailedStudents(
                Tenant.TenantDbName, Tenant.SchoolId,
                academicYearId, examTypeId, classId, sectionId, passMarkPct));
        }

        // GET school/reports/daily-attendance-summary?dateFrom=2025-06-01&dateTo=2025-06-30&classId=&sectionId=
        [HttpGet, Route("daily-attendance-summary")]
        public HttpResponseMessage GetDailyAttendanceSummary(
            [FromUri] DateTime dateFrom,
            [FromUri] DateTime dateTo,
            [FromUri] int?     classId   = null,
            [FromUri] int?     sectionId = null)
        {
            if (dateFrom == default(DateTime) || dateTo == default(DateTime))
                return BadRequest("dateFrom and dateTo are required.");
            if (dateFrom > dateTo)
                return BadRequest("dateFrom must be on or before dateTo.");
            return Ok(_repo.GetDailyAttendanceSummary(
                Tenant.TenantDbName, Tenant.SchoolId,
                dateFrom, dateTo, classId, sectionId));
        }

        // GET school/reports/detained-students?academicYearId=&classId=&sectionId=
        [HttpGet, Route("detained-students")]
        public HttpResponseMessage GetDetainedStudents(
            [FromUri] int? academicYearId = null,
            [FromUri] int? classId        = null,
            [FromUri] int? sectionId      = null)
        {
            return Ok(_repo.GetDetainedStudents(
                Tenant.TenantDbName, Tenant.SchoolId, academicYearId, classId, sectionId));
        }

        // GET school/reports/regular-absentees?dateFrom=&dateTo=&minDays=3&classId=&sectionId=
        [HttpGet, Route("regular-absentees")]
        public HttpResponseMessage GetRegularAbsentees(
            [FromUri] DateTime dateFrom,
            [FromUri] DateTime dateTo,
            [FromUri] int      minDays   = 3,
            [FromUri] int?     classId   = null,
            [FromUri] int?     sectionId = null)
        {
            if (dateFrom == default(DateTime) || dateTo == default(DateTime))
                return BadRequest("dateFrom and dateTo are required.");
            if (dateFrom > dateTo)
                return BadRequest("dateFrom must be on or before dateTo.");
            if (minDays < 1) minDays = 1;
            return Ok(_repo.GetRegularAbsentees(
                Tenant.TenantDbName, Tenant.SchoolId,
                dateFrom, dateTo, minDays, classId, sectionId));
        }
    }
}
