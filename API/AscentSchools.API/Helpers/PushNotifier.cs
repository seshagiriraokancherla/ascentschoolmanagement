using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.Mobile;
using Dapper;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AscentSchools.API.Helpers
{
    /// <summary>
    /// High-level push fan-out used by content-creation endpoints (announcements,
    /// homework, events, teacher announcements). Resolves the target parent tokens
    /// and sends via FCM. Fire-and-forget: runs on a background task and swallows all
    /// errors so a push failure never breaks the create.
    ///
    /// IMPORTANT: callers MUST pass primitive values captured on the request thread.
    /// Never read TenantContext/TeacherContext/HttpContext inside the Task — worker
    /// threads have no HttpContext (see decision: Phase 61 SMS Task.Run NRE).
    /// </summary>
    public class PushNotifier
    {
        private readonly TenantConnectionFactory   _db;
        private readonly DevicePushTokenRepository _tokens;
        private readonly FcmPushService            _fcm;

        public PushNotifier()
        {
            _db     = new TenantConnectionFactory();
            _tokens = new DevicePushTokenRepository(_db);
            _fcm    = new FcmPushService();
        }

        /// <summary>
        /// Notify the parents affected by a piece of content.
        /// classId == null  → school-wide (all parents of the branch).
        /// classId set       → that class; sectionId set narrows to one section.
        /// </summary>
        public void NotifyClass(string dbName, int groupId, int schoolId, int? classId, int? sectionId,
                                string title, string body, string type, int entityId)
        {
            if (!_fcm.IsConfigured) return;   // cheap check before spinning a task

            Task.Run(async () =>
            {
                try
                {
                    List<string> tokens;
                    if (classId == null || classId <= 0)
                        tokens = _tokens.GetSchoolParentTokens(dbName, groupId, schoolId);
                    else
                    {
                        var studentIds = GetCurrentYearStudentIds(dbName, schoolId, classId.Value, sectionId);
                        tokens = _tokens.GetParentTokensForStudents(dbName, groupId, studentIds);
                    }
                    if (tokens.Count == 0) return;

                    var data = new Dictionary<string, string>
                    {
                        { "type",     type ?? "" },
                        { "entityId", entityId.ToString() },
                    };
                    await _fcm.SendAsync(tokens, title, body, data);
                }
                catch { /* best-effort */ }
            });
        }

        /// <summary>
        /// Notify specific teacher logins — a parent sent a message to their class.
        /// userIds must be captured on the request thread (see class remarks).
        /// </summary>
        public void NotifyTeachers(int groupId, int schoolId, List<int> userIds,
                                   string title, string body, string type, int entityId)
        {
            if (!_fcm.IsConfigured) return;
            if (userIds == null || userIds.Count == 0) return;

            Task.Run(async () =>
            {
                try
                {
                    var tokens = _tokens.GetTokensForTeachers(groupId, schoolId, userIds);
                    if (tokens.Count == 0) return;
                    await _fcm.SendAsync(tokens, title, body, BuildData(type, entityId));
                }
                catch { /* best-effort */ }
            });
        }

        /// <summary>Notify one parent — a teacher replied to their message.</summary>
        public void NotifyParent(int groupId, int parentId, string title, string body,
                                 string type, int entityId)
        {
            if (!_fcm.IsConfigured) return;

            Task.Run(async () =>
            {
                try
                {
                    var tokens = _tokens.GetTokensForParent(groupId, parentId);
                    if (tokens.Count == 0) return;
                    await _fcm.SendAsync(tokens, title, body, BuildData(type, entityId));
                }
                catch { /* best-effort */ }
            });
        }

        private static Dictionary<string, string> BuildData(string type, int entityId) =>
            new Dictionary<string, string>
            {
                { "type",     type ?? "" },
                { "entityId", entityId.ToString() },
            };

        // Current-year, active students of a class (optionally one section).
        // Matches the year-scoping used by attendance/announcement reads so we
        // target only the live roster, not promoted/prior-year rows.
        private List<long> GetCurrentYearStudentIds(string dbName, int schoolId, int classId, int? sectionId)
        {
            using (var conn = _db.GetTenantConnection(dbName))
                return conn.Query<long>(
                    @"SELECT student_id FROM students
                      WHERE school_id = @schoolId
                        AND class_id  = @classId
                        AND (@sectionId IS NULL OR section_id = @sectionId)
                        AND status IN ('Active','Y')
                        AND academic_year_id = (
                            SELECT TOP 1 academic_year_id FROM academic_years
                            WHERE school_id = @schoolId AND status = 'Active'
                            ORDER BY academic_year_id DESC)",
                    new { schoolId, classId, sectionId }).ToList();
        }
    }
}
