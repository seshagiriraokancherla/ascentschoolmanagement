using AscentSchools.API.Filters;
using AscentSchools.API.Helpers;
using AscentSchools.API.Middleware;
using AscentSchools.Core.DTOs.School.Messaging;
using AscentSchools.Core.Models;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.Mobile
{
    /// <summary>
    /// Parent side of parent &lt;-&gt; teacher messaging. Scoped to the selected child,
    /// so a parent has exactly one thread here — no thread list.
    /// Messages reach whichever teachers are assigned to the child's current
    /// class+section; with none assigned, sending is refused with a clear reason.
    /// </summary>
    [RoutePrefix("mobile/messages")]
    [MobileAuth(requireChildContext: true)]
    public class MobileMessagesController : ApiController
    {
        private const int MaxBodyLength = 2000;
        private const string NoTeacherReason =
            "Messaging isn't set up for your class yet. Please contact the school office.";

        private readonly MessagingRepository     _messages;
        private readonly ClassTeacherRepository  _classTeachers;
        private readonly TenantConnectionFactory _db;

        private MobileContext Mobile => MobileContext.Current;

        public MobileMessagesController()
        {
            _db            = new TenantConnectionFactory();
            _messages      = new MessagingRepository(_db);
            _classTeachers = new ClassTeacherRepository(_db);
        }

        // ── GET /mobile/messages ──────────────────────────────────────────
        // The child's conversation + who it reaches. Does NOT create a thread.

        [HttpGet, Route("")]
        public HttpResponseMessage GetThread()
        {
            var view = new ParentThreadViewDto
            {
                Teachers = new List<string>(),
                Messages = new List<MessageDto>(),
                Status   = "Active",
            };

            var ctx = _messages.GetStudentClassContext(Mobile.DbName, Mobile.SchoolId, Mobile.StudentId);
            if (ctx == null)
            {
                view.CanMessage = false;
                view.Reason     = NoTeacherReason;
                return Ok(view);
            }

            view.Teachers = _messages
                .GetRecipientTeacherNames(Mobile.DbName, Mobile.SchoolId, Mobile.StudentId)
                .ToList();

            if (view.Teachers.Count == 0)
            {
                view.CanMessage = false;
                view.Reason     = NoTeacherReason;
                return Ok(view);
            }
            view.CanMessage = true;

            var threadId = _messages.FindThreadId(
                Mobile.DbName, Mobile.SchoolId, ctx.StudentUniqueId, Mobile.ParentId);
            if (threadId == null) return Ok(view);   // nothing said yet

            var thread = _messages.GetThread(Mobile.DbName, Mobile.SchoolId, threadId.Value, "parent");
            view.ThreadId      = thread.ThreadId;
            view.Status        = thread.Status;
            view.BlockedByType = thread.BlockedByType;
            view.Messages      = _messages
                .GetMessages(Mobile.DbName, Mobile.SchoolId, thread.ThreadId).ToList();

            if (thread.Status == "Blocked")
            {
                view.CanMessage = false;
                view.Reason     = BlockedReason(thread.BlockedByType);
            }

            return Ok(view);
        }

        // ── POST /mobile/messages ─────────────────────────────────────────

        [HttpPost, Route("")]
        public HttpResponseMessage SendMessage([FromBody] SendMessageRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Body))
                return Fail(HttpStatusCode.BadRequest, "Message cannot be empty.");

            var body = request.Body.Trim();
            if (body.Length > MaxBodyLength)
                return Fail(HttpStatusCode.BadRequest, $"Message is too long (max {MaxBodyLength} characters).");

            // Capture primitives on the request thread — never read MobileContext
            // inside the push Task.Run (Phase 61 NRE rule).
            var dbName      = Mobile.DbName;
            var schoolId    = Mobile.SchoolId;
            var groupId     = Mobile.GroupId;
            var parentId    = Mobile.ParentId;
            var displayName = Mobile.DisplayName;

            var ctx = _messages.GetStudentClassContext(dbName, schoolId, Mobile.StudentId);
            if (ctx == null)
                return Fail(HttpStatusCode.BadRequest, "Student record not found.");

            var recipients = _classTeachers
                .GetTeachersForClass(dbName, schoolId, ctx.AcademicYearId, ctx.ClassId, ctx.SectionId)
                .ToList();
            if (recipients.Count == 0)
                return Fail(HttpStatusCode.BadRequest, NoTeacherReason);

            var threadId = _messages.GetOrCreateThread(dbName, schoolId, ctx.StudentUniqueId, parentId);

            if (_messages.GetThreadStatus(dbName, schoolId, threadId) == "Blocked")
                return Fail(HttpStatusCode.Forbidden, "This conversation is blocked.");

            var messageId = _messages.SendMessage(dbName, schoolId, threadId,
                "parent", parentId, displayName, body);

            new PushNotifier().NotifyTeachers(groupId, schoolId,
                recipients.Select(t => t.UserId).ToList(),
                "Message from a parent",
                $"{displayName}: {Truncate(body, 80)}",
                "message", threadId);

            return Request.CreateResponse(HttpStatusCode.Created,
                ApiResponse<object>.Ok(new { messageId, threadId }, "Message sent."));
        }

        // ── POST /mobile/messages/read ────────────────────────────────────

        [HttpPost, Route("read")]
        public HttpResponseMessage MarkRead()
        {
            var threadId = CurrentThreadId();
            if (threadId == null) return Ok(0);
            return Ok(_messages.MarkRead(Mobile.DbName, Mobile.SchoolId, threadId.Value, "parent"));
        }

        // ── POST /mobile/messages/report ──────────────────────────────────
        // Play Store UGC requirement.

        [HttpPost, Route("report")]
        public HttpResponseMessage ReportMessage([FromBody] ReportMessageRequest request)
        {
            if (request == null || request.MessageId <= 0)
                return Fail(HttpStatusCode.BadRequest, "messageId is required.");

            var threadId = CurrentThreadId();
            if (threadId == null) return Fail(HttpStatusCode.NotFound, "Conversation not found.");
            if (!_messages.MessageInThread(Mobile.DbName, Mobile.SchoolId, threadId.Value, request.MessageId))
                return Fail(HttpStatusCode.NotFound, "Message not found.");

            _messages.ReportMessage(Mobile.DbName, Mobile.SchoolId, threadId.Value, request.MessageId,
                "parent", Mobile.ParentId, request.Reason?.Trim());

            return Ok(true, "Reported. The school will review this message.");
        }

        // ── POST /mobile/messages/block | /unblock ────────────────────────

        [HttpPost, Route("block")]
        public HttpResponseMessage BlockThread()
        {
            var threadId = CurrentThreadId();
            if (threadId == null) return Fail(HttpStatusCode.NotFound, "Conversation not found.");
            _messages.SetThreadBlocked(Mobile.DbName, Mobile.SchoolId, threadId.Value,
                true, "parent", Mobile.ParentId);
            return Ok(true, "Conversation blocked.");
        }

        [HttpPost, Route("unblock")]
        public HttpResponseMessage UnblockThread()
        {
            var threadId = CurrentThreadId();
            if (threadId == null) return Fail(HttpStatusCode.NotFound, "Conversation not found.");

            // Only the side that blocked may lift it — otherwise blocking means nothing.
            var blocker = _messages.GetThreadBlocker(Mobile.DbName, Mobile.SchoolId, threadId.Value);
            if (blocker.ByType != null && blocker.ByType != "parent")
                return Fail(HttpStatusCode.Forbidden, "This conversation was blocked by the school.");

            _messages.SetThreadBlocked(Mobile.DbName, Mobile.SchoolId, threadId.Value, false, null, 0);
            return Ok(true, "Conversation unblocked.");
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private int? CurrentThreadId()
        {
            var ctx = _messages.GetStudentClassContext(Mobile.DbName, Mobile.SchoolId, Mobile.StudentId);
            if (ctx == null) return null;
            return _messages.FindThreadId(Mobile.DbName, Mobile.SchoolId, ctx.StudentUniqueId, Mobile.ParentId);
        }

        private static string BlockedReason(string byType) =>
            byType == "parent"
                ? "You blocked this conversation. Unblock it to send messages."
                : "This conversation has been blocked by the school.";

        private static string Truncate(string s, int max) =>
            string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max) + "…";

        private HttpResponseMessage Ok<T>(T data, string message = null) =>
            Request.CreateResponse(HttpStatusCode.OK, ApiResponse<T>.Ok(data, message));

        private HttpResponseMessage Fail(HttpStatusCode code, string message) =>
            Request.CreateResponse(code, new { success = false, message });
    }
}
