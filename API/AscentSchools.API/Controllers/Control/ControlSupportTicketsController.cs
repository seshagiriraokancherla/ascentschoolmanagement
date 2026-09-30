using AscentSchools.Core.DTOs.Control.SupportTickets;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.Control;
using System;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.Control
{
    /// <summary>
    /// Control app side of support tickets — every group, every branch, one list.
    /// Named ControlSupportTicketsController (not just SupportTicketsController) to
    /// stay globally unique across the API project — two controllers sharing a bare
    /// class name silently breaks BOTH their routing under old-style Web API
    /// attribute routing (the exact Phase 87 trap: control/uploads vs school/uploads).
    /// Any control role can manage tickets (super_admin/support/billing), matching
    /// how most other control endpoints work.
    /// </summary>
    [RoutePrefix("control/support-tickets")]
    public class ControlSupportTicketsController : BaseControlController
    {
        private static readonly string[] ValidStatuses = { "Open", "InProgress", "Done", "Cancelled" };

        private readonly SupportTicketRepository _repo;

        public ControlSupportTicketsController()
        {
            _repo = new SupportTicketRepository(new TenantConnectionFactory());
        }

        // GET control/support-tickets?groupId=&schoolId=&status=&ticketType=&dateFrom=&dateTo=&search=
        [HttpGet, Route("")]
        public HttpResponseMessage GetTickets(
            int? groupId = null, int? schoolId = null, string status = null, string ticketType = null,
            DateTime? dateFrom = null, DateTime? dateTo = null, string search = null)
            => Ok(_repo.GetAllTickets(groupId, schoolId, status, ticketType, dateFrom, dateTo, search));

        // GET control/support-tickets/{id}
        [HttpGet, Route("{id:int}")]
        public HttpResponseMessage GetTicket(int id)
        {
            var ticket = _repo.GetTicketById(id);
            if (ticket == null) return NotFound("Ticket not found.");
            return Ok(ticket);
        }

        // PUT control/support-tickets/{id}/status
        [HttpPut, Route("{id:int}/status")]
        public HttpResponseMessage UpdateStatus(int id, [FromBody] UpdateSupportTicketStatusRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Status))
                return BadRequest("Status is required.");
            if (Array.IndexOf(ValidStatuses, request.Status) < 0)
                return BadRequest("Invalid status.");

            var existing = _repo.GetTicketById(id);
            if (existing == null) return NotFound("Ticket not found.");

            _repo.UpdateStatus(id, request);
            var updated = _repo.GetTicketById(id);
            return Ok(updated, "Ticket updated.");
        }
    }
}
