using AscentSchools.Core.DTOs.School.ExamMaster;
using AscentSchools.Core.DTOs.School.Students;   // BulkImportResult / BulkRowError
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AscentSchools.Data.Repositories.School
{
    /// <summary>
    /// exam_master — exam definitions per class + subject (marks, date, grade band).
    /// One row per (academic year, exam type, class, subject).
    /// </summary>
    public class ExamMasterRepository
    {
        private readonly IConnectionFactory _db;
        public ExamMasterRepository(IConnectionFactory db) { _db = db; }

        public IEnumerable<ExamMasterDto> GetExams(string tenantDbName, int schoolId,
            int academicYearId, int? examTypeId, int? classId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<ExamMasterDto>(
                    @"SELECT em.id Id, em.exam_name ExamName,
                             em.exam_type_id ExamTypeId, et.exam_type_name ExamTypeName,
                             em.class_id ClassId, c.class_name ClassName,
                             em.subject_id SubjectId, sub.subject_name SubjectName,
                             em.academic_year_id AcademicYearId,
                             em.exam_category ExamCategory, em.exam_date ExamDate,
                             em.exam_total_marks ExamTotalMarks, em.exam_min_marks ExamMinMarks,
                             em.sub_max_marks SubMaxMarks, em.subject_min_marks SubjectMinMarks,
                             em.activity_max_marks ActivityMaxMarks, em.exam_remarks ExamRemarks,
                             em.grade_type_id GradeTypeId, g.grade_name GradeName,
                             em.exam_status ExamStatus
                      FROM exam_master em
                      LEFT JOIN exam_types  et  ON et.exam_type_id  = em.exam_type_id
                      LEFT JOIN classes     c   ON c.class_id       = em.class_id
                      LEFT JOIN subjects    sub ON sub.subject_id   = em.subject_id
                      LEFT JOIN grade_types g   ON g.id             = em.grade_type_id
                      WHERE em.school_id = @schoolId
                        AND em.academic_year_id = @academicYearId
                        AND (@examTypeId IS NULL OR em.exam_type_id = @examTypeId)
                        AND (@classId    IS NULL OR em.class_id     = @classId)
                      ORDER BY c.class_name, et.exam_type_name, sub.subject_name",
                    new { schoolId, academicYearId, examTypeId, classId });
        }

        /// <summary>
        /// Create one exam row per selected subject (all share the header fields).
        /// A subject that already has a row for this year+exam type+class is skipped
        /// so "Add Exam" doesn't create duplicates on a re-add (edit the row to change it).
        /// </summary>
        public void CreateExams(string tenantDbName, int schoolId, string createdBy, CreateExamMasterRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                foreach (var subjectId in req.SubjectIds)
                {
                    conn.Execute(
                        @"IF NOT EXISTS (SELECT 1 FROM exam_master
                                         WHERE school_id = @schoolId AND academic_year_id = @AcademicYearId
                                           AND exam_type_id = @ExamTypeId AND class_id = @ClassId
                                           AND subject_id = @subjectId)
                          INSERT INTO exam_master
                              (exam_name, exam_type_id, class_id, subject_id, academic_year_id,
                               exam_category, exam_date, exam_total_marks, exam_min_marks,
                               sub_max_marks, subject_min_marks, activity_max_marks, exam_remarks,
                               grade_type_id, exam_status, school_id, created_by)
                          VALUES
                              (@ExamName, @ExamTypeId, @ClassId, @subjectId, @AcademicYearId,
                               @ExamCategory, @ExamDate, @ExamTotalMarks, @ExamMinMarks,
                               @SubMaxMarks, @SubjectMinMarks, @ActivityMaxMarks, @ExamRemarks,
                               @GradeTypeId, @ExamStatus, @schoolId, @createdBy);",
                        new
                        {
                            req.AcademicYearId, req.ExamTypeId, req.ClassId, subjectId, schoolId,
                            req.ExamName, req.ExamCategory, req.ExamDate,
                            req.ExamTotalMarks, req.ExamMinMarks, req.SubMaxMarks, req.SubjectMinMarks,
                            req.ActivityMaxMarks, req.ExamRemarks, req.GradeTypeId,
                            ExamStatus = string.IsNullOrWhiteSpace(req.ExamStatus) ? "Active" : req.ExamStatus,
                            createdBy,
                        });
                }
            }
        }

        public void UpdateExam(string tenantDbName, int schoolId, int id, UpdateExamMasterRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE exam_master SET
                          exam_name = @ExamName, exam_type_id = @ExamTypeId, class_id = @ClassId,
                          subject_id = @SubjectId, academic_year_id = @AcademicYearId,
                          exam_category = @ExamCategory, exam_date = @ExamDate,
                          exam_total_marks = @ExamTotalMarks, exam_min_marks = @ExamMinMarks,
                          sub_max_marks = @SubMaxMarks, subject_min_marks = @SubjectMinMarks,
                          activity_max_marks = @ActivityMaxMarks,
                          exam_remarks = @ExamRemarks, grade_type_id = @GradeTypeId,
                          exam_status = @ExamStatus
                      WHERE id = @id AND school_id = @schoolId",
                    new
                    {
                        req.ExamName, req.ExamTypeId, req.ClassId, req.SubjectId, req.AcademicYearId,
                        req.ExamCategory, req.ExamDate, req.ExamTotalMarks, req.ExamMinMarks,
                        req.SubMaxMarks, req.SubjectMinMarks, req.ActivityMaxMarks, req.ExamRemarks, req.GradeTypeId,
                        ExamStatus = string.IsNullOrWhiteSpace(req.ExamStatus) ? "Active" : req.ExamStatus,
                        id, schoolId,
                    });
        }

        public int DeleteExam(string tenantDbName, int schoolId, int id)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Execute(
                    @"DELETE FROM exam_master WHERE id = @id AND school_id = @schoolId",
                    new { id, schoolId });
        }

        /// <summary>
        /// CSV import. All references are matched BY NAME (year / exam type / class /
        /// subject required; grade type optional). A row whose (year, exam type, class,
        /// subject) already exists is SKIPPED (same rule as single-create). Bad rows are
        /// reported, not fatal.
        /// </summary>
        public BulkImportResult BulkImport(string tenantDbName, int schoolId, string createdBy,
            IEnumerable<ExamMasterBulkRow> rows)
        {
            var list = rows?.ToList() ?? new List<ExamMasterBulkRow>();
            var result = new BulkImportResult { Total = list.Count };

            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                // ── name → id lookup maps ──────────────────────────────────────
                var yearMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var y in conn.Query<NameId>(
                    @"SELECT academic_year_id Id, academic_year Name FROM academic_years WHERE school_id = @schoolId",
                    new { schoolId }))
                    if (!string.IsNullOrWhiteSpace(y.Name) && !yearMap.ContainsKey(y.Name.Trim()))
                        yearMap[y.Name.Trim()] = y.Id;

                var classMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var c in conn.Query<NameId>(
                    @"SELECT class_id Id, class_name Name FROM classes WHERE school_id = @schoolId",
                    new { schoolId }))
                    if (!string.IsNullOrWhiteSpace(c.Name) && !classMap.ContainsKey(c.Name.Trim()))
                        classMap[c.Name.Trim()] = c.Id;

                // Subjects repeat per year — latest year wins on a duplicate name.
                var subjectMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var s in conn.Query<NameId>(
                    @"SELECT subject_id Id, subject_name Name FROM subjects
                      WHERE school_id = @schoolId AND ISNULL(status,'Active') = 'Active'
                      ORDER BY academic_year_id DESC",
                    new { schoolId }))
                    if (!string.IsNullOrWhiteSpace(s.Name) && !subjectMap.ContainsKey(s.Name.Trim()))
                        subjectMap[s.Name.Trim()] = s.Id;

                // Exam types keyed by name + academic_year_id.
                var examTypeMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var e in conn.Query<NameIdYear>(
                    @"SELECT exam_type_id Id, exam_type_name Name, academic_year_id YearId
                      FROM exam_types WHERE school_id = @schoolId AND status = 'Active'",
                    new { schoolId }))
                {
                    var key = $"{(e.Name ?? "").Trim()}|{e.YearId}";
                    if (!string.IsNullOrWhiteSpace(e.Name) && !examTypeMap.ContainsKey(key))
                        examTypeMap[key] = e.Id;
                }

                // Grade types by name (first match wins; optional).
                var gradeMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var g in conn.Query<NameId>(
                    @"SELECT id Id, grade_name Name FROM grade_types
                      WHERE school_id = @schoolId AND ISNULL(status,'Active') = 'Active'",
                    new { schoolId }))
                    if (!string.IsNullOrWhiteSpace(g.Name) && !gradeMap.ContainsKey(g.Name.Trim()))
                        gradeMap[g.Name.Trim()] = g.Id;

                int rowNum = 1;   // header is row 1
                foreach (var r in list)
                {
                    rowNum++;
                    string label = $"{r.ExamType}/{r.Class}/{r.Subject}";

                    void Fail(string reason) { result.Failed++; result.Errors.Add(new BulkRowError { Row = rowNum, Identifier = label, Reason = reason }); }

                    if (string.IsNullOrWhiteSpace(r.AcademicYear) || !yearMap.TryGetValue(r.AcademicYear.Trim(), out var yearId))
                    { Fail($"Academic year '{r.AcademicYear}' not found."); continue; }
                    if (string.IsNullOrWhiteSpace(r.ExamType) ||
                        !examTypeMap.TryGetValue($"{r.ExamType.Trim()}|{yearId}", out var examTypeId))
                    { Fail($"Exam type '{r.ExamType}' not found for {r.AcademicYear}."); continue; }
                    if (string.IsNullOrWhiteSpace(r.Class) || !classMap.TryGetValue(r.Class.Trim(), out var classId))
                    { Fail($"Class '{r.Class}' not found."); continue; }
                    if (string.IsNullOrWhiteSpace(r.Subject) || !subjectMap.TryGetValue(r.Subject.Trim(), out var subjectId))
                    { Fail($"Subject '{r.Subject}' not found."); continue; }

                    int? gradeTypeId = null;
                    if (!string.IsNullOrWhiteSpace(r.GradeType) && gradeMap.TryGetValue(r.GradeType.Trim(), out var gid))
                        gradeTypeId = gid;

                    DateTime? examDate = null;
                    if (!string.IsNullOrWhiteSpace(r.ExamDate) && DateTime.TryParse(r.ExamDate.Trim(), out var d))
                        examDate = d;

                    // Skip if this (year, exam type, class, subject) exam already exists.
                    var exists = conn.ExecuteScalar<int>(
                        @"SELECT COUNT(1) FROM exam_master
                          WHERE school_id = @schoolId AND academic_year_id = @yearId
                            AND exam_type_id = @examTypeId AND class_id = @classId AND subject_id = @subjectId",
                        new { schoolId, yearId, examTypeId, classId, subjectId }) > 0;
                    if (exists) { result.Skipped++; continue; }

                    try
                    {
                        conn.Execute(
                            @"INSERT INTO exam_master
                                (exam_name, exam_type_id, class_id, subject_id, academic_year_id,
                                 exam_category, exam_date, exam_total_marks, exam_min_marks,
                                 sub_max_marks, subject_min_marks, activity_max_marks, exam_remarks,
                                 grade_type_id, exam_status, school_id, created_by)
                              VALUES
                                (@ExamName, @examTypeId, @classId, @subjectId, @yearId,
                                 @Category, @examDate, @TotalMarks, @ExamMinMarks,
                                 @SubjectMax, @SubjectMin, @ActivityMax, @Remarks,
                                 @gradeTypeId, @Status, @schoolId, @createdBy)",
                            new
                            {
                                ExamName = string.IsNullOrWhiteSpace(r.ExamName) ? null : r.ExamName.Trim(),
                                examTypeId, classId, subjectId, yearId,
                                Category = string.IsNullOrWhiteSpace(r.Category) ? null : r.Category.Trim(),
                                examDate,
                                r.TotalMarks, r.ExamMinMarks, r.SubjectMax, r.SubjectMin, r.ActivityMax,
                                Remarks = string.IsNullOrWhiteSpace(r.Remarks) ? null : r.Remarks.Trim(),
                                gradeTypeId,
                                Status = string.IsNullOrWhiteSpace(r.Status) ? "Active" : r.Status.Trim(),
                                schoolId, createdBy,
                            });
                        result.Imported++;
                    }
                    catch (Exception ex) { Fail(ex.Message); }
                }
            }
            return result;
        }

        private class NameId     { public int Id { get; set; } public string Name { get; set; } }
        private class NameIdYear { public int Id { get; set; } public string Name { get; set; } public int? YearId { get; set; } }
    }
}
