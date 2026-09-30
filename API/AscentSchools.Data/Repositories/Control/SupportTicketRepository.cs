using AscentSchools.Core.DTOs.Control.SupportTickets;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System;
using System.Collections.Generic;

namespace AscentSchools.Data.Repositories.Control
{
    /// <summary>
    /// Support tickets (Issue/Change) — raised from the school app, managed from the
    /// control app. Lives in ascent_master (a cross-tenant concept), so every method
    /// here uses GetMasterConnection() regardless of which side calls it. Instantiated
    /// from both a school controller (create + branch list) and a control controller
    /// (list across every group + status updates) — same dual-use shape as
    /// SchoolSettingsRepository (Phase 36), which is school-side but master-DB-backed.
    /// </summary>
    public class SupportTicketRepository
    {
        private readonly IConnectionFactory _db;
        public SupportTicketRepository(IConnectionFactory db) { _db = db; }

        // Server runs US Eastern (Phase 98) — every server-stamped date must be IST.
        private const string IstNow =
            "CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'India Standard Time' AS DATETIME)";

        // ── School side ──────────────────────────────────────────────────────

        public int CreateTicket(
            int groupId, int schoolId, string dbName,
            int raisedByUserId, string raisedByName, string raisedByUsername,
            CreateSupportTicketRequest req)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.QuerySingle<int>(
                    $@"INSERT INTO support_tickets
                        (group_id, school_id, db_name, raised_by_user_id, raised_by_name, raised_by_username,
                         ticket_type, priority, subject, description, module, status, created_at)
                      VALUES
                        (@groupId, @schoolId, @dbName, @raisedByUserId, @raisedByName, @raisedByUsername,
                         @TicketType, @Priority, @Subject, @Description, @Module, 'Open', {IstNow});
                      SELECT CAST(SCOPE_IDENTITY() AS INT)",
                    new
                    {
                        groupId, schoolId, dbName, raisedByUserId, raisedByName, raisedByUsername,
                        req.TicketType, req.Priority, req.Subject, req.Description, req.Module
                    });
        }

