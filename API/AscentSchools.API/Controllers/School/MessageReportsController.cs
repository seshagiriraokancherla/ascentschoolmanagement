using AscentSchools.Core.DTOs.School.Messaging;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    /// <summary>
    /// Admin review of reported parent/teacher messages.
    /// Required by Play Store UGC policy: reported content must have a path to be
    /// reviewed and removed. Surfaced in the school app under Settings.
    /// </summary>
    [RoutePrefix("school/message-reports")]
    public class MessageReportsController : BaseSchoolController
    {
        private readonly MessagingRepository _repo;

        public MessageReportsController()
        {
            _repo = new MessagingRepository(new TenantConnectionFactory());
        }

        // GET school/message-reports?status=Open
        [HttpGet, Route("")]
        public HttpResponseMessage GetReports([FromUri] string status = null)
            => Ok(_repo.GetReports(Tenant.TenantDbName, Tenant.SchoolId, status));

        // GET school/message-reports/open-count — for the nav badge
        [HttpGet, Route("open-count")]
        public HttpResponseMessage GetOpenCount()
            => Ok(_repo.CountOpenReports(Tenant.TenantDbName, Tenant.SchoolId));

        // POST school/message-reports/{id}/resolve
        [HttpPost, Route("{id:int}/resolve")]
        public HttpResponseMessage ResolveReport(int id, [FromBody] ResolveReportRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Action))
                return BadRequest("action is required.");
            if (request.Action != "Removed" && request.Action != "Reviewed")
                return BadRequest("action must be 'Removed' or 'Reviewed'.");

            _repo.ResolveReport(Tenant.TenantDbName, Tenant.SchoolId, id, request.Action, Tenant.FullName);
            return Ok(_repo.GetReports(Tenant.TenantDbName, Tenant.SchoolId, null),
                request.Action == "Removed" ? "Message removed." : "Report marked reviewed.");
        }
    }
}
