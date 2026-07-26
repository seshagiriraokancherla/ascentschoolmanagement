using AscentSchools.Core.DTOs.School.Announcements;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System.Collections.Generic;

namespace AscentSchools.Data.Repositories.School
{
    public class AnnouncementsRepository
    {
        private readonly IConnectionFactory _db;
        public AnnouncementsRepository(IConnectionFactory db) { _db = db; }

        public IEnumerable<AnnouncementDto> GetAnnouncements(string tenantDbName, int schoolId, int? classId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<AnnouncementDto>(
                    @"SELECT a.announcement_id AnnouncementId, a.title Title, a.description Description,
                             a.scope Scope, a.class_id ClassId, c.class_name ClassName,
                             a.section_id SectionId, s.section_name SectionName,
                             a.is_pinned IsPinned, a.status Status, a.created_by CreatedBy, a.created_at CreatedAt,
                             a.attachment_url AttachmentUrl
                      FROM announcements a
                      LEFT JOIN classes c  ON c.class_id   = a.class_id
                      LEFT JOIN sections s ON s.section_id = a.section_id
                      WHERE a.school_id = @schoolId
                        AND a.status    = 'Active'
                        AND (@classId IS NULL OR a.scope = 'School' OR a.class_id = @classId)
                      ORDER BY a.is_pinned DESC, a.created_at DESC",
                    new { schoolId, classId });
        }

        public int CreateAnnouncement(string tenantDbName, int schoolId, string createdBy, SaveAnnouncementRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QuerySingle<int>(
                    @"INSERT INTO announcements (title, description, scope, class_id, section_id, is_pinned, attachment_url, school_id, created_by)
                      VALUES (@title, @description, @scope, @classId, @sectionId, @isPinned, @attachmentUrl, @schoolId, @createdBy);
                      SELECT SCOPE_IDENTITY();",
                    new { req.Title, req.Description, req.Scope, req.ClassId, req.SectionId, req.IsPinned, req.AttachmentUrl, schoolId, createdBy });
        }

        public void UpdateAnnouncement(string tenantDbName, int schoolId, int id, string updatedBy, SaveAnnouncementRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE announcements
                      SET title = @title, description = @description, scope = @scope,
                          class_id = @classId, section_id = @sectionId, is_pinned = @isPinned, attachment_url = @attachmentUrl,
                          updated_by = @updatedBy, updated_at = GETDATE()
                      WHERE announcement_id = @id AND school_id = @schoolId",
                    new { req.Title, req.Description, req.Scope, req.ClassId, req.SectionId, req.IsPinned, req.AttachmentUrl, updatedBy, id, schoolId });
        }

        public void DeleteAnnouncement(string tenantDbName, int schoolId, int id, string updatedBy)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE announcements SET status = 'Inactive', updated_by = @updatedBy, updated_at = GETDATE()
                      WHERE announcement_id = @id AND school_id = @schoolId",
                    new { updatedBy, id, schoolId });
        }
    }
}
