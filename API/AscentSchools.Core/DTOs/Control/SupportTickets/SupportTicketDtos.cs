using System;

namespace AscentSchools.Core.DTOs.Control.SupportTickets
{
    /// <summary>
    /// A support ticket (Issue/Change) raised from the school app and managed
    /// from the control app. Lives in ascent_master — one place for every
    /// school group. raised_by_* are snapshots (tenant users have no FK across
    /// databases); Group/School/AssignedTo names come from the control-side
    /// list query's joins (null on the school-side list, which doesn't need them).
    /// </summary>
    public class SupportTicketDto
    {
        public int       TicketId                 { get; set; }
        public int       GroupId                  { get; set; }
        public string    GroupName                { get; set; }
        public int?      SchoolId                 { get; set; }
        public string    SchoolName               { get; set; }
        public string    DbName                   { get; set; }
        public int       RaisedByUserId            { get; set; }
        public string    RaisedByName              { get; set; }
        public string    RaisedByUsername          { get; set; }
        public string    TicketType                { get; set; }   // Issue | Change
        public string    Priority                  { get; set; }   // Low | Medium | High | Critical
        public string    Subject                   { get; set; }
        public string    Description               { get; set; }
        public string    Module                    { get; set; }
        public string    Status                    { get; set; }   // Open | InProgress | Done | Cancelled
        public int?      AssignedToControlUserId   { get; set; }
        public string    AssignedToName            { get; set; }
        public string    ResolutionNotes           { get; set; }
        public DateTime  CreatedAt                 { get; set; }
        public DateTime? UpdatedAt                 { get; set; }
        public DateTime? ResolvedAt                { get; set; }
    }

    public class CreateSupportTicketRequest
    {
        public string TicketType  { get; set; }   // Issue | Change
        public string Priority    { get; set; }   // Low | Medium | High | Critical
        public string Subject     { get; set; }
        public string Description { get; set; }
        public string Module      { get; set; }
    }

    public class UpdateSupportTicketStatusRequest
    {
        public string Status                    { get; set; }   // Open | InProgress | Done | Cancelled
        public string ResolutionNotes           { get; set; }
        public int?   AssignedToControlUserId   { get; set; }
    }
}
