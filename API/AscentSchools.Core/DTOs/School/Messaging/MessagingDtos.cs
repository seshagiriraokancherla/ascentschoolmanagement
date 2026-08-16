using System;
using System.Collections.Generic;

namespace AscentSchools.Core.DTOs.School.Messaging
{
    /// <summary>A parent &lt;-&gt; teacher conversation about one child.</summary>
    public class MessageThreadDto
    {
        public int       ThreadId        { get; set; }
        public int       StudentUniqueId { get; set; }
        public int       ParentId        { get; set; }
        public string    StudentName     { get; set; }
        public string    AdmissionNo     { get; set; }
        public string    ClassName       { get; set; }
        public string    SectionName     { get; set; }
        public string    Status          { get; set; }   // Active | Blocked
        public string    BlockedByType   { get; set; }
        public DateTime? BlockedAt       { get; set; }
        public string    LastMessageBody { get; set; }
        public DateTime? LastMessageAt   { get; set; }
        public int       UnreadCount     { get; set; }   // unread for the CALLER's side
        public DateTime  CreatedAt       { get; set; }
    }

    public class MessageDto
    {
        public int       MessageId  { get; set; }
        public int       ThreadId   { get; set; }
        public string    SenderType { get; set; }   // parent | teacher
        public int       SenderId   { get; set; }
        public string    SenderName { get; set; }
        public string    Body       { get; set; }
        public string    Status     { get; set; }   // Active | Removed
        public DateTime? ReadAt     { get; set; }
        public DateTime  CreatedAt  { get; set; }
    }

    /// <summary>A thread plus its messages — what a chat screen opens with.</summary>
    public class MessageThreadDetailDto
    {
        public MessageThreadDto Thread   { get; set; }
        public List<MessageDto> Messages { get; set; }
    }

    /// <summary>
    /// Read-only admin conversation-list row (school web app — Conversations viewer).
    /// Class/section reflect the student's placement in the SELECTED academic year;
    /// TeacherNames are the class teachers assigned for that year.
    /// </summary>
    public class ConversationListItemDto
    {
        public int       ThreadId        { get; set; }
        public int       StudentUniqueId { get; set; }
        public string    StudentName     { get; set; }
        public string    AdmissionNo     { get; set; }
        public string    ClassName       { get; set; }
        public string    SectionName     { get; set; }
        public string    TeacherNames    { get; set; }   // comma-separated assigned class teachers
        public string    Status          { get; set; }   // Active | Blocked
        public string    LastMessageBody { get; set; }
        public DateTime? LastMessageAt   { get; set; }
        public int       MessageCount    { get; set; }
    }

    public class SendMessageRequest
    {
        public string Body { get; set; }
    }

    /// <summary>
    /// A student's current-year placement — everything message routing needs,
    /// in one lookup. Data-layer row shape.
    /// </summary>
    public class StudentClassContextDto
    {
        public int  AcademicYearId  { get; set; }
        public int  ClassId         { get; set; }
        public int? SectionId       { get; set; }
        public int  StudentUniqueId { get; set; }
    }

    public class ReportMessageRequest
    {
        public int    MessageId { get; set; }
        public string Reason    { get; set; }
    }

    /// <summary>Admin review queue row (school web app).</summary>
    public class MessageReportDto
    {
        public int       ReportId       { get; set; }
        public int       MessageId      { get; set; }
        public int       ThreadId       { get; set; }
        public string    ReportedByType { get; set; }
        public int       ReportedById   { get; set; }
        public string    Reason         { get; set; }
        public string    Status         { get; set; }   // Open | Reviewed | Removed
        public string    ReviewedBy     { get; set; }
        public DateTime? ReviewedAt     { get; set; }
        public DateTime  CreatedAt      { get; set; }
        // Context for the reviewer
        public string    MessageBody    { get; set; }
        public string    MessageStatus  { get; set; }
        public string    SenderType     { get; set; }
        public string    SenderName     { get; set; }
        public string    StudentName    { get; set; }
        public string    ClassName      { get; set; }
    }

    public class ResolveReportRequest
    {
        /// <summary>Removed = hide the message from both sides; Reviewed = no action.</summary>
        public string Action { get; set; }   // Removed | Reviewed
    }

    /// <summary>
    /// Everything the parent chat screen needs in one call: whether messaging is
    /// available for this child's class, who it reaches, and the conversation.
    /// </summary>
    public class ParentThreadViewDto
    {
        public bool         CanMessage    { get; set; }
        public string       Reason        { get; set; }   // set when CanMessage = false
        public List<string> Teachers      { get; set; }   // recipient display names
        public int?         ThreadId      { get; set; }   // null until the first message
        public string       Status        { get; set; }   // Active | Blocked
        public string       BlockedByType { get; set; }   // parent | teacher
        public List<MessageDto> Messages  { get; set; }
    }
}
