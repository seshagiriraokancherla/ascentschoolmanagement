using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System.Collections.Generic;
using System.Linq;

namespace AscentSchools.Data.Repositories.Mobile
{
    /// <summary>
    /// FCM device-token storage + push-target resolution.
    /// device_push_tokens lives in the MASTER DB (like parent_children), so a
    /// single query joins parent tokens to the children they own.
    /// </summary>
    public class DevicePushTokenRepository
    {
        private readonly IConnectionFactory _db;
        public DevicePushTokenRepository(IConnectionFactory db) { _db = db; }

        // ── Registration (upsert by fcm_token — a device re-registers / switches user) ──

        public void UpsertParentToken(string fcmToken, int parentId, int groupId, int schoolId,
                                      string dbName, string applicationId, string platform)
        {
            using (var conn = _db.GetMasterConnection())
                conn.Execute(
                    @"MERGE device_push_tokens AS t
                      USING (SELECT @fcmToken AS fcm_token) AS s ON t.fcm_token = s.fcm_token
                      WHEN MATCHED THEN UPDATE SET
                          user_type='parent', parent_id=@parentId, teacher_user_id=NULL,
                          group_id=@groupId, school_id=@schoolId, db_name=@dbName,
                          application_id=@applicationId, platform=@platform, updated_at=GETDATE()
                      WHEN NOT MATCHED THEN INSERT
                          (fcm_token, user_type, parent_id, group_id, school_id, db_name, application_id, platform)
                          VALUES (@fcmToken,'parent',@parentId,@groupId,@schoolId,@dbName,@applicationId,@platform);",
                    new { fcmToken, parentId, groupId, schoolId, dbName, applicationId,
                          platform = string.IsNullOrEmpty(platform) ? "android" : platform });
        }

        public void UpsertTeacherToken(string fcmToken, int teacherUserId, int groupId, int schoolId,
                                       string dbName, string applicationId, string platform)
        {
            using (var conn = _db.GetMasterConnection())
                conn.Execute(
                    @"MERGE device_push_tokens AS t
                      USING (SELECT @fcmToken AS fcm_token) AS s ON t.fcm_token = s.fcm_token
                      WHEN MATCHED THEN UPDATE SET
                          user_type='teacher', teacher_user_id=@teacherUserId, parent_id=NULL,
                          group_id=@groupId, school_id=@schoolId, db_name=@dbName,
                          application_id=@applicationId, platform=@platform, updated_at=GETDATE()
                      WHEN NOT MATCHED THEN INSERT
                          (fcm_token, user_type, teacher_user_id, group_id, school_id, db_name, application_id, platform)
                          VALUES (@fcmToken,'teacher',@teacherUserId,@groupId,@schoolId,@dbName,@applicationId,@platform);",
                    new { fcmToken, teacherUserId, groupId, schoolId, dbName, applicationId,
                          platform = string.IsNullOrEmpty(platform) ? "android" : platform });
        }

        public void DeleteToken(string fcmToken)
        {
            using (var conn = _db.GetMasterConnection())
                conn.Execute("DELETE FROM device_push_tokens WHERE fcm_token = @fcmToken", new { fcmToken });
        }

        // Remove tokens the gateway reported as stale (UNREGISTERED / invalid).
        public void DeleteTokens(IEnumerable<string> fcmTokens)
        {
            var list = fcmTokens?.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList();
            if (list == null || list.Count == 0) return;
            using (var conn = _db.GetMasterConnection())
                conn.Execute("DELETE FROM device_push_tokens WHERE fcm_token IN @list", new { list });
        }

        // ── Target resolution (parents) ────────────────────────────────────────

        /// <summary>Parent device tokens for a given set of students (by tenant student_id).</summary>
        // The dpt.* scoping matters: a master parent_id is shared across every school the
        // parent has a child in (parent_accounts is a single master row), and the same parent
        // may have several app installs (the generic CHAK IN app + baked per-school flavors),
        // each a separate fcm_token. A token is scoped to the school currently selected in that
        // app (UpsertParentToken stores group/school/db from the parent's active child). Without
        // this filter every install of that parent receives the push regardless of which school
        // it's viewing — the cause of "5 notifications on one device". (Phase 100)
        public List<string> GetParentTokensForStudents(string dbName, int groupId, int schoolId, IEnumerable<long> studentIds)
        {
            var ids = studentIds?.Distinct().ToList();
            if (ids == null || ids.Count == 0) return new List<string>();
            using (var conn = _db.GetMasterConnection())
                return conn.Query<string>(
                    @"SELECT DISTINCT dpt.fcm_token
                      FROM parent_children pc
                      JOIN device_push_tokens dpt
                        ON dpt.parent_id = pc.parent_id AND dpt.user_type = 'parent'
                       AND dpt.db_name = @dbName AND dpt.group_id = @groupId AND dpt.school_id = @schoolId
                      WHERE pc.db_name = @dbName AND pc.group_id = @groupId AND pc.is_active = 1
                        AND pc.student_id IN @ids",
                    new { dbName, groupId, schoolId, ids }).ToList();
        }

        /// <summary>Every parent device token for a school branch (school-wide announcements/events).</summary>
        // See GetParentTokensForStudents — the dpt.* filter keeps a school-wide announcement from
        // reaching a device whose app is currently logged into a different school.
        public List<string> GetSchoolParentTokens(string dbName, int groupId, int schoolId)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.Query<string>(
                    @"SELECT DISTINCT dpt.fcm_token
                      FROM parent_children pc
                      JOIN device_push_tokens dpt
                        ON dpt.parent_id = pc.parent_id AND dpt.user_type = 'parent'
                       AND dpt.db_name = @dbName AND dpt.group_id = @groupId AND dpt.school_id = @schoolId
                      WHERE pc.db_name = @dbName AND pc.group_id = @groupId AND pc.is_active = 1
                        AND pc.school_id = @schoolId",
                    new { dbName, groupId, schoolId }).ToList();
        }

        /// <summary>Device tokens of one parent (a teacher replying to their message).</summary>
        // Scoped to the school of the conversation so the reply only lands on the app install
        // currently viewing this school (same shared-parent_id reasoning as above).
        public List<string> GetTokensForParent(string dbName, int groupId, int schoolId, int parentId)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.Query<string>(
                    @"SELECT DISTINCT fcm_token FROM device_push_tokens
                      WHERE user_type = 'parent' AND parent_id = @parentId
                        AND db_name = @dbName AND group_id = @groupId AND school_id = @schoolId",
                    new { dbName, groupId, schoolId, parentId }).ToList();
        }

        // ── Target resolution (teachers) ───────────────────────────────────────

        /// <summary>Device tokens of specific teacher logins (a parent messaging their class teachers).</summary>
        public List<string> GetTokensForTeachers(int groupId, int schoolId, IEnumerable<int> userIds)
        {
            var ids = userIds?.Distinct().ToList();
            if (ids == null || ids.Count == 0) return new List<string>();
            using (var conn = _db.GetMasterConnection())
                return conn.Query<string>(
                    @"SELECT DISTINCT fcm_token FROM device_push_tokens
                      WHERE user_type = 'teacher' AND group_id = @groupId AND school_id = @schoolId
                        AND teacher_user_id IN @ids",
                    new { groupId, schoolId, ids }).ToList();
        }
    }
}
