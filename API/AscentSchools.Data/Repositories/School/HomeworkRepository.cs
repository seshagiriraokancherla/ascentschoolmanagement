using AscentSchools.Core.DTOs.School.Homework;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System.Collections.Generic;

namespace AscentSchools.Data.Repositories.School
{
    public class HomeworkRepository
    {
        private readonly IConnectionFactory _db;
        public HomeworkRepository(IConnectionFactory db) { _db = db; }

        public IEnumerable<HomeworkDto> GetHomework(string tenantDbName, int schoolId, int? classId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<HomeworkDto>(
                    @"SELECT h.homework_id HomeworkId, h.title Title, h.description Description,
                             h.subject_id SubjectId, sub.subject_name SubjectName,
                             h.class_id ClassId, c.class_name ClassName,
                             h.section_id SectionId, sec.section_name SectionName,
                             h.assigned_date AssignedDate, h.due_date DueDate,
                             h.attachment_url AttachmentUrl,
                             h.status Status, h.created_by CreatedBy, h.created_at CreatedAt
                      FROM homework h
                      LEFT JOIN subjects  sub ON sub.subject_id  = h.subject_id
                      LEFT JOIN classes   c   ON c.class_id      = h.class_id
                      LEFT JOIN sections  sec ON sec.section_id  = h.section_id
                      WHERE h.school_id = @schoolId
                        AND h.status    != 'Cancelled'
                        AND (@classId IS NULL OR h.class_id = @classId)
                      ORDER BY h.due_date DESC",
                    new { schoolId, classId });
        }

        public int CreateHomework(string tenantDbName, int schoolId, string createdBy, SaveHomeworkRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QuerySingle<int>(
                    @"INSERT INTO homework (title, description, subject_id, class_id, section_id, assigned_date, due_date, attachment_url, school_id, created_by)
                      VALUES (@title, @description, @subjectId, @classId, @sectionId, @assignedDate, @dueDate, @attachmentUrl, @schoolId, @createdBy);
                      SELECT SCOPE_IDENTITY();",
                    new { req.Title, req.Description, req.SubjectId, req.ClassId, req.SectionId, req.AssignedDate, req.DueDate, req.AttachmentUrl, schoolId, createdBy });
        }

        public void UpdateHomework(string tenantDbName, int schoolId, int id, string updatedBy, SaveHomeworkRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE homework
                      SET title = @title, description = @description, subject_id = @subjectId,
                          class_id = @classId, section_id = @sectionId,
                          assigned_date = @assignedDate, due_date = @dueDate,
                          attachment_url = @attachmentUrl,
                          updated_by = @updatedBy, updated_at = GETDATE()
                      WHERE homework_id = @id AND school_id = @schoolId",
                    new { req.Title, req.Description, req.SubjectId, req.ClassId, req.SectionId, req.AssignedDate, req.DueDate, req.AttachmentUrl, updatedBy, id, schoolId });
        }

        public void DeleteHomework(string tenantDbName, int schoolId, int id, string updatedBy)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE homework SET status = 'Cancelled', updated_by = @updatedBy, updated_at = GETDATE()
                      WHERE homework_id = @id AND school_id = @schoolId",
                    new { updatedBy, id, schoolId });
        }
    }
}
