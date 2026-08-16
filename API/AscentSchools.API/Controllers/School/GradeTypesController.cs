using AscentSchools.Core.DTOs.School.GradeTypes;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System.Linq;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    /// <summary>
    /// grade_types — marks-to-grade bands (Master Data → Grade Types).
    /// Also feeds the Exam Master grade-type dropdown.
    /// </summary>
    [RoutePrefix("school/grade-types")]
    public class GradeTypesController : BaseSchoolController
    {
        private readonly GradeTypeRepository _repo;

        public GradeTypesController()
        {
            _repo = new GradeTypeRepository(new TenantConnectionFactory());
        }

        // GET school/grade-types?subjectId=
        [HttpGet, Route("")]
        public HttpResponseMessage GetAll([FromUri] int? subjectId = null)
            => Ok(_repo.GetAll(Tenant.TenantDbName, Tenant.SchoolId, subjectId));

        [HttpPost, Route("")]
        public HttpResponseMessage Create([FromBody] SaveGradeTypeRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.GradeName))
                return BadRequest("Grade name is required.");
            _repo.Create(Tenant.TenantDbName, Tenant.SchoolId, Tenant.FullName, request);
            return Ok(_repo.GetAll(Tenant.TenantDbName, Tenant.SchoolId, null), "Grade type created.");
        }

        [HttpPut, Route("{id:int}")]
        public HttpResponseMessage Update(int id, [FromBody] SaveGradeTypeRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.GradeName))
                return BadRequest("Grade name is required.");
            _repo.Update(Tenant.TenantDbName, Tenant.SchoolId, id, request);
            return Ok(_repo.GetAll(Tenant.TenantDbName, Tenant.SchoolId, null), "Grade type updated.");
        }

        // POST school/grade-types/bulk — CSV import (subject matched by name)
        [HttpPost, Route("bulk")]
        public HttpResponseMessage BulkImport([FromBody] BulkGradeTypeRequest request)
        {
            if (request?.Rows == null || !request.Rows.Any())
                return BadRequest("No rows provided.");
            if (request.Rows.Count > 1000)
                return BadRequest("Max 1000 rows per import.");
            var result = _repo.BulkCreate(Tenant.TenantDbName, Tenant.SchoolId, Tenant.FullName, request.Rows);
            return Ok(result, $"Imported {result.Imported} of {result.Total}.");
        }

        [HttpDelete, Route("{id:int}")]
        public HttpResponseMessage Delete(int id)
        {
            var removed = _repo.Delete(Tenant.TenantDbName, Tenant.SchoolId, id);
            if (removed == 0)
                return NotFound("Grade type not found.");
            return Ok(true, "Grade type deleted.");
        }
    }
}