        /// <summary>
        /// Branch-wide visibility — every ticket raised for this school (any staff
        /// member there, not just the current user), so colleagues can see whether
        /// something was already reported before filing a duplicate.
        /// </summary>
        public IEnumerable<SupportTicketDto> GetTicketsForSchool(int groupId, int schoolId)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.Query<SupportTicketDto>(
                    @"SELECT ticket_id TicketId, group_id GroupId, school_id SchoolId, db_name DbName,
                             raised_by_user_id RaisedByUserId, raised_by_name RaisedByName,
                             raised_by_username RaisedByUsername,
                             ticket_type TicketType, priority Priority, subject Subject,
                             description Description, module Module, status Status,
                             resolution_notes ResolutionNotes,
                             created_at CreatedAt, updated_at UpdatedAt, resolved_at ResolvedAt
                      FROM support_tickets
                      WHERE group_id = @groupId AND school_id = @schoolId
                      ORDER BY created_at DESC",
                    new { groupId, schoolId });
        }

        /// <summary>Detail for the school side — scoped so a ticket from another branch/group 404s.</summary>
        public SupportTicketDto GetTicketForSchool(int ticketId, int groupId, int schoolId)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.QueryFirstOrDefault<SupportTicketDto>(
                    @"SELECT ticket_id TicketId, group_id GroupId, school_id SchoolId, db_name DbName,
                             raised_by_user_id RaisedByUserId, raised_by_name RaisedByName,
                             raised_by_username RaisedByUsername,
                             ticket_type TicketType, priority Priority, subject Subject,
                             description Description, module Module, status Status,
                             resolution_notes ResolutionNotes,
                             created_at CreatedAt, updated_at UpdatedAt, resolved_at ResolvedAt
                      FROM support_tickets
                      WHERE ticket_id = @ticketId AND group_id = @groupId AND school_id = @schoolId",
                    new { ticketId, groupId, schoolId });
        }

        // ── Control side ─────────────────────────────────────────────────────

        public IEnumerable<SupportTicketDto> GetAllTickets(
            int? groupId, int? schoolId, string status, string ticketType,
            DateTime? dateFrom, DateTime? dateTo, string search)
        {
            var where = "1 = 1";
            if (groupId.HasValue)      where += " AND t.group_id = @groupId";
            if (schoolId.HasValue)     where += " AND t.school_id = @schoolId";
            if (!string.IsNullOrWhiteSpace(status))     where += " AND t.status = @status";
            if (!string.IsNullOrWhiteSpace(ticketType)) where += " AND t.ticket_type = @ticketType";
            if (dateFrom.HasValue)     where += " AND t.created_at >= @dateFrom";
            if (dateTo.HasValue)       where += " AND t.created_at <= @dateTo";
            if (!string.IsNullOrWhiteSpace(search))
                where += " AND (t.subject LIKE @search OR t.description LIKE @search OR t.raised_by_name LIKE @search)";

            using (var conn = _db.GetMasterConnection())
                return conn.Query<SupportTicketDto>(
                    $@"SELECT t.ticket_id TicketId, t.group_id GroupId, sg.group_name GroupName,
                              t.school_id SchoolId, sc.school_name SchoolName, t.db_name DbName,
                              t.raised_by_user_id RaisedByUserId, t.raised_by_name RaisedByName,
                              t.raised_by_username RaisedByUsername,
                              t.ticket_type TicketType, t.priority Priority, t.subject Subject,
                              t.description Description, t.module Module, t.status Status,
                              t.assigned_to_control_user_id AssignedToControlUserId, cu.full_name AssignedToName,
                              t.resolution_notes ResolutionNotes,
                              t.created_at CreatedAt, t.updated_at UpdatedAt, t.resolved_at ResolvedAt
                       FROM support_tickets t
                       JOIN school_groups sg     ON sg.group_id  = t.group_id
                       LEFT JOIN schools sc      ON sc.school_id = t.school_id
                       LEFT JOIN control_users cu ON cu.user_id  = t.assigned_to_control_user_id
                       WHERE {where}
                       ORDER BY t.created_at DESC",
                    new { groupId, schoolId, status, ticketType, dateFrom, dateTo, search = $"%{search}%" });
        }

        public SupportTicketDto GetTicketById(int ticketId)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.QueryFirstOrDefault<SupportTicketDto>(
                    @"SELECT t.ticket_id TicketId, t.group_id GroupId, sg.group_name GroupName,
                              t.school_id SchoolId, sc.school_name SchoolName, t.db_name DbName,
                              t.raised_by_user_id RaisedByUserId, t.raised_by_name RaisedByName,
                              t.raised_by_username RaisedByUsername,
                              t.ticket_type TicketType, t.priority Priority, t.subject Subject,
                              t.description Description, t.module Module, t.status Status,
                              t.assigned_to_control_user_id AssignedToControlUserId, cu.full_name AssignedToName,
                              t.resolution_notes ResolutionNotes,
                              t.created_at CreatedAt, t.updated_at UpdatedAt, t.resolved_at ResolvedAt
                       FROM support_tickets t
                       JOIN school_groups sg     ON sg.group_id  = t.group_id
                       LEFT JOIN schools sc      ON sc.school_id = t.school_id
                       LEFT JOIN control_users cu ON cu.user_id  = t.assigned_to_control_user_id
                       WHERE t.ticket_id = @ticketId",
                    new { ticketId });
        }

        /// <summary>
        /// resolved_at is set the moment status moves to Done/Cancelled, and cleared if
        /// the ticket is reopened (moved back to Open/InProgress) — so it always reflects
        /// the ticket's CURRENT closed state, not just "closed at least once".
        /// </summary>
        public void UpdateStatus(int ticketId, UpdateSupportTicketStatusRequest req)
        {
            using (var conn = _db.GetMasterConnection())
                conn.Execute(
                    $@"UPDATE support_tickets
                       SET status = @Status,
                           resolution_notes = @ResolutionNotes,
                           assigned_to_control_user_id = @AssignedToControlUserId,
                           updated_at = {IstNow},
                           resolved_at = CASE WHEN @Status IN ('Done', 'Cancelled') THEN {IstNow} ELSE NULL END
                       WHERE ticket_id = @ticketId",
                    new { ticketId, req.Status, req.ResolutionNotes, req.AssignedToControlUserId });
        }
    }
}
