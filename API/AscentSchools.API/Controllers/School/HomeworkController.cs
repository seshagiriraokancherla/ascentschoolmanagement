using AscentSchools.Core.DTOs.School.Homework;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    [RoutePrefix("school/homework")]
    public class HomeworkController : BaseSchoolController
    {
        private readonly HomeworkRepository _repo;

        public HomeworkController()
        {
            _repo = new HomeworkRepository(new TenantConnectionFactory());
        }

        [HttpGet, Route("")]
        public HttpResponseMessage GetHomework([FromUri] int? classId = null)
            => Ok(_repo.GetHomework(Tenant.TenantDbName, Tenant.SchoolId, classId));

        [HttpPost, Route("")]
        public HttpResponseMessage CreateHomework([FromBody] SaveHomeworkRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Title))
                return BadRequest("Title is required.");
            if (request.DueDate == default(DateTime))
                return BadRequest("Due date is required.");
            var id = _repo.CreateHomework(Tenant.TenantDbName, Tenant.SchoolId, Tenant.FullName, request);
            return Created(id, "Homework created.");
        }

        [HttpPut, Route("{id:int}")]
        public HttpResponseMessage UpdateHomework(int id, [FromBody] SaveHomeworkRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Title))
                return BadRequest("Title is required.");
            if (request.DueDate == default(DateTime))
                return BadRequest("Due date is required.");
            _repo.UpdateHomework(Tenant.TenantDbName, Tenant.SchoolId, id, Tenant.FullName, request);
            return Ok(_repo.GetHomework(Tenant.TenantDbName, Tenant.SchoolId, request.ClassId));
        }

        [HttpDelete, Route("{id:int}")]
        public HttpResponseMessage DeleteHomework(int id)
        {
            _repo.DeleteHomework(Tenant.TenantDbName, Tenant.SchoolId, id, Tenant.FullName);
            return Ok(true, "Homework deleted.");
        }
    }
}
