using AscentSchools.Core.DTOs.School.ExamMaster;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System.Linq;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    /// <summary>
    /// exam_master — exam definitions per class + subject (Master Data → Exam Master).
    /// </summary>
    [RoutePrefix("school/exam-master")]
    public class ExamMasterController : BaseSchoolController
    {
        private readonly ExamMasterRepository _repo;

        public ExamMasterController()
        {
            _repo = new ExamMasterRepository(new TenantConnectionFactory());
        }

        // GET school/exam-master?academicYearId=&examTypeId=&classId=
        [HttpGet, Route("")]
        public HttpResponseMessage GetExams(
            [FromUri] int academicYearId, [FromUri] int? examTypeId = null, [FromUri] int? classId = null)
        {
            if (academicYearId <= 0)
                return BadRequest("academicYearId is required.");
            return Ok(_repo.GetExams(Tenant.TenantDbName, Tenant.SchoolId, academicYearId, examTypeId, classId));
        }

        // POST school/exam-master — creates one exam row per selected subject
        [HttpPost, Route("")]
        public HttpResponseMessage CreateExams([FromBody] CreateExamMasterRequest request)
        {
            if (request == null)
                return BadRequest("Request body is required.");
            if (request.AcademicYearId <= 0 || request.ExamTypeId <= 0 || request.ClassId <= 0)
                return BadRequest("academicYearId, examTypeId and classId are required.");
            if (request.SubjectIds == null || !request.SubjectIds.Any())
                return BadRequest("Select at least one subject.");

            _repo.CreateExams(Tenant.TenantDbName, Tenant.SchoolId, Tenant.FullName, request);
            return Ok(
                _repo.GetExams(Tenant.TenantDbName, Tenant.SchoolId, request.AcademicYearId, request.ExamTypeId, request.ClassId),
                "Exams saved.");
        }

        // POST school/exam-master/bulk — CSV import (names → ids; skips existing)
        [HttpPost, Route("bulk")]
        public HttpResponseMessage BulkImport([FromBody] BulkExamMasterRequest request)
        {
            if (request?.Rows == null || !request.Rows.Any())
                return BadRequest("No rows provided.");
            if (request.Rows.Count > 2000)
                return BadRequest("Max 2000 rows per import.");
            var result = _repo.BulkImport(Tenant.TenantDbName, Tenant.SchoolId, Tenant.FullName, request.Rows);
            return Ok(result, $"Imported {result.Imported} of {result.Total}.");
        }

        // PUT school/exam-master/{id}
        [HttpPut, Route("{id:int}")]
        public HttpResponseMessage UpdateExam(int id, [FromBody] UpdateExamMasterRequest request)
        {
            if (request == null)
                return BadRequest("Request body is required.");
            if (request.AcademicYearId <= 0 || request.ExamTypeId <= 0 || request.ClassId <= 0 || request.SubjectId <= 0)
                return BadRequest("academicYearId, examTypeId, classId and subjectId are required.");

            _repo.UpdateExam(Tenant.TenantDbName, Tenant.SchoolId, id, request);
            return Ok(
                _repo.GetExams(Tenant.TenantDbName, Tenant.SchoolId, request.AcademicYearId, request.ExamTypeId, request.ClassId),
                "Exam updated.");
        }

        // DELETE school/exam-master/{id}
        [HttpDelete, Route("{id:int}")]
        public HttpResponseMessage DeleteExam(int id)
        {
            var removed = _repo.DeleteExam(Tenant.TenantDbName, Tenant.SchoolId, id);
            if (removed == 0)
                return NotFound("Exam not found.");
            return Ok(true, "Exam deleted.");
        }
    }
}
