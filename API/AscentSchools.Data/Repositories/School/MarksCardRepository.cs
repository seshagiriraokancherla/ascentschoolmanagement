using AscentSchools.Core.DTOs.School.Marks;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AscentSchools.Data.Repositories.School
{
    /// <summary>
    /// Marks card (progress report) data for one section + exam: each student's
    /// subject marks, subject grades (Grade Types, via the grade type chosen on each
    /// subject's Exam Master row), total, total grade (the Marks Grade Master scale
    /// chosen for the exam+class) and rank within the section.
    /// </summary>
    public class MarksCardRepository
    {
        private readonly IConnectionFactory _db;
        public MarksCardRepository(IConnectionFactory db) { _db = db; }

        /// <summary>Returns null when the section does not belong to the class.</summary>
        public MarksCardDataDto GetMarksCards(string tenantDbName, int schoolId,
            int academicYearId, int examTypeId, int classId, int sectionId)
        {
            var p = new { schoolId, academicYearId, examTypeId, classId, sectionId };

            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var head = conn.QuerySingle<HeaderRow>(
                    @"SELECT (SELECT academic_year FROM academic_years
                              WHERE academic_year_id = @academicYearId AND school_id = @schoolId) AcademicYear,
                             (SELECT exam_type_name FROM exam_types
                              WHERE exam_type_id = @examTypeId AND school_id = @schoolId) ExamTypeName,
                             (SELECT class_name FROM classes WHERE class_id = @classId) ClassName,
                             (SELECT section_name FROM sections
                              WHERE section_id = @sectionId AND class_id = @classId) SectionName", p);
                if (head.SectionName == null) return null;

                // Subjects mapped to the class for the year, with the exam's per-subject config.
                var subjects = conn.Query<SubjectRow>(
                    @"SELECT cs.subject_id SubjectId, sub.subject_name SubjectName,
                             CAST(ISNULL(em.SubMax, 100) AS DECIMAL(6,2)) MaxMarks,
                             CAST(em.ActMax AS DECIMAL(6,2)) ActivityMaxMarks,
                             em.GradeTypeId
                      FROM class_subjects cs
                      JOIN subjects sub ON sub.subject_id = cs.subject_id
                      OUTER APPLY (
                          SELECT TOP 1 em.sub_max_marks SubMax, em.activity_max_marks ActMax,
                                       em.grade_type_id GradeTypeId
                          FROM exam_master em
                          WHERE em.school_id = @schoolId AND em.academic_year_id = @academicYearId
                            AND em.exam_type_id = @examTypeId AND em.class_id = @classId
                            AND em.subject_id = cs.subject_id
                            AND ISNULL(em.exam_status, 'Active') = 'Active'
                          ORDER BY em.id DESC
                      ) em
                      WHERE cs.school_id = @schoolId AND cs.academic_year_id = @academicYearId
                        AND cs.class_id = @classId AND cs.status = 'Active'
                      ORDER BY ISNULL(cs.display_order, 9999), sub.subject_name", p).ToList();

                // Students of the SELECTED year only — promotion leaves last year's rows
                // Active, so filtering by class+section alone would mix years.
                var students = conn.Query<StudentRow>(
                    @"SELECT student_id StudentId, student_name StudentName,
                             admission_no AdmissionNo, father_name FatherName
                      FROM students
                      WHERE school_id = @schoolId AND academic_year_id = @academicYearId
                        AND class_id = @classId AND section_id = @sectionId
                        AND status IN ('Active', 'Y')
                      ORDER BY student_name", p).ToList();

                var marks = conn.Query<MarkRow>(
                    @"SELECT sm.student_id StudentId, sm.subject_id SubjectId,
                             sm.marks_obtained MarksObtained, sm.activity_marks ActivityMarks,
                             ISNULL(sm.is_absent, 0) IsAbsent
                      FROM student_marks sm
                      JOIN students s ON s.student_id = sm.student_id
                      WHERE s.school_id = @schoolId AND s.academic_year_id = @academicYearId
                        AND s.class_id = @classId AND s.section_id = @sectionId
                        AND sm.exam_type_id = @examTypeId AND sm.academic_year_id = @academicYearId
                        AND sm.school_id = @schoolId", p).ToLookup(m => m.StudentId);

                // Subject grades: exam_master.grade_type_id points at ONE grade_types row; its
                // grade_name identifies the scheme, whose bands are every row with that name.
                var gradeRows = conn.Query<GradeTypeRow>(
                    @"SELECT id Id, grade_name GradeName, subject_id SubjectId,
                             min_marks MinMarks, max_marks MaxMarks, grade Grade,
                             ISNULL(status, 'Active') Status
                      FROM grade_types WHERE school_id = @schoolId", p).ToList();
                var schemeNameById = gradeRows.ToDictionary(g => g.Id, g => g.GradeName);

                // Total grade: the grading scale the class's exam rows point at. The scale is
                // set per exam row, so rows could disagree — the most-used one wins and the
                // page warns (ScaleConflict).
                var scales = conn.Query<ScaleRow>(
                    @"SELECT m.id Id, m.scale_name ScaleName, COUNT(1) Rows
                      FROM exam_master em
                      JOIN marks_grade_master m ON m.id = em.marks_grade_master_id
                      WHERE em.school_id = @schoolId AND em.academic_year_id = @academicYearId
                        AND em.exam_type_id = @examTypeId AND em.class_id = @classId
                        AND ISNULL(em.exam_status, 'Active') = 'Active'
                      GROUP BY m.id, m.scale_name
                      ORDER BY COUNT(1) DESC, m.id", p).ToList();

                var scale      = scales.FirstOrDefault();
                var scaleName  = scale?.ScaleName;
                var totalBands = new List<Band>();
                if (scale != null)
                    totalBands = conn.Query<Band>(
                        @"SELECT min_marks Min, max_marks Max, grade Grade
                          FROM marks_grade_master_bands
                          WHERE scale_id = @scaleId AND school_id = @schoolId",
                        new { scaleId = scale.Id, schoolId }).ToList();

                // Per subject: the band list of its grade scheme (subject-specific rows win
                // over the scheme's "all subjects" rows).
                var subjectBands = subjects.ToDictionary(s => s.SubjectId, s =>
                {
                    if (s.GradeTypeId == null || !schemeNameById.TryGetValue(s.GradeTypeId.Value, out var scheme))
                        return new List<Band>();
                    var inScheme = gradeRows.Where(g => g.Status == "Active"
                        && string.Equals(g.GradeName, scheme, StringComparison.OrdinalIgnoreCase)
                        && g.MinMarks != null && g.MaxMarks != null).ToList();
                    var specific = inScheme.Where(g => g.SubjectId == s.SubjectId).ToList();
                    return (specific.Count > 0 ? specific : inScheme.Where(g => g.SubjectId == null))
                        .Select(g => new Band { Min = (decimal)g.MinMarks.Value, Max = (decimal)g.MaxMarks.Value, Grade = g.Grade })
                        .ToList();
                });

                var maxTotal = subjects.Sum(s => s.MaxMarks + (s.ActivityMaxMarks ?? 0));

                var cards = students.Select(st =>
                {
                    var byId = marks[st.StudentId].GroupBy(m => m.SubjectId).ToDictionary(g => g.Key, g => g.First());
                    var lines = subjects.Select(sub =>
                    {
                        byId.TryGetValue(sub.SubjectId, out var m);
                        var absent  = m?.IsAbsent ?? false;
                        var entered = m != null && (m.MarksObtained != null || m.ActivityMarks != null);
                        decimal? subTotal = !absent && entered
                            ? (m.MarksObtained ?? 0) + (m.ActivityMarks ?? 0)
                            : (decimal?)null;
                        return new MarksCardLineDto
                        {
                            SubjectId     = sub.SubjectId,
                            ActivityMarks = absent ? null : m?.ActivityMarks,
                            MarksObtained = absent ? null : m?.MarksObtained,
                            IsAbsent      = absent,
                            Entered       = entered,
                            SubjectTotal  = subTotal,
                            Grade         = subTotal.HasValue ? PickGrade(subjectBands[sub.SubjectId], subTotal.Value) : null,
                        };
                    }).ToList();

                    var hasMarks = lines.Any(l => l.Entered || l.IsAbsent);
                    var total    = lines.Sum(l => l.SubjectTotal ?? 0);
                    return new MarksCardStudentDto
                    {
                        StudentId   = st.StudentId,
                        StudentName = st.StudentName,
                        AdmissionNo = st.AdmissionNo,
                        FatherName  = st.FatherName,
                        Lines       = lines,
                        HasMarks    = hasMarks,
                        Total       = total,
                        MaxTotal    = maxTotal,
                        TotalGrade  = hasMarks ? PickGrade(totalBands, total) : null,
                    };
                }).ToList();

                // Rank within the section — equal totals share a rank and the next rank
                // skips (1, 1, 3). Students with no marks at all are not ranked.
                var ranked = cards.Where(c => c.HasMarks).OrderByDescending(c => c.Total).ToList();
                for (int i = 0; i < ranked.Count; i++)
                    ranked[i].Rank = i > 0 && ranked[i].Total == ranked[i - 1].Total ? ranked[i - 1].Rank : i + 1;

                // ── Attendance particulars: June (start year) → March (end year) ──────
                var startYear = AcademicStartYear(head.AcademicYear);
                var months = Enumerable.Range(0, 10).Select(i =>
                {
                    var d = new DateTime(startYear, 6, 1).AddMonths(i);
                    return new MarksCardMonthDto
                    {
                        Year = d.Year, Month = d.Month,
                        Label = d.ToString("MMM", System.Globalization.CultureInfo.InvariantCulture),
                    };
                }).ToList();
                var ap = new
                {
                    schoolId, academicYearId, classId, sectionId,
                    from = new DateTime(startYear, 6, 1),
                    to   = new DateTime(startYear + 1, 3, 31),
                };

                // Working days = distinct days attendance was marked for this class+section
                // (the selected year's students); Holiday rows are not working days.
                var working = conn.Query<MonthCount>(
                    @"SELECT YEAR(sa.attendance_date) Y, MONTH(sa.attendance_date) M,
                             CAST(COUNT(DISTINCT sa.attendance_date) AS DECIMAL(9,2)) Value
                      FROM student_attendance sa
                      JOIN students s ON s.student_id = sa.student_id
                      WHERE s.school_id = @schoolId AND s.academic_year_id = @academicYearId
                        AND s.class_id = @classId AND s.section_id = @sectionId
                        AND sa.attendance_date BETWEEN @from AND @to
                        AND sa.status <> 'Holiday'
                      GROUP BY YEAR(sa.attendance_date), MONTH(sa.attendance_date)", ap)
                    .ToDictionary(r => (r.Y, r.M), r => (int)r.Value);

                // Present days — same weighting as the web app's attendance %:
                // Present / Late = 1 day, HalfDay = 0.5.
                var present = conn.Query<StudentMonthCount>(
                    @"SELECT sa.student_id StudentId, YEAR(sa.attendance_date) Y, MONTH(sa.attendance_date) M,
                             CAST(SUM(CASE WHEN sa.status IN ('Present', 'Late') THEN 1.0
                                           WHEN sa.status = 'HalfDay'           THEN 0.5
                                           ELSE 0 END) AS DECIMAL(9,2)) Value
                      FROM student_attendance sa
                      JOIN students s ON s.student_id = sa.student_id
                      WHERE s.school_id = @schoolId AND s.academic_year_id = @academicYearId
                        AND s.class_id = @classId AND s.section_id = @sectionId
                        AND sa.attendance_date BETWEEN @from AND @to
                      GROUP BY sa.student_id, YEAR(sa.attendance_date), MONTH(sa.attendance_date)", ap)
                    .ToDictionary(r => (r.StudentId, r.Y, r.M), r => r.Value);

                foreach (var m in months)
                    m.WorkingDays = working.TryGetValue((m.Year, m.Month), out var w) ? w : (int?)null;

                foreach (var c in cards)
                {
                    // A month the class had attendance but this student has no row counts as 0.
                    c.PresentDays = months.Select(m => m.WorkingDays == null
                        ? (decimal?)null
                        : (present.TryGetValue((c.StudentId, m.Year, m.Month), out var days) ? days : 0m)).ToList();
                    c.TotalPresent = c.PresentDays.Sum(d => d ?? 0);
                }

                return new MarksCardDataDto
                {
                    AcademicYear  = head.AcademicYear,
                    ExamTypeName  = head.ExamTypeName,
                    ClassName     = head.ClassName,
                    SectionName   = head.SectionName,
                    ScaleName     = scaleName,
                    ScaleHasBands = totalBands.Count > 0,
                    ScaleConflict = scales.Count > 1,
                    Subjects      = subjects.Select(s => new MarksCardSubjectDto
                    {
                        SubjectId        = s.SubjectId,
                        SubjectName      = s.SubjectName,
                        MaxMarks         = s.MaxMarks,
                        ActivityMaxMarks = s.ActivityMaxMarks,
                        HasActivity      = (s.ActivityMaxMarks ?? 0) > 0,
                        HasGradeType     = subjectBands[s.SubjectId].Count > 0,
                    }).ToList(),
                    Students      = cards,
                    AttendanceMonths = months,
                    TotalWorkingDays = months.Sum(m => m.WorkingDays ?? 0),
                };
            }
        }

        /// <summary>
        /// The calendar year the academic year starts in — the first 4-digit number of its
        /// label ("2026-27" / "2026-2027" → 2026). Falls back to the current Indian academic
        /// year (June onwards = this year) when the label has none.
        /// </summary>
        private static int AcademicStartYear(string academicYear)
        {
            var m = Regex.Match(academicYear ?? "", @"\d{4}");
            if (m.Success) return int.Parse(m.Value);
            var istNow = DateTime.UtcNow.AddHours(5.5);
            return istNow.Month >= 6 ? istNow.Year : istNow.Year - 1;
        }

        /// <summary>
        /// The band with the highest minimum that the value reaches. Equivalent to
        /// min ≤ value ≤ max for contiguous bands, and puts a value that falls in a gap
        /// between integer bands (e.g. 90.5 between 81–90 and 91–100) into the lower band.
        /// </summary>
        private static string PickGrade(List<Band> bands, decimal value)
        {
            return bands.Where(b => b.Min <= value)
                        .OrderByDescending(b => b.Min)
                        .Select(b => b.Grade)
                        .FirstOrDefault();
        }

        // ── Internal row types ──────────────────────────────────────────────────

        private class HeaderRow
        {
            public string AcademicYear { get; set; }
            public string ExamTypeName { get; set; }
            public string ClassName    { get; set; }
            public string SectionName  { get; set; }
        }

        private class SubjectRow
        {
            public int      SubjectId        { get; set; }
            public string   SubjectName      { get; set; }
            public decimal  MaxMarks         { get; set; }
            public decimal? ActivityMaxMarks { get; set; }
            public int?     GradeTypeId      { get; set; }
        }

        private class StudentRow
        {
            public long   StudentId   { get; set; }
            public string StudentName { get; set; }
            public string AdmissionNo { get; set; }
            public string FatherName  { get; set; }
        }

        private class MarkRow
        {
            public long     StudentId     { get; set; }
            public int      SubjectId     { get; set; }
            public decimal? MarksObtained { get; set; }
            public decimal? ActivityMarks { get; set; }
            public bool     IsAbsent      { get; set; }
        }

        private class GradeTypeRow
        {
            public int     Id        { get; set; }
            public string  GradeName { get; set; }
            public int?    SubjectId { get; set; }
            public double? MinMarks  { get; set; }
            public double? MaxMarks  { get; set; }
            public string  Grade     { get; set; }
            public string  Status    { get; set; }
        }

        private class ScaleRow
        {
            public int    Id        { get; set; }
            public string ScaleName { get; set; }
            public int    Rows      { get; set; }
        }

        private class MonthCount
        {
            public int     Y     { get; set; }
            public int     M     { get; set; }
            public decimal Value { get; set; }
        }

        private class StudentMonthCount
        {
            public long    StudentId { get; set; }
            public int     Y         { get; set; }
            public int     M         { get; set; }
            public decimal Value     { get; set; }
        }

        private class Band
        {
            public decimal Min   { get; set; }
            public decimal Max   { get; set; }
            public string  Grade { get; set; }
        }
    }
}
