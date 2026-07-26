using AscentSchools.API.Filters;
using AscentSchools.API.Helpers;
using AscentSchools.API.Middleware;
using AscentSchools.Core.DTOs.School.Messaging;
using AscentSchools.Core.Models;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.Mobile
{
    /// <summary>
    /// Teacher side of parent &lt;-&gt; teacher messaging. A teacher sees the threads of
    /// children in the classes they're assigned to (class_teacher_assignments).
    /// Several teachers may share a class — any of them can reply, and the thread is
    /// common to all of them.
    /// </summary>
    [RoutePrefix("mobile/teacher/messages")]
    [MobileTeacherAuth]
    public class MobileTeacherMessagesController : ApiController
    {
        private const int MaxBodyLength = 2000;

        private readonly MessagingRepository     _messages;
        private readonly TenantConnectionFactory _db;

        private TeacherContext Teacher => TeacherContext.Current;

        public MobileTeacherMessagesController()
        {
            _db       = new TenantConnectionFactory();
            _messages = new MessagingRepository(_db);
        }

        // ── GET /mobile/teacher/messages ──────────────────────────────────

        [HttpGet, Route("")]
        public HttpResponseMessage GetThreads()
            => Ok(_messages.GetThreadsForTeacher(Teacher.DbName, Teacher.SchoolId, Teacher.UserId));

        // ── GET /mobile/teacher/messages/{threadId} ───────────────────────

        [HttpGet, Route("{threadId:int}")]
        public HttpResponseMessage GetThread(int threadId)
        {
            if (!CanAccess(threadId))
                return Fail(HttpStatusCode.Forbidden, "This conversation isn't for one of your classes.");

            return Ok(new MessageThreadDetailDto
            {
                Thread   = _messages.GetThread(Teacher.DbName, Teacher.SchoolId, threadId, "teacher"),
                Messages = _messages.GetMessages(Teacher.DbName, Teacher.SchoolId, threadId).ToList(),
            });
        }

        // ── POST /mobile/teacher/messages/{threadId} — reply ──────────────

        [HttpPost, Route("{threadId:int}")]
        public HttpResponseMessage Reply(int threadId, [FromBody] SendMessageRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Body))
                return Fail(HttpStatusCode.BadRequest, "Message cannot be empty.");

            var body = request.Body.Trim();
            if (body.Length > MaxBodyLength)
                return Fail(HttpStatusCode.BadRequest, $"Message is too long (max {MaxBodyLength} characters).");

            if (!CanAccess(threadId))
                return Fail(HttpStatusCode.Forbidden, "This conversation isn't for one of your classes.");

            // Capture on the request thread — never read TeacherContext inside Task.Run (Phase 61).
            var dbName   = Teacher.DbName;
            var schoolId = Teacher.SchoolId;
            var groupId  = Teacher.GroupId;
            var fullName = Teacher.FullName;

            if (_messages.GetThreadStatus(dbName, schoolId, threadId) == "Blocked")
                return Fail(HttpStatusCode.Forbidden, "This conversation is blocked.");

            var thread = _messages.GetThread(dbName, schoolId, threadId, "teacher");
            if (thread == null) return Fail(HttpStatusCode.NotFound, "Conversation not found.");

            var messageId = _messages.SendMessage(dbName, schoolId, threadId,
                "teacher", Teacher.UserId, fullName, body);

            new PushNotifier().NotifyParent(dbName, groupId, schoolId, thread.ParentId,
                "Message from " + fullName,
                Truncate(body, 80),
                "message", threadId);

            return Request.CreateResponse(HttpStatusCode.Created,
                ApiResponse<object>.Ok(new { messageId, threadId }, "Reply sent."));
        }

        // ── POST /mobile/teacher/messages/{threadId}/read ─────────────────

        [HttpPost, Route("{threadId:int}/read")]
        public HttpResponseMessage MarkRead(int threadId)
        {
            if (!CanAccess(threadId))
                return Fail(HttpStatusCode.Forbidden, "This conversation isn't for one of your classes.");
            return Ok(_messages.MarkRead(Teacher.DbName, Teacher.SchoolId, threadId, "teacher"));
        }

        // ── POST /mobile/teacher/messages/{threadId}/report ───────────────
        // Play Store UGC requirement.

        [HttpPost, Route("{threadId:int}/report")]
        public HttpResponseMessage ReportMessage(int threadId, [FromBody] ReportMessageRequest request)
        {
            if (request == null || request.MessageId <= 0)
                return Fail(HttpStatusCode.BadRequest, "messageId is required.");
            if (!CanAccess(threadId))
                return Fail(HttpStatusCode.Forbidden, "This conversation isn't for one of your classes.");
            if (!_messages.MessageInThread(Teacher.DbName, Teacher.SchoolId, threadId, request.MessageId))
                return Fail(HttpStatusCode.NotFound, "Message not found.");

            _messages.ReportMessage(Teacher.DbName, Teacher.SchoolId, threadId, request.MessageId,
                "teacher", Teacher.UserId, request.Reason?.Trim());

            return Ok(true, "Reported. The school will review this message.");
        }

        // ── POST /mobile/teacher/messages/{threadId}/block | /unblock ─────

        [HttpPost, Route("{threadId:int}/block")]
        public HttpResponseMessage BlockThread(int threadId)
        {
            if (!CanAccess(threadId))
                return Fail(HttpStatusCode.Forbidden, "This conversation isn't for one of your classes.");
            _messages.SetThreadBlocked(Teacher.DbName, Teacher.SchoolId, threadId,
                true, "teacher", Teacher.UserId);
            return Ok(true, "Conversation blocked.");
        }

        [HttpPost, Route("{threadId:int}/unblock")]
        public HttpResponseMessage UnblockThread(int threadId)
        {
            if (!CanAccess(threadId))
                return Fail(HttpStatusCode.Forbidden, "This conversation isn't for one of your classes.");

            // A teacher may lift a school-side block, but not one the parent set.
            var blocker = _messages.GetThreadBlocker(Teacher.DbName, Teacher.SchoolId, threadId);
            if (blocker.ByType == "parent")
                return Fail(HttpStatusCode.Forbidden, "This conversation was blocked by the parent.");

            _messages.SetThreadBlocked(Teacher.DbName, Teacher.SchoolId, threadId, false, null, 0);
            return Ok(true, "Conversation unblocked.");
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private bool CanAccess(int threadId) =>
            _messages.TeacherCanAccessThread(Teacher.DbName, Teacher.SchoolId, Teacher.UserId, threadId);

        private static string Truncate(string s, int max) =>
            string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max) + "…";

        private HttpResponseMessage Ok<T>(T data, string message = null) =>
            Request.CreateResponse(HttpStatusCode.OK, ApiResponse<T>.Ok(data, message));

        private HttpResponseMessage Fail(HttpStatusCode code, string message) =>
            Request.CreateResponse(code, new { success = false, message });
    }
}
