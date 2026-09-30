using AscentSchools.Core.DTOs.School.MarksGrades;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System.Collections.Generic;
using System.Data;

namespace AscentSchools.Data.Repositories.School
{
    /// <summary>
    /// marks_grade_master — reusable named grading scales for a student's TOTAL marks
    /// (header) and marks_grade_master_bands (their bands). School-wide: NOT tied to a
    /// class, section or academic year. Each exam row picks a scale
    /// (exam_master.marks_grade_master_id), like it already picks a grade type per subject.
    /// </summary>
    public class MarksGradeMasterRepository
    {
        private readonly IConnectionFactory _db;
        public MarksGradeMasterRepository(IConnectionFactory db) { _db = db; }

        /// <summary>All scales with their band count and how many exams use each.</summary>
        public IEnumerable<MarksGradeScaleDto> GetScales(string tenantDbName, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<MarksGradeScaleDto>(
                    @"SELECT m.id Id, m.scale_name ScaleName, m.description Description, m.status Status,
                             (SELECT COUNT(1) FROM marks_grade_master_bands b WHERE b.scale_id = m.id) BandCount,
                             (SELECT COUNT(1) FROM exam_master em WHERE em.marks_grade_master_id = m.id) ExamCount
                      FROM marks_grade_master m
                      WHERE m.school_id = @schoolId
                      ORDER BY m.scale_name",
                    new { schoolId });
        }

        public IEnumerable<MarksGradeBandDto> GetBands(string tenantDbName, int schoolId, int scaleId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<MarksGradeBandDto>(
                    @"SELECT id Id, min_marks MinMarks, max_marks MaxMarks, grade Grade,
                             grade_point GradePoint, description Description
                      FROM marks_grade_master_bands
                      WHERE scale_id = @scaleId AND school_id = @schoolId
                      ORDER BY max_marks DESC",
                    new { scaleId, schoolId });
        }

        /// <summary>True when another scale of this school already has the name.</summary>
        public bool NameExists(string tenantDbName, int schoolId, string scaleName, int? exceptId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.ExecuteScalar<int>(
                    @"SELECT COUNT(1) FROM marks_grade_master
                      WHERE school_id = @schoolId AND scale_name = @scaleName
                        AND (@exceptId IS NULL OR id <> @exceptId)",
                    new { schoolId, scaleName, exceptId }) > 0;
        }

        public int CreateScale(string tenantDbName, int schoolId, string createdBy, SaveMarksGradeScaleRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QuerySingle<int>(
                    @"INSERT INTO marks_grade_master (scale_name, description, status, school_id, created_by)
                      VALUES (@ScaleName, @Description, @Status, @schoolId, @createdBy);
                      SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    new { req.ScaleName, req.Description, Status = req.Status ?? "Active", schoolId, createdBy });
        }

        public int UpdateScale(string tenantDbName, int schoolId, int id, SaveMarksGradeScaleRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Execute(
                    @"UPDATE marks_grade_master
                      SET scale_name = @ScaleName, description = @Description, status = @Status
                      WHERE id = @id AND school_id = @schoolId",
                    new { req.ScaleName, req.Description, Status = req.Status ?? "Active", id, schoolId });
        }

        /// <summary>Exams pointing at this scale — a used scale must not be deleted.</summary>
        public int CountExamsUsing(string tenantDbName, int schoolId, int scaleId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.ExecuteScalar<int>(
                    @"SELECT COUNT(1) FROM exam_master
                      WHERE marks_grade_master_id = @scaleId AND school_id = @schoolId",
                    new { scaleId, schoolId });
        }

        /// <summary>Delete a scale and its bands (callers must check CountExamsUsing first).</summary>
        public int DeleteScale(string tenantDbName, int schoolId, int id)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                if (conn.State != ConnectionState.Open) conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    conn.Execute("DELETE FROM marks_grade_master_bands WHERE scale_id = @id AND school_id = @schoolId",
                        new { id, schoolId }, tx);
                    var n = conn.Execute("DELETE FROM marks_grade_master WHERE id = @id AND school_id = @schoolId",
                        new { id, schoolId }, tx);
                    tx.Commit();
                    return n;
                }
            }
        }

        /// <summary>
        /// Replace a scale's bands (delete + insert in one transaction) so the saved set is
        /// exactly what the form submitted.
        /// </summary>
        public void SaveBands(string tenantDbName, int schoolId, string createdBy, int scaleId,
            List<MarksGradeBand> bands)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                if (conn.State != ConnectionState.Open) conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    conn.Execute("DELETE FROM marks_grade_master_bands WHERE scale_id = @scaleId AND school_id = @schoolId",
                        new { scaleId, schoolId }, tx);

                    if (bands != null)
                    {
                        foreach (var b in bands)
                        {
                            conn.Execute(
                                @"INSERT INTO marks_grade_master_bands
                                    (scale_id, min_marks, max_marks, grade, grade_point, description,
                                     school_id, created_by)
                                  VALUES (@scaleId, @MinMarks, @MaxMarks, @Grade, @GradePoint, @Description,
                                          @schoolId, @createdBy)",
                                new
                                {
                                    scaleId,
                                    b.MinMarks,
                                    b.MaxMarks,
                                    Grade       = b.Grade.Trim(),
                                    GradePoint  = string.IsNullOrWhiteSpace(b.GradePoint)  ? null : b.GradePoint.Trim(),
                                    Description = string.IsNullOrWhiteSpace(b.Description) ? null : b.Description.Trim(),
                                    schoolId,
                                    createdBy,
                                }, tx);
                        }
                    }

                    tx.Commit();
                }
            }
        }
    }
}
