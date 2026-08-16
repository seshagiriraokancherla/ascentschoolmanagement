using AscentSchools.Core.DTOs.School.Messaging;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System.Collections.Generic;
using System.Linq;

namespace AscentSchools.Data.Repositories.School
{
    /// <summary>
    /// Parent &lt;-&gt; teacher messaging.
    /// Threads key on student_unique_id (stable across promotions). Which teachers
    /// can see a thread is resolved LIVE from the student's CURRENT-year class+section
    /// via class_teacher_assignments, so a promoted child's thread follows them to
    /// their new class teacher with no data migration.
    /// </summary>
    public class MessagingRepository
    {
        private readonly IConnectionFactory _db;
        public MessagingRepository(IConnectionFactory db) { _db = db; }

        // The school's current academic year — same subquery used by attendance (Phase 66/67).
        private const string CurrentYear = @"
            (SELECT TOP 1 academic_year_id FROM academic_years
             WHERE school_id = @schoolId AND status = 'Active'
             ORDER BY academic_year_id DESC)";

        // Server runs US Eastern (Phase 98) — every server-stamped message time must be IST.
        private const string IstNow =
            "CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'India Standard Time' AS DATETIME)";

        // ── Parent side ───────────────────────────────────────────────────

        /// <summary>
        /// Teachers a parent's message would reach, for the child's current class+section.
        /// Empty = messaging isn't set up for that class yet.
        /// </summary>
        public IEnumerable<string> GetRecipientTeacherNames(string tenantDbName, int schoolId, long studentId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<string>(
                    $@"SELECT DISTINCT u.full_name
                       FROM students s
                       JOIN class_teacher_assignments a
                            ON a.class_id = s.class_id
                           AND a.school_id = s.school_id
                           AND a.academic_year_id = s.academic_year_id
                           AND (a.section_id IS NULL OR a.section_id = s.section_id)
                       JOIN users u ON u.user_id = a.user_id AND u.status = 'Active'
                       WHERE s.student_id = @studentId AND s.school_id = @schoolId
                         AND s.academic_year_id = {CurrentYear}
                       ORDER BY u.full_name",
                    new { studentId, schoolId });
        }

        /// <summary>
        /// A student's current-year placement (year / class / section / stable id) —
        /// everything routing needs, in one lookup. Null when the student row isn't
        /// in the current academic year (e.g. not yet promoted).
        /// </summary>
        public StudentClassContextDto GetStudentClassContext(string tenantDbName, int schoolId, long studentId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QueryFirstOrDefault<StudentClassContextDto>(
                    @"SELECT academic_year_id AcademicYearId, class_id ClassId,
                             section_id SectionId, student_unique_id StudentUniqueId
                      FROM students
                      WHERE student_id = @studentId AND school_id = @schoolId",
                    new { studentId, schoolId });
        }

        /// <summary>The thread id for a child+parent, or null when none exists yet (no insert).</summary>
        public int? FindThreadId(string tenantDbName, int schoolId, int studentUniqueId, int parentId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QueryFirstOrDefault<int?>(
                    @"SELECT thread_id FROM message_threads
                      WHERE school_id = @schoolId AND student_unique_id = @studentUniqueId
                        AND parent_id = @parentId",
                    new { schoolId, studentUniqueId, parentId });
        }

        /// <summary>The child's thread, creating it on first use. Returns the thread id.</summary>
        public int GetOrCreateThread(string tenantDbName, int schoolId, int studentUniqueId, int parentId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QuerySingle<int>(
                    $@"DECLARE @existing INT =
                          (SELECT thread_id FROM message_threads
                           WHERE school_id = @schoolId AND student_unique_id = @studentUniqueId
                             AND parent_id = @parentId);
                      IF @existing IS NOT NULL
                          SELECT @existing;
                      ELSE
                      BEGIN
                          INSERT INTO message_threads (student_unique_id, parent_id, school_id, created_at)
                          VALUES (@studentUniqueId, @parentId, @schoolId, {IstNow});
                          SELECT CAST(SCOPE_IDENTITY() AS INT);
                      END",
                    new { schoolId, studentUniqueId, parentId });
        }

        // ── Teacher side ──────────────────────────────────────────────────

