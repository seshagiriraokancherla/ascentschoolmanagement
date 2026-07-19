using AscentSchools.Core.DTOs.School.Homework;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

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
                      ORDER BY h.assigned_date DESC, h.homework_id DESC",
                    new { schoolId, classId });
        }

        /// <summary>
        /// Server-paged homework list for the web admin screen. Optional sectionId /
        /// assignedDate filters let the Daily Homework page fetch one class+section+date
        /// precisely instead of pulling the whole history and filtering client-side.
        /// Returns the page of rows plus the total count (for the pager).
        /// </summary>
        public (IEnumerable<HomeworkDto> Items, int Total) GetHomeworkPaged(
            string tenantDbName, int schoolId, int? classId, int? sectionId,
            DateTime? assignedDate, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 200) pageSize = 200;
            var skip = (page - 1) * pageSize;

            const string where =
                @"WHERE h.school_id = @schoolId
                    AND h.status    != 'Cancelled'
                    AND (@classId      IS NULL OR h.class_id     = @classId)
                    AND (@sectionId    IS NULL OR h.section_id   = @sectionId)
                    AND (@assignedDate IS NULL OR h.assigned_date = @assignedDate)";

            var args = new { schoolId, classId, sectionId, assignedDate, skip, take = pageSize };
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var total = conn.ExecuteScalar<int>($"SELECT COUNT(*) FROM homework h {where}", args);
                var items = conn.Query<HomeworkDto>(
                    $@"SELECT h.homework_id HomeworkId, h.title Title, h.description Description,
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
                       {where}
                       ORDER BY h.assigned_date DESC, h.homework_id DESC
                       OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY", args).ToList();
                return (items, total);
            }
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

        /// <summary>Saves one homework row per filled subject for a Class+Section+Date.
        /// Re-saving the same Class+Section+Date first soft-cancels that day's existing
        /// Active rows, then inserts the new set — so the page acts as create-or-edit.</summary>
        public int CreateBatchHomework(string tenantDbName, int schoolId, string createdBy, BatchHomeworkRequest req)
        {
            var items = (req.Items ?? new List<BatchHomeworkItem>())
                .Where(i => i.SubjectId.HasValue && !string.IsNullOrWhiteSpace(i.Description))
                .ToList();

            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                if (conn.State != ConnectionState.Open) conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    // Subject names (for the row title) resolved in one query.
                    var subjectIds = items.Select(i => i.SubjectId.Value).Distinct().ToList();
                    var names = conn.Query<SubjectNameRow>(
                        "SELECT subject_id SubjectId, subject_name SubjectName FROM subjects WHERE subject_id IN @ids",
                        new { ids = subjectIds }, tx)
                        .ToDictionary(r => r.SubjectId, r => r.SubjectName);

                    // Overwrite-on-resave: cancel the day's existing Active rows for this class+section.
                    conn.Execute(
                        @"UPDATE homework
                          SET status = 'Cancelled', updated_by = @createdBy, updated_at = GETDATE()
                          WHERE school_id = @schoolId
                            AND status = 'Active'
                            AND assigned_date = @assignedDate
                            AND ISNULL(class_id, 0)   = ISNULL(@classId, 0)
                            AND ISNULL(section_id, 0) = ISNULL(@sectionId, 0)",
                        new { schoolId, createdBy, req.AssignedDate, req.ClassId, req.SectionId }, tx);

                    foreach (var item in items)
                    {
                        var subjectName = names.TryGetValue(item.SubjectId.Value, out var n) ? n : null;
                        conn.Execute(
                            @"INSERT INTO homework (title, description, subject_id, class_id, section_id, assigned_date, due_date, school_id, created_by)
                              VALUES (@title, @description, @subjectId, @classId, @sectionId, @assignedDate, @dueDate, @schoolId, @createdBy)",
                            new
                            {
                                title        = string.IsNullOrWhiteSpace(subjectName) ? "Homework" : subjectName + " Homework",
                                description  = item.Description,
                                subjectId    = item.SubjectId,
                                req.ClassId,
                                req.SectionId,
                                req.AssignedDate,
                                dueDate      = (DateTime?)null,   // due date retired
                                schoolId,
                                createdBy,
                            }, tx);
                    }

                    tx.Commit();
                }
            }
            return items.Count;
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

        private class SubjectNameRow
        {
            public int    SubjectId   { get; set; }
            public string SubjectName { get; set; }
        }
    }
}
