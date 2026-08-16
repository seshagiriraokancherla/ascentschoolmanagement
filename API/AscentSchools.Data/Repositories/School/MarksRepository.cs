using AscentSchools.Core.DTOs.School.Marks;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System.Collections.Generic;
using System.Linq;

namespace AscentSchools.Data.Repositories.School
{
    public class MarksRepository
    {
        private readonly IConnectionFactory _db;
        public MarksRepository(IConnectionFactory db) { _db = db; }

        // ── Exam Types ────────────────────────────────────────────────────────

        public IEnumerable<ExamTypeDto> GetExamTypes(string tenantDbName, int schoolId, int? academicYearId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<ExamTypeDto>(
                    @"SELECT exam_type_id ExamTypeId, exam_type_name ExamTypeName,
                             academic_year_id AcademicYearId, display_order DisplayOrder, status Status
                      FROM exam_types
                      WHERE school_id = @schoolId
                        AND status = 'Active'
                        AND (@academicYearId IS NULL OR academic_year_id = @academicYearId)
                      ORDER BY ISNULL(display_order, 999), exam_type_name",
                    new { schoolId, academicYearId });
        }

        public int CreateExamType(string tenantDbName, int schoolId, string createdBy, SaveExamTypeRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QuerySingle<int>(
                    @"INSERT INTO exam_types (exam_type_name, academic_year_id, display_order, school_id, created_by)
                      VALUES (@name, @academicYearId, @displayOrder, @schoolId, @createdBy);
                      SELECT SCOPE_IDENTITY();",
                    new { name = req.ExamTypeName, req.AcademicYearId, req.DisplayOrder, schoolId, createdBy });
        }

        public void UpdateExamType(string tenantDbName, int schoolId, int id, SaveExamTypeRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE exam_types SET exam_type_name = @name, academic_year_id = @academicYearId,
                        display_order = @displayOrder
                      WHERE exam_type_id = @id AND school_id = @schoolId",
                    new { name = req.ExamTypeName, req.AcademicYearId, req.DisplayOrder, id, schoolId });
        }

        // ── Current academic year (mobile has no year picker) ───────────────────

        public int GetCurrentAcademicYearId(string tenantDbName, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.ExecuteScalar<int>(
                    @"SELECT TOP 1 academic_year_id FROM academic_years
                      WHERE school_id = @schoolId AND status = 'Active'
                      ORDER BY academic_year_id DESC",
                    new { schoolId });
        }