        /// <summary>
        /// Threads for children in the classes this teacher is assigned to.
        /// DISTINCT because a teacher may hold both a class-wide and a
        /// section-specific assignment that both match the same student.
        /// </summary>
        public IEnumerable<MessageThreadDto> GetThreadsForTeacher(string tenantDbName, int schoolId, int userId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<MessageThreadDto>(
                    $@"SELECT DISTINCT
                              t.thread_id ThreadId, t.student_unique_id StudentUniqueId,
                              t.parent_id ParentId, t.status Status,
                              t.blocked_by_type BlockedByType, t.blocked_at BlockedAt,
                              t.last_message_at LastMessageAt, t.created_at CreatedAt,
                              s.student_name StudentName, s.admission_no AdmissionNo,
                              c.class_name ClassName, sec.section_name SectionName,
                              (SELECT TOP 1 m.body FROM messages m
                               WHERE m.thread_id = t.thread_id AND m.status = 'Active'
                               ORDER BY m.created_at DESC) LastMessageBody,
                              (SELECT COUNT(1) FROM messages m
                               WHERE m.thread_id = t.thread_id AND m.sender_type = 'parent'
                                 AND m.status = 'Active' AND m.read_at IS NULL) UnreadCount
                       FROM message_threads t
                       JOIN students s
                            ON s.student_unique_id = t.student_unique_id
                           AND s.school_id = t.school_id
                           AND s.academic_year_id = {CurrentYear}
                       JOIN class_teacher_assignments a
                            ON a.class_id = s.class_id
                           AND a.school_id = s.school_id
                           AND a.academic_year_id = s.academic_year_id
                           AND (a.section_id IS NULL OR a.section_id = s.section_id)
                       JOIN classes c        ON c.class_id   = s.class_id
                       LEFT JOIN sections sec ON sec.section_id = s.section_id
                       WHERE t.school_id = @schoolId AND a.user_id = @userId
                         AND t.last_message_at IS NOT NULL
                       ORDER BY t.last_message_at DESC",
                    new { schoolId, userId });
        }

        /// <summary>Guards every teacher thread operation — is this thread in their classes?</summary>
        public bool TeacherCanAccessThread(string tenantDbName, int schoolId, int userId, int threadId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.ExecuteScalar<int>(
                    $@"SELECT COUNT(1)
                       FROM message_threads t
                       JOIN students s
                            ON s.student_unique_id = t.student_unique_id
                           AND s.school_id = t.school_id
                           AND s.academic_year_id = {CurrentYear}
                       JOIN class_teacher_assignments a
                            ON a.class_id = s.class_id
                           AND a.school_id = s.school_id
                           AND a.academic_year_id = s.academic_year_id
                           AND (a.section_id IS NULL OR a.section_id = s.section_id)
                       WHERE t.thread_id = @threadId AND t.school_id = @schoolId AND a.user_id = @userId",
                    new { threadId, schoolId, userId }) > 0;
        }

        // ── Admin conversation viewer (school web app, read-only) ──────────

        /// <summary>
        /// Threads filtered for the admin Conversations page. Class/section are resolved
        /// from the student's placement in the SELECTED academic year (null → current year),
        /// so a year filter shows a promoted child under the class they were in that year;
        /// a thread whose student isn't placed in the selected year won't appear (INNER JOIN).
        /// The date range matches threads that had ANY message activity in the window.
        /// </summary>
        public IEnumerable<ConversationListItemDto> GetThreadsForAdmin(
            string tenantDbName, int schoolId, int? academicYearId,
            int? classId, int? sectionId, int? studentUniqueId, int? teacherUserId,
            System.DateTime dateFrom, System.DateTime dateToExclusive)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<ConversationListItemDto>(
                    $@"SELECT DISTINCT
                              t.thread_id ThreadId, t.student_unique_id StudentUniqueId,
                              t.status Status, t.last_message_at LastMessageAt,
                              s.student_name StudentName, s.admission_no AdmissionNo,
                              c.class_name ClassName, sec.section_name SectionName,
                              (SELECT TOP 1 m.body FROM messages m
                               WHERE m.thread_id = t.thread_id AND m.status = 'Active'
                               ORDER BY m.created_at DESC) LastMessageBody,
                              (SELECT COUNT(1) FROM messages m WHERE m.thread_id = t.thread_id) MessageCount,
                              STUFF((SELECT DISTINCT ', ' + u.full_name
                                     FROM class_teacher_assignments a
                                     JOIN users u ON u.user_id = a.user_id AND u.status = 'Active'
                                     WHERE a.class_id = s.class_id AND a.school_id = s.school_id
                                       AND a.academic_year_id = s.academic_year_id
                                       AND (a.section_id IS NULL OR a.section_id = s.section_id)
                                     FOR XML PATH('')), 1, 2, '') TeacherNames
                       FROM message_threads t
                       JOIN students s
                            ON s.student_unique_id = t.student_unique_id
                           AND s.school_id = t.school_id
                           AND s.academic_year_id = ISNULL(@academicYearId, {CurrentYear})
                       LEFT JOIN classes c      ON c.class_id     = s.class_id
                       LEFT JOIN sections sec   ON sec.section_id = s.section_id
                       WHERE t.school_id = @schoolId
                         AND (@classId IS NULL OR s.class_id = @classId)
                         AND (@sectionId IS NULL OR s.section_id = @sectionId)
                         AND (@studentUniqueId IS NULL OR t.student_unique_id = @studentUniqueId)
                         AND (@teacherUserId IS NULL OR EXISTS (
                                 SELECT 1 FROM class_teacher_assignments a2
                                 WHERE a2.class_id = s.class_id AND a2.school_id = s.school_id
                                   AND a2.academic_year_id = s.academic_year_id
                                   AND (a2.section_id IS NULL OR a2.section_id = s.section_id)
                                   AND a2.user_id = @teacherUserId))
                         AND EXISTS (SELECT 1 FROM messages m
                                     WHERE m.thread_id = t.thread_id
                                       AND m.created_at >= @dateFrom AND m.created_at < @dateToExclusive)
                       ORDER BY t.last_message_at DESC",
                    new { schoolId, academicYearId, classId, sectionId, studentUniqueId,
                          teacherUserId, dateFrom, dateToExclusive });
        }

