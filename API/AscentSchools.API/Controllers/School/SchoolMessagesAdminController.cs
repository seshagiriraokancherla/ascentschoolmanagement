using AscentSchools.API.Filters;
using AscentSchools.API.Helpers;
using AscentSchools.Core.Constants;
using AscentSchools.Core.DTOs.School.Messaging;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    /// <summary>
    /// Read-only admin view of parent↔teacher conversations (school web app).
    /// Gated by MESSAGES.VIEW so it can be granted per role. View-only — no send,
    /// remove or block here (moderation stays in Settings → Reported Messages).
    /// </summary>
    [RoutePrefix("school/messages")]
    public class SchoolMessagesAdminController : BaseSchoolController
    {
        private readonly MessagingRepository _repo;

        public SchoolMessagesAdminController()
        {
            _repo = new MessagingRepository(new TenantConnectionFactory());
        }

        // GET school/messages/threads?academicYearId=&classId=&sectionId=&studentUniqueId=&teacherUserId=&dateFrom=&dateTo=
        // Defaults to the last 5 days when the date range is omitted.
        [HttpGet, Route("threads")]
        [RequirePermission(PermissionCodes.Messages.View)]
        public HttpResponseMessage GetThreads(
            [FromUri] int? academicYearId = null,
            [FromUri] int? classId = null,
            [FromUri] int? sectionId = null,
            [FromUri] int? studentUniqueId = null,
            [FromUri] int? teacherUserId = null,
            [FromUri] DateTime? dateFrom = null,
            [FromUri] DateTime? dateTo = null)
        {
            // Default window is the last 5 days in IST (server runs US Eastern — Phase 98).
            var from = (dateFrom ?? TimeHelper.IstToday().AddDays(-5)).Date;
            // Inclusive of the whole `dateTo` day → compare against the next day exclusively.
            var toExclusive = (dateTo ?? TimeHelper.IstToday()).Date.AddDays(1);

            return Ok(_repo.GetThreadsForAdmin(
                Tenant.TenantDbName, Tenant.SchoolId, academicYearId,
                classId, sectionId, studentUniqueId, teacherUserId, from, toExclusive));
        }

        // GET school/messages/threads/{threadId} — the full conversation, read-only.
        [HttpGet, Route("threads/{threadId:int}")]
        [RequirePermission(PermissionCodes.Messages.View)]
        public HttpResponseMessage GetThread(int threadId)
        {
            var thread = _repo.GetThread(Tenant.TenantDbName, Tenant.SchoolId, threadId, "teacher");
            if (thread == null)
                return NotFound("Conversation not found.");

            var messages = _repo.GetMessages(Tenant.TenantDbName, Tenant.SchoolId, threadId);
            return Ok(new MessageThreadDetailDto
            {
                Thread   = thread,
                Messages = new System.Collections.Generic.List<MessageDto>(messages)
            });
        }
    }
}
