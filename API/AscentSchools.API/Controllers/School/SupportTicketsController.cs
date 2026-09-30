using AscentSchools.Core.DTOs.Control.SupportTickets;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.Control;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    /// <summary>
    /// School staff raise Issue/Change support tickets here. Stored in ascent_master
    /// (SupportTicketRepository), not the tenant DB — the control app manages them
    /// across every group. Open to any authenticated staff member (no permission
    /// gate, same reasoning as the self-service Change Password endpoint — anyone
    /// hitting an issue should be able to report it without needing a grant).
    /// Branch-wide visibility: every ticket for this school branch, not just the
    /// caller's own, so colleagues can see if something was already reported.
    /// </summary>
    [RoutePrefix("school/support-tickets")]
    public class SupportTicketsController : BaseSchoolController
    {
        private static readonly string[] ValidTypes      = { "Issue", "Change" };
        private static readonly string[] ValidPriorities = { "Low", "Medium", "High", "Critical" };

        private readonly SupportTicketRepository _repo;

        public SupportTicketsController()
        {
            _repo = new SupportTicketRepository(new TenantConnectionFactory());
        }

        // GET school/support-tickets
        [HttpGet, Route("")]
        public HttpResponseMessage GetTickets()
            => Ok(_repo.GetTicketsForSchool(Tenant.GroupId, Tenant.SchoolId));

        // GET school/support-tickets/{id}
        [HttpGet, Route("{id:int}")]
        public HttpResponseMessage GetTicket(int id)
        {
            var ticket = _repo.GetTicketForSchool(id, Tenant.GroupId, Tenant.SchoolId);
            if (ticket == null) return NotFound("Ticket not found.");
            return Ok(ticket);
        }

        // POST school/support-tickets
        [HttpPost, Route("")]
        public HttpResponseMessage CreateTicket([FromBody] CreateSupportTicketRequest request)
        {
            if (request == null
                || string.IsNullOrWhiteSpace(request.Subject)
                || string.IsNullOrWhiteSpace(request.Description))
                return BadRequest("Subject and description are required.");

            var normalized = new CreateSupportTicketRequest
            {
                TicketType  = System.Array.IndexOf(ValidTypes, request.TicketType) >= 0 ? request.TicketType : "Issue",
                Priority    = System.Array.IndexOf(ValidPriorities, request.Priority) >= 0 ? request.Priority : "Medium",
                Subject     = request.Subject.Trim(),
                Description = request.Description.Trim(),
                Module      = string.IsNullOrWhiteSpace(request.Module) ? null : request.Module.Trim(),
            };

            var ticketId = _repo.CreateTicket(
                Tenant.GroupId, Tenant.SchoolId, Tenant.TenantDbName,
                Tenant.UserId, Tenant.FullName, null, normalized);

            var ticket = _repo.GetTicketForSchool(ticketId, Tenant.GroupId, Tenant.SchoolId);
            return Created(ticket, "Ticket submitted.");
        }
    }
}