        // ── Shared ────────────────────────────────────────────────────────

        public MessageThreadDto GetThread(string tenantDbName, int schoolId, int threadId, string forSide)
        {
            // Unread for the caller = messages the OTHER side sent that they haven't read.
            var otherSide = forSide == "parent" ? "teacher" : "parent";
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QueryFirstOrDefault<MessageThreadDto>(
                    $@"SELECT t.thread_id ThreadId, t.student_unique_id StudentUniqueId,
                              t.parent_id ParentId, t.status Status,
                              t.blocked_by_type BlockedByType, t.blocked_at BlockedAt,
                              t.last_message_at LastMessageAt, t.created_at CreatedAt,
                              s.student_name StudentName, s.admission_no AdmissionNo,
                              c.class_name ClassName, sec.section_name SectionName,
                              (SELECT COUNT(1) FROM messages m
                               WHERE m.thread_id = t.thread_id AND m.sender_type = @otherSide
                                 AND m.status = 'Active' AND m.read_at IS NULL) UnreadCount
                       FROM message_threads t
                       LEFT JOIN students s
                            ON s.student_unique_id = t.student_unique_id
                           AND s.school_id = t.school_id
                           AND s.academic_year_id = {CurrentYear}
                       LEFT JOIN classes c      ON c.class_id     = s.class_id
                       LEFT JOIN sections sec   ON sec.section_id = s.section_id
                       WHERE t.thread_id = @threadId AND t.school_id = @schoolId",
                    new { threadId, schoolId, otherSide });
        }

        public IEnumerable<MessageDto> GetMessages(string tenantDbName, int schoolId, int threadId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<MessageDto>(
                    @"SELECT message_id MessageId, thread_id ThreadId, sender_type SenderType,
                             sender_id SenderId, sender_name SenderName, body Body,
                             status Status, read_at ReadAt, created_at CreatedAt
                      FROM messages
                      WHERE thread_id = @threadId AND school_id = @schoolId
                      ORDER BY created_at",
                    new { threadId, schoolId });
        }

        /// <summary>Thread status — Active | Blocked. Null when the thread doesn't exist.</summary>
        public string GetThreadStatus(string tenantDbName, int schoolId, int threadId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QueryFirstOrDefault<string>(
                    @"SELECT status FROM message_threads
                      WHERE thread_id = @threadId AND school_id = @schoolId",
                    new { threadId, schoolId });
        }

