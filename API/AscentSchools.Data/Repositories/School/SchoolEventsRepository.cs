using AscentSchools.Core.DTOs.School.Events;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System.Collections.Generic;

namespace AscentSchools.Data.Repositories.School
{
    public class SchoolEventsRepository
    {
        private readonly IConnectionFactory _db;
        public SchoolEventsRepository(IConnectionFactory db) { _db = db; }

        public IEnumerable<SchoolEventDto> GetEvents(string tenantDbName, int schoolId, int? classId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<SchoolEventDto>(
                    @"SELECT e.event_id      EventId,
                             e.title         Title,
                             e.description   Description,
                             CONVERT(VARCHAR(10), e.event_date, 120) EventDate,
                             e.media_type    MediaType,
                             e.media_url     MediaUrl,
                             e.thumbnail_url  ThumbnailUrl,
                             e.attachment_url AttachmentUrl,
                             e.scope          Scope,
                             e.class_id      ClassId,
                             c.class_name    ClassName,
                             e.is_pinned     IsPinned,
                             e.created_by    CreatedBy,
                             e.created_at    CreatedAt
                      FROM school_events e
                      LEFT JOIN classes c ON c.class_id = e.class_id
                      WHERE e.school_id = @schoolId
                        AND e.status    = 'Active'
                        AND (@classId IS NULL OR e.scope = 'School' OR e.class_id = @classId)
                      ORDER BY e.is_pinned DESC, e.event_date DESC",
                    new { schoolId, classId });
        }

        public int CreateEvent(string tenantDbName, int schoolId, string createdBy, SaveSchoolEventRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QuerySingle<int>(
                    @"INSERT INTO school_events
                        (school_id, title, description, event_date, media_type, media_url,
                         thumbnail_url, attachment_url, scope, class_id, is_pinned, created_by)
                      VALUES
                        (@schoolId, @title, @description, @eventDate, @mediaType, @mediaUrl,
                         @thumbnailUrl, @attachmentUrl, @scope, @classId, @isPinned, @createdBy);
                      SELECT SCOPE_IDENTITY();",
                    new
                    {
                        schoolId,
                        req.Title, req.Description, req.EventDate, req.MediaType,
                        req.MediaUrl, req.ThumbnailUrl, req.AttachmentUrl, req.Scope, req.ClassId, req.IsPinned,
                        createdBy
                    });
        }

        public void UpdateEvent(string tenantDbName, int schoolId, int id, string updatedBy, SaveSchoolEventRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE school_events
                      SET title         = @title,
                          description   = @description,
                          event_date    = @eventDate,
                          media_type    = @mediaType,
                          media_url     = @mediaUrl,
                          thumbnail_url  = @thumbnailUrl,
                          attachment_url = @attachmentUrl,
                          scope          = @scope,
                          class_id      = @classId,
                          is_pinned     = @isPinned,
                          updated_by    = @updatedBy,
                          updated_at    = GETDATE()
                      WHERE event_id  = @id
                        AND school_id = @schoolId",
                    new
                    {
                        req.Title, req.Description, req.EventDate, req.MediaType,
                        req.MediaUrl, req.ThumbnailUrl, req.AttachmentUrl, req.Scope, req.ClassId, req.IsPinned,
                        updatedBy, id, schoolId
                    });
        }

        public void DeleteEvent(string tenantDbName, int schoolId, int id, string updatedBy)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE school_events
                      SET status     = 'Inactive',
                          updated_by = @updatedBy,
                          updated_at = GETDATE()
                      WHERE event_id  = @id
                        AND school_id = @schoolId",
                    new { updatedBy, id, schoolId });
        }
    }
}