        // The per-subject exam config (max / activity max / exam_id) resolved from
        // exam_master for a class+exam type+year. Shared by the grid and single-subject
        // reads. Falls back to max 100 / no activity when the exam isn't defined yet.
        private const string ExamConfigApply =
            @"OUTER APPLY (
                  SELECT TOP 1 em.id ExamId, em.sub_max_marks SubMax, em.activity_max_marks ActMax
                  FROM exam_master em
                  WHERE em.school_id = @schoolId AND em.academic_year_id = @academicYearId
                    AND em.exam_type_id = @examTypeId AND em.class_id = @classId
                    AND em.subject_id = cs.subject_id
                    AND ISNULL(em.exam_status, 'Active') = 'Active'
                  ORDER BY em.id DESC
              ) em";

        // ── Marks Grid (web — every mapped subject of the class) ────────────────

        public MarksGridDto GetMarksGrid(string tenantDbName, int schoolId,
            int classId, int sectionId, int examTypeId, int academicYearId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var subjects = conn.Query<SubjectHeaderDto>(
                    @"SELECT cs.subject_id SubjectId, sub.subject_name SubjectName,
                             em.ExamId,
                             CAST(ISNULL(em.SubMax, 100) AS DECIMAL(6,2)) MaxMarks,
                             em.ActMax ActivityMaxMarks,
                             CASE WHEN ISNULL(em.ActMax, 0) > 0 THEN 1 ELSE 0 END HasActivity
                      FROM class_subjects cs
                      JOIN subjects sub ON sub.subject_id = cs.subject_id
                      " + ExamConfigApply + @"
                      WHERE cs.school_id = @schoolId AND cs.academic_year_id = @academicYearId
                        AND cs.class_id = @classId AND cs.status = 'Active'
                      ORDER BY ISNULL(cs.display_order, 9999), sub.subject_name",
                    new { schoolId, academicYearId, examTypeId, classId }).ToList();

                var students = conn.Query<StudentRow>(
                    @"SELECT student_id StudentId, student_name StudentName, admission_no AdmissionNo
                      FROM students
                      WHERE class_id = @classId AND section_id = @sectionId
                        AND school_id = @schoolId AND status IN ('Active', 'Y')
                      ORDER BY student_name",
                    new { classId, sectionId, schoolId }).ToList();

                var marks = conn.Query<MarkRow>(
                    @"SELECT sm.student_id StudentId, sm.subject_id SubjectId,
                             sm.marks_obtained MarksObtained, sm.activity_marks ActivityMarks,
                             sm.is_absent IsAbsent
                      FROM student_marks sm
                      JOIN students s ON s.student_id = sm.student_id
                      WHERE s.class_id = @classId AND s.section_id = @sectionId
                        AND sm.exam_type_id = @examTypeId
                        AND sm.academic_year_id = @academicYearId AND sm.school_id = @schoolId",
                    new { classId, sectionId, examTypeId, academicYearId, schoolId }).ToLookup(m => m.StudentId);

                var rows = students.Select(s => new StudentMarksRowDto
                {
                    StudentId   = s.StudentId,
                    StudentName = s.StudentName,
                    AdmissionNo = s.AdmissionNo,
                    Marks       = subjects.Select(sub =>
                    {
                        var existing = marks[s.StudentId].FirstOrDefault(m => m.SubjectId == sub.SubjectId);
                        return new MarkCellDto
                        {
                            SubjectId        = sub.SubjectId,
                            MarksObtained    = existing?.MarksObtained,
                            ActivityMarks    = existing?.ActivityMarks,
                            MaxMarks         = sub.MaxMarks,
                            ActivityMaxMarks = sub.ActivityMaxMarks,
                            IsAbsent         = existing?.IsAbsent ?? false,
                        };
                    }).ToList()
                });

                return new MarksGridDto { Subjects = subjects, Rows = rows };
            }
        }

        // ── Single subject (mobile) ─────────────────────────────────────────────

        public SubjectMarksDto GetSubjectMarks(string tenantDbName, int schoolId,
            int classId, int sectionId, int examTypeId, int subjectId, int academicYearId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                // Reuse ExamConfigApply by aliasing the subject as cs.subject_id.
                var header = conn.QuerySingleOrDefault<SubjectHeaderDto>(
                    @"SELECT sub.subject_id SubjectId, sub.subject_name SubjectName,
                             em.ExamId,
                             CAST(ISNULL(em.SubMax, 100) AS DECIMAL(6,2)) MaxMarks,
                             em.ActMax ActivityMaxMarks,
                             CASE WHEN ISNULL(em.ActMax, 0) > 0 THEN 1 ELSE 0 END HasActivity
                      FROM (SELECT @subjectId AS subject_id) cs
                      JOIN subjects sub ON sub.subject_id = cs.subject_id AND sub.school_id = @schoolId
                      " + ExamConfigApply,
                    new { schoolId, academicYearId, examTypeId, classId, subjectId });

                if (header == null) return null;

                var students = conn.Query<StudentSubjectMarkDto>(
                    @"SELECT s.student_id StudentId, s.student_name StudentName, s.admission_no AdmissionNo,
                             sm.marks_obtained MarksObtained, sm.activity_marks ActivityMarks,
                             ISNULL(sm.is_absent, 0) IsAbsent
                      FROM students s
                      LEFT JOIN student_marks sm
                             ON sm.student_id = s.student_id AND sm.subject_id = @subjectId
                            AND sm.exam_type_id = @examTypeId AND sm.academic_year_id = @academicYearId
                            AND sm.school_id = @schoolId
                      WHERE s.class_id = @classId AND s.section_id = @sectionId
                        AND s.school_id = @schoolId AND s.status IN ('Active', 'Y')
                      ORDER BY s.student_name",
                    new { schoolId, academicYearId, examTypeId, classId, sectionId, subjectId }).ToList();

                return new SubjectMarksDto
                {
                    SubjectId        = header.SubjectId,
                    SubjectName      = header.SubjectName,
                    ExamId           = header.ExamId,
                    MaxMarks         = header.MaxMarks,
                    ActivityMaxMarks = header.ActivityMaxMarks,
                    HasActivity      = header.HasActivity,
                    Students         = students,
                };
            }
        }

        // ── Save Marks (upsert per entry; carries exam_id + activity) ───────────

        public void SaveMarks(string tenantDbName, int schoolId, string enteredBy, SaveMarksRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                foreach (var entry in req.Entries)
                {
                    conn.Execute(
                        @"MERGE student_marks AS target
                          USING (VALUES (@studentId, @subjectId, @examTypeId, @academicYearId, @schoolId))
                                AS source (student_id, subject_id, exam_type_id, academic_year_id, school_id)
                          ON  target.student_id       = source.student_id
                          AND target.subject_id       = source.subject_id
                          AND target.exam_type_id     = source.exam_type_id
                          AND target.academic_year_id = source.academic_year_id
                          AND target.school_id        = source.school_id
                          WHEN MATCHED THEN
                              UPDATE SET marks_obtained = @marksObtained, max_marks = @maxMarks,
                                         activity_marks = @activityMarks, activity_max_marks = @activityMaxMarks,
                                         exam_id = @examId, is_absent = @isAbsent,
                                         updated_by = @enteredBy, updated_at = GETDATE()
                          WHEN NOT MATCHED THEN
                              INSERT (student_id, subject_id, exam_type_id, exam_id, academic_year_id,
                                      marks_obtained, max_marks, activity_marks, activity_max_marks,
                                      is_absent, school_id, entered_by)
                              VALUES (@studentId, @subjectId, @examTypeId, @examId, @academicYearId,
                                      @marksObtained, @maxMarks, @activityMarks, @activityMaxMarks,
                                      @isAbsent, @schoolId, @enteredBy);",
                        new
                        {
                            entry.StudentId,
                            entry.SubjectId,
                            req.ExamTypeId,
                            examId = entry.ExamId,
                            req.AcademicYearId,
                            schoolId,
                            marksObtained    = entry.IsAbsent ? 0 : entry.MarksObtained,
                            maxMarks         = entry.MaxMarks > 0 ? entry.MaxMarks : 100,
                            activityMarks    = entry.IsAbsent ? (decimal?)null : entry.ActivityMarks,
                            activityMaxMarks = entry.ActivityMaxMarks,
                            entry.IsAbsent,
                            enteredBy,
                        });
                }
            }
        }

        // ── Internal row types ──────────────────────────────────────────────────

        private class StudentRow
        {
            public long   StudentId   { get; set; }
            public string StudentName { get; set; }
            public string AdmissionNo { get; set; }
        }

        private class MarkRow
        {
            public long     StudentId     { get; set; }
            public int      SubjectId     { get; set; }
            public decimal? MarksObtained { get; set; }
            public decimal? ActivityMarks { get; set; }
            public bool     IsAbsent      { get; set; }
        }
    }
}