        public int SendMessage(string tenantDbName, int schoolId, int threadId,
            string senderType, int senderId, string senderName, string body)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QuerySingle<int>(
                    $@"INSERT INTO messages (thread_id, sender_type, sender_id, sender_name, body, school_id, created_at)
                      VALUES (@threadId, @senderType, @senderId, @senderName, @body, @schoolId, {IstNow});
                      DECLARE @newId INT = CAST(SCOPE_IDENTITY() AS INT);
                      UPDATE message_threads SET last_message_at = {IstNow}
                      WHERE thread_id = @threadId AND school_id = @schoolId;
                      SELECT @newId;",
                    new { threadId, senderType, senderId, senderName, body, schoolId });
        }

        /// <summary>Marks the OTHER side's messages read. readerSide = the caller.</summary>
        public int MarkRead(string tenantDbName, int schoolId, int threadId, string readerSide)
        {
            var otherSide = readerSide == "parent" ? "teacher" : "parent";
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Execute(
                    $@"UPDATE messages SET read_at = {IstNow}
                      WHERE thread_id = @threadId AND school_id = @schoolId
                        AND sender_type = @otherSide AND read_at IS NULL",
                    new { threadId, schoolId, otherSide });
        }

        // ── UGC: block + report (Play Store requirements) ──────────────────

        public void SetThreadBlocked(string tenantDbName, int schoolId, int threadId,
            bool blocked, string byType, int byId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    $@"UPDATE message_threads
                      SET status          = CASE WHEN @blocked = 1 THEN 'Blocked' ELSE 'Active' END,
                          blocked_by_type = CASE WHEN @blocked = 1 THEN @byType ELSE NULL END,
                          blocked_by_id   = CASE WHEN @blocked = 1 THEN @byId   ELSE NULL END,
                          blocked_at      = CASE WHEN @blocked = 1 THEN {IstNow} ELSE NULL END
                      WHERE thread_id = @threadId AND school_id = @schoolId",
                    new { threadId, schoolId, blocked, byType, byId });
        }

        /// <summary>Who blocked the thread — only they (or an admin) may unblock it.</summary>
        public ThreadBlocker GetThreadBlocker(string tenantDbName, int schoolId, int threadId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QueryFirstOrDefault<ThreadBlocker>(
                    @"SELECT blocked_by_type ByType, blocked_by_id ById FROM message_threads
                      WHERE thread_id = @threadId AND school_id = @schoolId",
                    new { threadId, schoolId }) ?? new ThreadBlocker();
        }

        /// <summary>Data-layer row shape for <see cref="GetThreadBlocker"/>.</summary>
        public class ThreadBlocker
        {
            public string ByType { get; set; }
            public int?   ById   { get; set; }
        }

        public bool MessageInThread(string tenantDbName, int schoolId, int threadId, int messageId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.ExecuteScalar<int>(
                    @"SELECT COUNT(1) FROM messages
                      WHERE message_id = @messageId AND thread_id = @threadId AND school_id = @schoolId",
                    new { messageId, threadId, schoolId }) > 0;
        }

        public int ReportMessage(string tenantDbName, int schoolId, int threadId, int messageId,
            string byType, int byId, string reason)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QuerySingle<int>(
                    $@"INSERT INTO message_reports
                        (message_id, thread_id, reported_by_type, reported_by_id, reason, school_id, created_at)
                      VALUES (@messageId, @threadId, @byType, @byId, @reason, @schoolId, {IstNow});
                      SELECT CAST(SCOPE_IDENTITY() AS INT)",
                    new { messageId, threadId, byType, byId, reason, schoolId });
        }

        // ── Admin review (school web app) ──────────────────────────────────

        public IEnumerable<MessageReportDto> GetReports(string tenantDbName, int schoolId, string status)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<MessageReportDto>(
                    $@"SELECT r.report_id ReportId, r.message_id MessageId, r.thread_id ThreadId,
                              r.reported_by_type ReportedByType, r.reported_by_id ReportedById,
                              r.reason Reason, r.status Status, r.reviewed_by ReviewedBy,
                              r.reviewed_at ReviewedAt, r.created_at CreatedAt,
                              m.body MessageBody, m.status MessageStatus,
                              m.sender_type SenderType, m.sender_name SenderName,
                              s.student_name StudentName, c.class_name ClassName
                       FROM message_reports r
                       JOIN messages m       ON m.message_id = r.message_id
                       JOIN message_threads t ON t.thread_id = r.thread_id
                       LEFT JOIN students s
                            ON s.student_unique_id = t.student_unique_id
                           AND s.school_id = t.school_id
                           AND s.academic_year_id = {CurrentYear}
                       LEFT JOIN classes c ON c.class_id = s.class_id
                       WHERE r.school_id = @schoolId
                         AND (@status IS NULL OR r.status = @status)
                       ORDER BY r.created_at DESC",
                    new { schoolId, status });
        }

        /// <summary>Resolve a report. 'Removed' also hides the message from both sides.</summary>
        public void ResolveReport(string tenantDbName, int schoolId, int reportId,
            string action, string reviewedBy)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    $@"UPDATE message_reports
                      SET status = @action, reviewed_by = @reviewedBy, reviewed_at = {IstNow}
                      WHERE report_id = @reportId AND school_id = @schoolId;

                      IF @action = 'Removed'
                          UPDATE messages SET status = 'Removed'
                          WHERE message_id = (SELECT message_id FROM message_reports
                                              WHERE report_id = @reportId AND school_id = @schoolId)
                            AND school_id = @schoolId;",
                    new { reportId, schoolId, action, reviewedBy });
        }

        public int CountOpenReports(string tenantDbName, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.ExecuteScalar<int>(
                    @"SELECT COUNT(1) FROM message_reports
                      WHERE school_id = @schoolId AND status = 'Open'",
                    new { schoolId });
        }
    }
}
