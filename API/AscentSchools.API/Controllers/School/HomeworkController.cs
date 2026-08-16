using AscentSchools.API.Helpers;
using AscentSchools.Core.DTOs.School.Homework;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System;
using System.Linq;
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

        // GET school/homework?classId=&sectionId=&assignedDate=&page=1&pageSize=20
        // Server-paged. Returns { items, total, page, pageSize }.
        [HttpGet, Route("")]
        public HttpResponseMessage GetHomework(
            [FromUri] int? classId = null, [FromUri] int? sectionId = null,
            [FromUri] DateTime? assignedDate = null,
            [FromUri] int page = 1, [FromUri] int pageSize = 20)
        {
            var (items, total) = _repo.GetHomeworkPaged(
                Tenant.TenantDbName, Tenant.SchoolId, classId, sectionId, assignedDate, page, pageSize);
            return Ok(new { items, total, page, pageSize });
        }

        [HttpPost, Route("")]
        public HttpResponseMessage CreateHomework([FromBody] SaveHomeworkRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Title))
                return BadRequest("Title is required.");
            var id = _repo.CreateHomework(Tenant.TenantDbName, Tenant.SchoolId, Tenant.FullName, request);

            new PushNotifier().NotifyClass(
                Tenant.TenantDbName, Tenant.GroupId, Tenant.SchoolId,
                request.ClassId, request.SectionId,
                "New Homework", request.Title, "homework", id);

            return Created(id, "Homework created.");
        }

        // POST school/homework/batch — multi-subject daily homework entry.
        [HttpPost, Route("batch")]
        public HttpResponseMessage CreateBatchHomework([FromBody] BatchHomeworkRequest request)
        {
            if (request == null)
                return BadRequest("No data provided.");
            if (!request.ClassId.HasValue || request.ClassId.Value <= 0)
                return BadRequest("Class is required.");
            if (request.AssignedDate == default(DateTime))
                return BadRequest("Date is required.");
            if (request.Items == null || !request.Items.Any(i => i.SubjectId.HasValue && !string.IsNullOrWhiteSpace(i.Description)))
                return BadRequest("Enter homework for at least one subject.");

            var result = _repo.CreateBatchHomework(Tenant.TenantDbName, Tenant.SchoolId, Tenant.FullName, request);

            // One push per target section; a null target notifies the whole class.
            if (result.SubjectCount > 0)
            {
                var notifier = new PushNotifier();
                foreach (var sectionId in result.Sections)
                    notifier.NotifyClass(
                        Tenant.TenantDbName, Tenant.GroupId, Tenant.SchoolId,
                        request.ClassId, sectionId,
                        "New Homework", "Today's homework has been posted.", "homework", 0);
            }

            var scope = result.Sections.Count == 1 && !result.Sections[0].HasValue
                ? "all sections"
                : $"{result.Sections.Count} section(s)";
            return Ok(result, $"Saved homework for {result.SubjectCount} subject(s) in {scope}.");
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
