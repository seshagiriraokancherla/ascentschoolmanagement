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
        // includeClassWide: when true and a sectionId is given, class-wide homework
        // (section_id IS NULL) is returned alongside the section's own — used by the
        // teacher app so a section teacher still sees whole-class homework. The web
        // Daily Homework page leaves it false for an exact class+section match.
        public (IEnumerable<HomeworkDto> Items, int Total) GetHomeworkPaged(
            string tenantDbName, int schoolId, int? classId, int? sectionId,
            DateTime? assignedDate, int page, int pageSize, bool includeClassWide = false)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 200) pageSize = 200;
            var skip = (page - 1) * pageSize;

            const string where =
                @"WHERE h.school_id = @schoolId
                    AND h.status    != 'Cancelled'
                    AND (@classId      IS NULL OR h.class_id     = @classId)
                    AND (@sectionId    IS NULL
                         OR h.section_id = @sectionId
                         OR (@includeClassWide = 1 AND h.section_id IS NULL))
                    AND (@assignedDate IS NULL OR h.assigned_date = @assignedDate)";

            var args = new { schoolId, classId, sectionId, assignedDate, skip, take = pageSize, includeClassWide };
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

        /// <summary>Saves one homework row per filled subject, for each target section.
        /// The day's existing Active rows for the sections being written (plus any
        /// class-wide rows, which reach those same students) are soft-cancelled first,
        /// so the page acts as create-or-edit.</summary>
        public BatchHomeworkResult CreateBatchHomework(string tenantDbName, int schoolId, string createdBy, BatchHomeworkRequest req)
        {
            var items = (req.Items ?? new List<BatchHomeworkItem>())
                .Where(i => i.SubjectId.HasValue && !string.IsNullOrWhiteSpace(i.Description))
                .ToList();

            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                if (conn.State != ConnectionState.Open) conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    var targets = ResolveTargetSections(conn, tx, schoolId, req);

                    // A single null target means one class-wide row set, which every
                    // section sees — so it supersedes each section's own rows too.
                    var classWide = targets.Count == 1 && !targets[0].HasValue;
                    var targetIds = targets.Where(t => t.HasValue).Select(t => t.Value).ToList();

                    // Subject names (for the row title) resolved in one query.
                    var subjectIds = items.Select(i => i.SubjectId.Value).Distinct().ToList();
                    var names = conn.Query<SubjectNameRow>(
                        @"SELECT subject_id SubjectId, subject_name SubjectName
                          FROM subjects WHERE subject_id IN @ids AND school_id = @schoolId",
                        new { ids = subjectIds, schoolId }, tx)
                        .ToDictionary(r => r.SubjectId, r => r.SubjectName);

                    // Overwrite-on-resave. Class-wide rows (section_id NULL) are always
                    // cancelled: the parent feed matches `section_id IS NULL OR = @section`,
                    // so leaving one behind would show stale homework next to the new rows.
                    // Sections we are NOT writing keep their own rows. Writing class-wide
                    // reaches everyone, so it clears every section. The section predicate is
                    // branched rather than parameterised so we never emit `IN ()` for an
                    // empty list (targetIds is empty only in the class-wide case).
                    var sectionFilter = classWide
                        ? ""
                        : " AND (section_id IS NULL OR section_id IN @targetIds)";
                    conn.Execute(
                        @"UPDATE homework
                          SET status = 'Cancelled', updated_by = @createdBy, updated_at = GETDATE()
                          WHERE school_id = @schoolId
                            AND status = 'Active'
                            AND assigned_date = @assignedDate
                            AND ISNULL(class_id, 0) = ISNULL(@classId, 0)" + sectionFilter,
                        new { schoolId, createdBy, req.AssignedDate, req.ClassId, targetIds }, tx);

                    foreach (var sectionId in targets)
                    {
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
                                    sectionId,
                                    req.AssignedDate,
                                    dueDate      = (DateTime?)null,   // due date retired
                                    schoolId,
                                    createdBy,
                                }, tx);
                        }
                    }

                    tx.Commit();

                    return new BatchHomeworkResult
                    {
                        SubjectCount = items.Count,
                        RowCount     = items.Count * targets.Count,
                        Sections     = targets,
                    };
                }
            }
        }

        /// <summary>Section ids a batch save should write. A single null entry means
        /// one class-wide (section_id NULL) row set.</summary>
        private static List<int?> ResolveTargetSections(
            IDbConnection conn, IDbTransaction tx, int schoolId, BatchHomeworkRequest req)
        {
            var picked = (req.SectionIds ?? new List<int>())
                .Where(id => id > 0).Distinct().ToList();

            // No list sent (older client) → fall back to the single-section field.
            if (picked.Count == 0)
                return new List<int?> { req.SectionId.HasValue && req.SectionId.Value > 0 ? req.SectionId : null };

            // Picking every active section is the same reach as class-wide, and one
            // NULL row beats N duplicates in the lists, reports and parent feed.
            if (req.ClassId.HasValue)
            {
                var active = conn.Query<int>(
                    @"SELECT section_id FROM sections
                      WHERE class_id = @classId AND school_id = @schoolId
                        AND ISNULL(status, 'Active') IN ('Active', 'Y')",
                    new { classId = req.ClassId, schoolId }, tx).ToList();

                if (active.Count > 0 && active.All(picked.Contains))
                    return new List<int?> { null };
            }

            return picked.Select(id => (int?)id).ToList();
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
