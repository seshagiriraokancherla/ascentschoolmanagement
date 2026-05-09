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

        // ── Marks Grid ────────────────────────────────────────────────────────

        public MarksGridDto GetMarksGrid(string tenantDbName, int schoolId, int classId, int sectionId, int examTypeId, int academicYearId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var subjects = conn.Query<SubjectHeaderDto>(
                    @"SELECT subject_id SubjectId, subject_name SubjectName
                      FROM subjects
                      WHERE school_id = @schoolId AND ISNULL(status, 'Active') = 'Active'
                      ORDER BY subject_name",
                    new { schoolId }).ToList();

                var students = conn.Query<StudentRow>(
                    @"SELECT student_id StudentId, student_name StudentName, admission_no AdmissionNo
                      FROM students
                      WHERE class_id = @classId AND section_id = @sectionId
                        AND school_id = @schoolId AND status IN ('Active', 'Y')
                      ORDER BY student_name",
                    new { classId, sectionId, schoolId }).ToList();

                var marks = conn.Query<MarkRow>(
                    @"SELECT sm.student_id StudentId, sm.subject_id SubjectId,
                             sm.marks_obtained MarksObtained, sm.max_marks MaxMarks, sm.is_absent IsAbsent
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
                            SubjectId     = sub.SubjectId,
                            MarksObtained = existing?.MarksObtained,
                            MaxMarks      = existing?.MaxMarks ?? 100,
                            IsAbsent      = existing?.IsAbsent ?? false,
                        };
                    }).ToList()
                });

                return new MarksGridDto { Subjects = subjects, Rows = rows };
            }
        }

        // ── Save Marks (upsert per entry) ─────────────────────────────────────

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
                                         is_absent = @isAbsent, updated_by = @enteredBy, updated_at = GETDATE()
                          WHEN NOT MATCHED THEN
                              INSERT (student_id, subject_id, exam_type_id, academic_year_id,
                                      marks_obtained, max_marks, is_absent, school_id, entered_by)
                              VALUES (@studentId, @subjectId, @examTypeId, @academicYearId,
                                      @marksObtained, @maxMarks, @isAbsent, @schoolId, @enteredBy);",
                        new
                        {
                            entry.StudentId,
                            entry.SubjectId,
                            req.ExamTypeId,
                            req.AcademicYearId,
                            schoolId,
                            marksObtained = entry.IsAbsent ? 0 : entry.MarksObtained,
                            maxMarks      = req.MaxMarks,
                            entry.IsAbsent,
                            enteredBy,
                        });
                }
            }
        }

        // ── Internal row types ────────────────────────────────────────────────

        private class StudentRow
        {
            public long   StudentId   { get; set; }
            public string StudentName { get; set; }
            public string AdmissionNo { get; set; }
        }

        private class MarkRow
        {
            public long    StudentId     { get; set; }
            public int     SubjectId     { get; set; }
            public decimal MarksObtained { get; set; }
            public decimal MaxMarks      { get; set; }
            public bool    IsAbsent      { get; set; }
        }
    }
}
