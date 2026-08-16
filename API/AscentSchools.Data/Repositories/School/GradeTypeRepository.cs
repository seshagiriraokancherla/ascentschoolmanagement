using AscentSchools.Core.DTOs.School.GradeTypes;
using AscentSchools.Core.DTOs.School.Students;   // BulkImportResult / BulkRowError
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AscentSchools.Data.Repositories.School
{
    /// <summary>
    /// grade_types — marks-to-grade bands (e.g. 90-100 → A+), optionally per subject.
    /// </summary>
    public class GradeTypeRepository
    {
        private readonly IConnectionFactory _db;
        public GradeTypeRepository(IConnectionFactory db) { _db = db; }

        public IEnumerable<GradeTypeDto> GetAll(string tenantDbName, int schoolId, int? subjectId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<GradeTypeDto>(
                    @"SELECT g.id Id, g.grade_name GradeName, g.subject_id SubjectId,
                             sub.subject_name SubjectName,
                             g.min_marks MinMarks, g.max_marks MaxMarks,
                             g.grade Grade, g.remarks Remarks, g.status Status
                      FROM grade_types g
                      LEFT JOIN subjects sub ON sub.subject_id = g.subject_id
                      WHERE g.school_id = @schoolId
                        AND (@subjectId IS NULL OR g.subject_id = @subjectId)
                      ORDER BY g.grade_name, g.min_marks",
                    new { schoolId, subjectId });
        }

        public int Create(string tenantDbName, int schoolId, string createdBy, SaveGradeTypeRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QuerySingle<int>(
                    @"INSERT INTO grade_types
                        (grade_name, subject_id, min_marks, max_marks, grade, remarks,
                         status, school_id, created_by)
                      VALUES (@GradeName, @SubjectId, @MinMarks, @MaxMarks, @Grade, @Remarks,
                              @Status, @schoolId, @createdBy);
                      SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    new
                    {
                        req.GradeName, req.SubjectId, req.MinMarks, req.MaxMarks, req.Grade, req.Remarks,
                        Status = string.IsNullOrWhiteSpace(req.Status) ? "Active" : req.Status,
                        schoolId, createdBy,
                    });
        }

        public void Update(string tenantDbName, int schoolId, int id, SaveGradeTypeRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE grade_types SET
                          grade_name = @GradeName, subject_id = @SubjectId,
                          min_marks = @MinMarks, max_marks = @MaxMarks,
                          grade = @Grade, remarks = @Remarks, status = @Status
                      WHERE id = @id AND school_id = @schoolId",
                    new
                    {
                        req.GradeName, req.SubjectId, req.MinMarks, req.MaxMarks, req.Grade, req.Remarks,
                        Status = string.IsNullOrWhiteSpace(req.Status) ? "Active" : req.Status,
                        id, schoolId,
                    });
        }

        public int Delete(string tenantDbName, int schoolId, int id)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Execute(
                    @"DELETE FROM grade_types WHERE id = @id AND school_id = @schoolId",
                    new { id, schoolId });
        }

        /// <summary>
        /// Insert many grade bands from a CSV. Subject is matched by NAME
        /// (blank = all subjects); latest year's subject wins on a duplicate name.
        /// Bad rows are reported, not fatal.
        /// </summary>
        public BulkImportResult BulkCreate(string tenantDbName, int schoolId, string createdBy,
            IEnumerable<GradeTypeBulkRow> rows)
        {
            var list = rows?.ToList() ?? new List<GradeTypeBulkRow>();
            var result = new BulkImportResult { Total = list.Count };

            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var subjectMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var s in conn.Query<SubjectNameId>(
                    @"SELECT subject_id SubjectId, subject_name SubjectName
                      FROM subjects
                      WHERE school_id = @schoolId AND ISNULL(status, 'Active') = 'Active'
                      ORDER BY academic_year_id DESC",
                    new { schoolId }))
                {
                    if (!string.IsNullOrWhiteSpace(s.SubjectName) && !subjectMap.ContainsKey(s.SubjectName.Trim()))
                        subjectMap[s.SubjectName.Trim()] = s.SubjectId;
                }

                int rowNum = 1;   // header is row 1
                foreach (var r in list)
                {
                    rowNum++;
                    if (string.IsNullOrWhiteSpace(r.GradeName))
                    {
                        result.Failed++;
                        result.Errors.Add(new BulkRowError { Row = rowNum, Identifier = r.GradeName, Reason = "Grade name is required." });
                        continue;
                    }

                    int? subjectId = null;
                    if (!string.IsNullOrWhiteSpace(r.SubjectName))
                    {
                        if (subjectMap.TryGetValue(r.SubjectName.Trim(), out var sid)) subjectId = sid;
                        else
                        {
                            result.Failed++;
                            result.Errors.Add(new BulkRowError { Row = rowNum, Identifier = r.GradeName, Reason = $"Subject '{r.SubjectName}' not found." });
                            continue;
                        }
                    }

                    try
                    {
                        conn.Execute(
                            @"INSERT INTO grade_types
                                (grade_name, subject_id, min_marks, max_marks, grade, remarks,
                                 status, school_id, created_by)
                              VALUES (@GradeName, @subjectId, @MinMarks, @MaxMarks, @Grade, @Remarks,
                                      @Status, @schoolId, @createdBy)",
                            new
                            {
                                GradeName = r.GradeName.Trim(),
                                subjectId,
                                r.MinMarks, r.MaxMarks,
                                Grade   = string.IsNullOrWhiteSpace(r.Grade)   ? null : r.Grade.Trim(),
                                Remarks = string.IsNullOrWhiteSpace(r.Remarks) ? null : r.Remarks.Trim(),
                                Status  = string.IsNullOrWhiteSpace(r.Status)  ? "Active" : r.Status.Trim(),
                                schoolId, createdBy,
                            });
                        result.Imported++;
                    }
                    catch (Exception ex)
                    {
                        result.Failed++;
                        result.Errors.Add(new BulkRowError { Row = rowNum, Identifier = r.GradeName, Reason = ex.Message });
                    }
                }
            }
            return result;
        }

        private class SubjectNameId
        {
            public int    SubjectId   { get; set; }
            public string SubjectName { get; set; }
        }
    }
}
