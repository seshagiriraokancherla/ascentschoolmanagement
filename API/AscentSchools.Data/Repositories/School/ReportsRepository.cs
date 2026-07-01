using AscentSchools.Core.DTOs.School.Reports;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AscentSchools.Data.Repositories.School
{
    public class ReportsRepository
    {
        private readonly IConnectionFactory _db;
        public ReportsRepository(IConnectionFactory db) { _db = db; }

        public IEnumerable<StrengthRowDto> GetStrength(string tenantDbName, int schoolId, int academicYearId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<StrengthRowDto>(
                    @"SELECT
                        c.class_name                                              ClassName,
                        ISNULL(sec.section_name, '-')                             SectionName,
                        SUM(CASE WHEN UPPER(s.gender) IN ('MALE','M','BOY','BOYS')
                                 THEN 1 ELSE 0 END)                              Boys,
                        SUM(CASE WHEN UPPER(s.gender) IN ('FEMALE','F','GIRL','GIRLS')
                                 THEN 1 ELSE 0 END)                              Girls,
                        SUM(CASE WHEN UPPER(ISNULL(s.gender,'')) NOT IN
                                      ('MALE','M','BOY','BOYS','FEMALE','F','GIRL','GIRLS')
                                 THEN 1 ELSE 0 END)                              Others,
                        COUNT(*)                                                  Total
                      FROM students s
                      LEFT JOIN classes  c   ON c.class_id   = s.class_id
                      LEFT JOIN sections sec ON sec.section_id = s.section_id
                      WHERE s.school_id        = @schoolId
                        AND s.academic_year_id = @academicYearId
                        AND s.status IN ('Active', 'Y')
                      GROUP BY s.class_id, c.class_name, c.sequence_no, s.section_id, sec.section_name
                      ORDER BY c.sequence_no, sec.section_name",
                    new { schoolId, academicYearId });
        }

        public IEnumerable<AbsentRowDto> GetAbsents(
            string tenantDbName, int schoolId,
            DateTime dateFrom, DateTime dateTo,
            int? classId, int? sectionId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<AbsentRowDto>(
                    @"SELECT
                        CONVERT(VARCHAR(10), sa.attendance_date, 105) AttendanceDate,
                        s.admission_no   AdmissionNo,
                        s.student_name   StudentName,
                        c.class_name     ClassName,
                        ISNULL(sec.section_name, '-') SectionName,
                        s.father_name    FatherName,
                        s.father_mobile  FatherMobile
                      FROM student_attendance sa
                      JOIN students s   ON s.student_id   = sa.student_id
                      LEFT JOIN classes  c   ON c.class_id   = s.class_id
                      LEFT JOIN sections sec ON sec.section_id = s.section_id
                      WHERE sa.school_id      = @schoolId
                        AND sa.status         = 'Absent'
                        AND sa.attendance_date BETWEEN @dateFrom AND @dateTo
                        AND (@classId   IS NULL OR s.class_id   = @classId)
                        AND (@sectionId IS NULL OR s.section_id = @sectionId)
                      ORDER BY sa.attendance_date, c.class_name, s.student_name",
                    new { schoolId, dateFrom, dateTo, classId, sectionId });
        }

        public AttendanceSheetDto GetAttendanceSheet(
            string tenantDbName, int schoolId, int classId, int sectionId, int month, int year)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var meta = conn.QueryFirstOrDefault<ClassMeta>(
                    @"SELECT c.class_name ClassName, sec.section_name SectionName
                      FROM classes c
                      JOIN sections sec ON sec.section_id = @sectionId
                      WHERE c.class_id = @classId",
                    new { classId, sectionId });

                // Scope to the current academic year — this report has no year picker, and
                // without this the multi-year student model (classes/sections shared across
                // years) returns prior-year/promoted students too.
                var students = conn.Query<StudentMeta>(
                    @"SELECT student_id StudentId, admission_no AdmissionNo, student_name StudentName
                      FROM students
                      WHERE class_id = @classId AND section_id = @sectionId
                        AND school_id = @schoolId AND status IN ('Active','Y')
                        AND academic_year_id = (
                            SELECT TOP 1 academic_year_id FROM academic_years
                            WHERE school_id = @schoolId AND status = 'Active'
                            ORDER BY academic_year_id DESC)
                      ORDER BY student_name",
                    new { classId, sectionId, schoolId }).ToList();

                var records = conn.Query<DayRecord>(
                    @"SELECT student_id StudentId,
                             DAY(attendance_date) DayNo,
                             status Status
                      FROM student_attendance
                      WHERE school_id = @schoolId
                        AND MONTH(attendance_date) = @month
                        AND YEAR(attendance_date)  = @year
                        AND student_id IN @ids",
                    new { schoolId, month, year, ids = students.Select(s => s.StudentId).ToList() })
                    .ToLookup(r => r.StudentId);

                int daysInMonth = DateTime.DaysInMonth(year, month);

                var rows = students.Select(s =>
                {
                    var dayMap = new Dictionary<int, string>();
                    int p = 0, a = 0, l = 0, h = 0;
                    foreach (var rec in records[s.StudentId])
                    {
                        dayMap[rec.DayNo] = rec.Status == "Present" ? "P"
                                          : rec.Status == "Absent"  ? "A"
                                          : rec.Status == "Late"    ? "L"
                                          : rec.Status == "HalfDay" ? "H"
                                          : rec.Status?.Substring(0, 1).ToUpper() ?? "";
                        if (rec.Status == "Present") p++;
                        else if (rec.Status == "Absent") a++;
                        else if (rec.Status == "Late")   l++;
                        else if (rec.Status == "HalfDay") h++;
                    }
                    return new AttendanceSheetRowDto
                    {
                        StudentId   = s.StudentId,
                        AdmissionNo = s.AdmissionNo,
                        StudentName = s.StudentName,
                        Days        = dayMap,
                        Present     = p,
                        Absent      = a,
                        Late        = l,
                        HalfDay     = h,
                    };
                });

                return new AttendanceSheetDto
                {
                    Month       = month,
                    Year        = year,
                    DaysInMonth = daysInMonth,
                    ClassName   = meta?.ClassName   ?? "",
                    SectionName = meta?.SectionName ?? "",
                    Rows        = rows,
                };
            }
        }

        public IEnumerable<TransportStudentRowDto> GetTransportStudents(
            string tenantDbName, int schoolId, int academicYearId, int? busRouteId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<TransportStudentRowDto>(
                    @"SELECT
                        s.admission_no              AdmissionNo,
                        s.student_name              StudentName,
                        c.class_name                ClassName,
                        ISNULL(sec.section_name,'-') SectionName,
                        br.route_name               RouteName,
                        b.bus_name                  BusName,
                        s.bus_trip                  BusTrip,
                        s.father_name               FatherName,
                        s.father_mobile             FatherMobile
                      FROM students s
                      LEFT JOIN classes    c   ON c.class_id    = s.class_id
                      LEFT JOIN sections   sec ON sec.section_id = s.section_id
                      LEFT JOIN bus_routes br  ON br.bus_route_id = s.bus_route_id
                      LEFT JOIN buses      b   ON b.bus_id       = s.bus_id
                      WHERE s.school_id        = @schoolId
                        AND s.academic_year_id = @academicYearId
                        AND s.transport_type   = 'Bus'
                        AND s.status IN ('Active','Y')
                        AND (@busRouteId IS NULL OR s.bus_route_id = @busRouteId)
                      ORDER BY br.route_name, c.class_name, s.student_name",
                    new { schoolId, academicYearId, busRouteId });
        }

        public FailedStudentsDto GetFailedStudents(
            string tenantDbName, int schoolId, int academicYearId,
            int examTypeId, int? classId, int? sectionId, int passMarkPct)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var examName = conn.ExecuteScalar<string>(
                    "SELECT exam_type_name FROM exam_types WHERE exam_type_id = @examTypeId AND school_id = @schoolId",
                    new { examTypeId, schoolId }) ?? "";

                var academicYear = conn.ExecuteScalar<string>(
                    "SELECT academic_year FROM academic_years WHERE academic_year_id = @academicYearId",
                    new { academicYearId }) ?? "";

                var subjects = conn.Query<SubjectMeta>(
                    @"SELECT subject_id SubjectId, subject_name SubjectName
                      FROM subjects
                      WHERE school_id = @schoolId AND ISNULL(status, 'Active') = 'Active'
                      ORDER BY subject_name",
                    new { schoolId }).ToList();

                var students = conn.Query<TopperStudentMeta>(
                    @"SELECT s.student_id StudentId, s.admission_no AdmissionNo, s.student_name StudentName,
                             c.class_name ClassName, ISNULL(sec.section_name,'-') SectionName,
                             s.class_id ClassId, ISNULL(s.section_id,0) SectionId
                      FROM students s
                      LEFT JOIN classes  c   ON c.class_id    = s.class_id
                      LEFT JOIN sections sec ON sec.section_id = s.section_id
                      WHERE s.school_id        = @schoolId
                        AND s.academic_year_id = @academicYearId
                        AND s.status IN ('Active','Y')
                        AND (@classId   IS NULL OR s.class_id   = @classId)
                        AND (@sectionId IS NULL OR s.section_id = @sectionId)
                      ORDER BY c.class_name, sec.section_name, s.student_name",
                    new { schoolId, academicYearId, classId, sectionId }).ToList();

                var ids = students.Select(s => s.StudentId).ToList();

                var marksLookup = ids.Any()
                    ? conn.Query<TopperMarkRow>(
                        @"SELECT student_id StudentId, subject_id SubjectId,
                                 marks_obtained MarksObtained, max_marks MaxMarks, is_absent IsAbsent
                          FROM student_marks
                          WHERE school_id        = @schoolId
                            AND exam_type_id     = @examTypeId
                            AND academic_year_id = @academicYearId
                            AND student_id IN @ids",
                        new { schoolId, examTypeId, academicYearId, ids })
                      .ToLookup(m => m.StudentId)
                    : Enumerable.Empty<TopperMarkRow>().ToLookup(m => m.StudentId);

                decimal threshold = passMarkPct / 100m;

                var rows = students
                    .Where(s => marksLookup[s.StudentId].Any())
                    .Select(s =>
                    {
                        var subjMarks    = new Dictionary<string, string>();
                        var failedSubjs  = new List<string>();
                        decimal obtained = 0, max = 0;

                        foreach (var sub in subjects)
                        {
                            var m = marksLookup[s.StudentId].FirstOrDefault(x => x.SubjectId == sub.SubjectId);
                            if (m == null)
                            {
                                subjMarks[sub.SubjectName] = "—";
                            }
                            else if (m.IsAbsent)
                            {
                                subjMarks[sub.SubjectName] = "AB";
                                failedSubjs.Add(sub.SubjectName);
                                max += m.MaxMarks;
                            }
                            else
                            {
                                subjMarks[sub.SubjectName] = $"{m.MarksObtained}/{m.MaxMarks}";
                                obtained += m.MarksObtained;
                                max      += m.MaxMarks;
                                if (m.MaxMarks > 0 && m.MarksObtained < m.MaxMarks * threshold)
                                    failedSubjs.Add(sub.SubjectName);
                            }
                        }

                        return new FailedStudentRowDto
                        {
                            AdmissionNo   = s.AdmissionNo,
                            StudentName   = s.StudentName,
                            ClassName     = s.ClassName,
                            SectionName   = s.SectionName,
                            FailedCount   = failedSubjs.Count,
                            TotalObtained = obtained,
                            TotalMax      = max,
                            Percentage    = max > 0 ? Math.Round(obtained / max * 100, 1) : 0m,
                            SubjectMarks  = subjMarks,
                            FailedSubjects = failedSubjs,
                        };
                    })
                    .Where(r => r.FailedCount > 0)
                    .OrderByDescending(r => r.FailedCount)
                    .ThenBy(r => r.ClassName)
                    .ThenBy(r => r.StudentName);

                return new FailedStudentsDto
                {
                    ExamName     = examName,
                    AcademicYear = academicYear,
                    PassMarkPct  = passMarkPct,
                    Subjects     = subjects.Select(s => s.SubjectName).ToList(),
                    Rows         = rows,
                };
            }
        }

        public ClassToppersDto GetClassToppers(
            string tenantDbName, int schoolId, int academicYearId,
            int classId, int sectionId, int topN)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var meta = conn.QueryFirstOrDefault<ClassMeta>(
                    @"SELECT c.class_name ClassName, sec.section_name SectionName
                      FROM classes c JOIN sections sec ON sec.section_id = @sectionId
                      WHERE c.class_id = @classId",
                    new { classId, sectionId });

                var academicYear = conn.ExecuteScalar<string>(
                    "SELECT academic_year FROM academic_years WHERE academic_year_id = @academicYearId",
                    new { academicYearId }) ?? "";

                var examTypes = conn.Query<ExamTypeMeta>(
                    @"SELECT exam_type_id ExamTypeId, exam_type_name ExamTypeName
                      FROM exam_types
                      WHERE school_id = @schoolId AND academic_year_id = @academicYearId
                        AND status = 'Active'
                      ORDER BY ISNULL(display_order, 999), exam_type_name",
                    new { schoolId, academicYearId }).ToList();

                var subjects = conn.Query<SubjectMeta>(
                    @"SELECT subject_id SubjectId, subject_name SubjectName
                      FROM subjects
                      WHERE school_id = @schoolId AND ISNULL(status, 'Active') = 'Active'
                      ORDER BY subject_name",
                    new { schoolId }).ToList();

                var students = conn.Query<StudentMeta>(
                    @"SELECT student_id StudentId, admission_no AdmissionNo, student_name StudentName
                      FROM students
                      WHERE class_id = @classId AND section_id = @sectionId
                        AND school_id = @schoolId AND academic_year_id = @academicYearId
                        AND status IN ('Active','Y')
                      ORDER BY student_name",
                    new { classId, sectionId, schoolId, academicYearId }).ToList();

                var ids = students.Select(s => s.StudentId).ToList();

                // Fetch ALL marks for this class across all exams in one query
                var marksByExam = ids.Any()
                    ? conn.Query<TopperMarkRowWithExam>(
                        @"SELECT student_id StudentId, subject_id SubjectId, exam_type_id ExamTypeId,
                                 marks_obtained MarksObtained, max_marks MaxMarks, is_absent IsAbsent
                          FROM student_marks
                          WHERE school_id        = @schoolId
                            AND academic_year_id = @academicYearId
                            AND student_id IN @ids",
                        new { schoolId, academicYearId, ids })
                      .ToLookup(m => m.ExamTypeId)
                    : Enumerable.Empty<TopperMarkRowWithExam>().ToLookup(m => m.ExamTypeId);

                var subjectNames = subjects.Select(s => s.SubjectName).ToList();

                var exams = examTypes
                    .Where(et => marksByExam[et.ExamTypeId].Any())
                    .Select(et =>
                    {
                        var examMarks = marksByExam[et.ExamTypeId].ToLookup(m => m.StudentId);

                        var computed = students
                            .Where(s => examMarks[s.StudentId].Any())
                            .Select(s =>
                            {
                                var subjMarks = new Dictionary<string, string>();
                                decimal obtained = 0, max = 0;
                                foreach (var sub in subjects)
                                {
                                    var m = examMarks[s.StudentId].FirstOrDefault(x => x.SubjectId == sub.SubjectId);
                                    if (m == null)       { subjMarks[sub.SubjectName] = "—"; }
                                    else if (m.IsAbsent) { subjMarks[sub.SubjectName] = "AB"; max += m.MaxMarks; }
                                    else                 { subjMarks[sub.SubjectName] = $"{m.MarksObtained}/{m.MaxMarks}"; obtained += m.MarksObtained; max += m.MaxMarks; }
                                }
                                return new
                                {
                                    s.AdmissionNo, s.StudentName,
                                    TotalObtained = obtained,
                                    TotalMax      = max,
                                    Percentage    = max > 0 ? Math.Round(obtained / max * 100, 1) : 0m,
                                    SubjectMarks  = subjMarks,
                                };
                            })
                            .OrderByDescending(r => r.Percentage)
                            .ThenByDescending(r => r.TotalObtained)
                            .ToList();

                        int rank = 1;
                        var ranked = computed.Select((r, i) =>
                        {
                            if (i > 0 && r.Percentage != computed[i - 1].Percentage) rank = i + 1;
                            return new ExamTopperRowDto
                            {
                                Rank          = rank,
                                AdmissionNo   = r.AdmissionNo,
                                StudentName   = r.StudentName,
                                TotalObtained = r.TotalObtained,
                                TotalMax      = r.TotalMax,
                                Percentage    = r.Percentage,
                                SubjectMarks  = r.SubjectMarks,
                            };
                        }).ToList();

                        var topRows = topN > 0 ? ranked.Where(r => r.Rank <= topN).ToList() : ranked;

                        return new ClassTopperExamDto { ExamName = et.ExamTypeName, Rows = topRows };
                    })
                    .Where(e => e.Rows.Any());

                return new ClassToppersDto
                {
                    ClassName    = meta?.ClassName   ?? "",
                    SectionName  = meta?.SectionName ?? "",
                    AcademicYear = academicYear,
                    Subjects     = subjectNames,
                    Exams        = exams,
                };
            }
        }

        public ExamToppersDto GetExamToppers(
            string tenantDbName, int schoolId, int academicYearId,
            int examTypeId, int? classId, int topN)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var examName = conn.ExecuteScalar<string>(
                    "SELECT exam_type_name FROM exam_types WHERE exam_type_id = @examTypeId AND school_id = @schoolId",
                    new { examTypeId, schoolId }) ?? "";

                var academicYear = conn.ExecuteScalar<string>(
                    "SELECT academic_year FROM academic_years WHERE academic_year_id = @academicYearId",
                    new { academicYearId }) ?? "";

                var subjects = conn.Query<SubjectMeta>(
                    @"SELECT subject_id SubjectId, subject_name SubjectName
                      FROM subjects
                      WHERE school_id = @schoolId AND ISNULL(status, 'Active') = 'Active'
                      ORDER BY subject_name",
                    new { schoolId }).ToList();

                var students = conn.Query<TopperStudentMeta>(
                    @"SELECT s.student_id StudentId, s.admission_no AdmissionNo, s.student_name StudentName,
                             c.class_name ClassName, ISNULL(sec.section_name, '-') SectionName,
                             s.class_id ClassId, ISNULL(s.section_id, 0) SectionId
                      FROM students s
                      LEFT JOIN classes  c   ON c.class_id    = s.class_id
                      LEFT JOIN sections sec ON sec.section_id = s.section_id
                      WHERE s.school_id        = @schoolId
                        AND s.academic_year_id = @academicYearId
                        AND s.status IN ('Active','Y')
                        AND (@classId IS NULL OR s.class_id = @classId)
                      ORDER BY c.class_name, sec.section_name, s.student_name",
                    new { schoolId, academicYearId, classId }).ToList();

                var ids = students.Select(s => s.StudentId).ToList();

                var marksLookup = ids.Any()
                    ? conn.Query<TopperMarkRow>(
                        @"SELECT student_id StudentId, subject_id SubjectId,
                                 marks_obtained MarksObtained, max_marks MaxMarks, is_absent IsAbsent
                          FROM student_marks
                          WHERE school_id        = @schoolId
                            AND exam_type_id     = @examTypeId
                            AND academic_year_id = @academicYearId
                            AND student_id IN @ids",
                        new { schoolId, examTypeId, academicYearId, ids })
                      .ToLookup(m => m.StudentId)
                    : Enumerable.Empty<TopperMarkRow>().ToLookup(m => m.StudentId);

                var subjectNames = subjects.Select(s => s.SubjectName).ToList();

                // Build per-student totals
                var studentRows = students
                    .Where(s => marksLookup[s.StudentId].Any())
                    .Select(s =>
                    {
                        var subjMarks = new Dictionary<string, string>();
                        decimal obtained = 0, max = 0;
                        foreach (var sub in subjects)
                        {
                            var m = marksLookup[s.StudentId].FirstOrDefault(x => x.SubjectId == sub.SubjectId);
                            if (m == null)             { subjMarks[sub.SubjectName] = "—"; }
                            else if (m.IsAbsent)       { subjMarks[sub.SubjectName] = "AB"; max += m.MaxMarks; }
                            else                       { subjMarks[sub.SubjectName] = $"{m.MarksObtained}/{m.MaxMarks}"; obtained += m.MarksObtained; max += m.MaxMarks; }
                        }
                        return new
                        {
                            s.StudentId, s.AdmissionNo, s.StudentName,
                            s.ClassName, s.SectionName, s.ClassId, s.SectionId,
                            TotalObtained = obtained,
                            TotalMax      = max,
                            Percentage    = max > 0 ? Math.Round(obtained / max * 100, 1) : 0m,
                            SubjectMarks  = subjMarks,
                        };
                    }).ToList();

                // Group by class+section, rank, take topN
                var classes = studentRows
                    .GroupBy(r => new { r.ClassId, r.SectionId, r.ClassName, r.SectionName })
                    .OrderBy(g => g.Key.ClassName).ThenBy(g => g.Key.SectionName)
                    .Select(g =>
                    {
                        var sorted = g.OrderByDescending(r => r.Percentage)
                                      .ThenByDescending(r => r.TotalObtained)
                                      .ToList();
                        int displayRank = 1;
                        var ranked = sorted.Select((r, i) =>
                        {
                            if (i > 0 && r.Percentage != sorted[i - 1].Percentage)
                                displayRank = i + 1;
                            return new ExamTopperRowDto
                            {
                                Rank          = displayRank,
                                AdmissionNo   = r.AdmissionNo,
                                StudentName   = r.StudentName,
                                TotalObtained = r.TotalObtained,
                                TotalMax      = r.TotalMax,
                                Percentage    = r.Percentage,
                                SubjectMarks  = r.SubjectMarks,
                            };
                        }).ToList();

                        var topRows = topN > 0
                            ? ranked.Where(r => r.Rank <= topN).ToList()
                            : ranked;

                        return new ExamTopperClassDto
                        {
                            ClassName   = g.Key.ClassName,
                            SectionName = g.Key.SectionName,
                            Rows        = topRows,
                        };
                    })
                    .Where(g => g.Rows.Any());

                return new ExamToppersDto
                {
                    ExamName     = examName,
                    AcademicYear = academicYear,
                    Subjects     = subjectNames,
                    Classes      = classes,
                };
            }
        }

        public AttendanceRegisterDto GetAttendanceRegister(
            string tenantDbName, int schoolId, int academicYearId,
            int classId, int sectionId, DateTime dateFrom, DateTime dateTo)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var meta = conn.QueryFirstOrDefault<ClassMeta>(
                    @"SELECT c.class_name ClassName, sec.section_name SectionName
                      FROM classes c
                      JOIN sections sec ON sec.section_id = @sectionId
                      WHERE c.class_id = @classId",
                    new { classId, sectionId });

                var students = conn.Query<StudentMeta>(
                    @"SELECT student_id StudentId, admission_no AdmissionNo, student_name StudentName
                      FROM students
                      WHERE class_id = @classId AND section_id = @sectionId
                        AND school_id = @schoolId AND academic_year_id = @academicYearId
                        AND status IN ('Active','Y')
                      ORDER BY student_name",
                    new { classId, sectionId, schoolId, academicYearId }).ToList();

                var ids = students.Select(s => s.StudentId).ToList();

                // Fetch all attendance rows in range; only dates that were actually marked
                var records = ids.Any()
                    ? conn.Query<DateRecord>(
                        @"SELECT student_id StudentId,
                                 CONVERT(VARCHAR(10), attendance_date, 120) DateKey,
                                 status Status
                          FROM student_attendance
                          WHERE school_id = @schoolId
                            AND attendance_date BETWEEN @dateFrom AND @dateTo
                            AND student_id IN @ids",
                        new { schoolId, dateFrom, dateTo, ids })
                      .ToLookup(r => r.StudentId)
                    : Enumerable.Empty<DateRecord>().ToLookup(r => r.StudentId);

                // Working days = distinct dates that appear in ANY student's record
                var dates = records
                    .SelectMany(g => g)
                    .Select(r => r.DateKey)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();

                int serial = 1;
                var rows = students.Select(s =>
                {
                    var dateMap = new Dictionary<string, string>();
                    int p = 0, a = 0, l = 0, h = 0;
                    foreach (var rec in records[s.StudentId])
                    {
                        var v = rec.Status == "Present" ? "P"
                              : rec.Status == "Absent"  ? "A"
                              : rec.Status == "Late"    ? "L"
                              : rec.Status == "HalfDay" ? "H"
                              : rec.Status?.Substring(0, 1).ToUpper() ?? "";
                        dateMap[rec.DateKey] = v;
                        if (v == "P") p++;
                        else if (v == "A") a++;
                        else if (v == "L") l++;
                        else if (v == "H") h++;
                    }
                    return new AttendanceRegisterRowDto
                    {
                        SerialNo    = serial++,
                        AdmissionNo = s.AdmissionNo,
                        StudentName = s.StudentName,
                        Present     = p,
                        Absent      = a,
                        Late        = l,
                        HalfDay     = h,
                        DateData    = dateMap,
                    };
                });

                return new AttendanceRegisterDto
                {
                    ClassName   = meta?.ClassName   ?? "",
                    SectionName = meta?.SectionName ?? "",
                    Dates       = dates,
                    Rows        = rows,
                };
            }
        }

        public MonthlyAttendanceSheetDto GetMonthlyAttendanceSheet(
            string tenantDbName, int schoolId, int academicYearId, int classId, int sectionId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var meta = conn.QueryFirstOrDefault<ClassMeta>(
                    @"SELECT c.class_name ClassName, sec.section_name SectionName
                      FROM classes c
                      JOIN sections sec ON sec.section_id = @sectionId
                      WHERE c.class_id = @classId",
                    new { classId, sectionId });

                var academicYear = conn.ExecuteScalar<string>(
                    "SELECT academic_year FROM academic_years WHERE academic_year_id = @academicYearId",
                    new { academicYearId });

                var students = conn.Query<StudentMeta>(
                    @"SELECT student_id StudentId, admission_no AdmissionNo, student_name StudentName
                      FROM students
                      WHERE class_id = @classId AND section_id = @sectionId
                        AND school_id = @schoolId AND academic_year_id = @academicYearId
                        AND status IN ('Active','Y')
                      ORDER BY student_name",
                    new { classId, sectionId, schoolId, academicYearId }).ToList();

                var ids = students.Select(s => s.StudentId).ToList();

                var records = ids.Any()
                    ? conn.Query<MonthRecord>(
                        @"SELECT student_id StudentId,
                                 YEAR(attendance_date)  Yr,
                                 MONTH(attendance_date) Mo,
                                 status Status
                          FROM student_attendance
                          WHERE school_id = @schoolId
                            AND student_id IN @ids",
                        new { schoolId, ids })
                      .ToLookup(r => r.StudentId)
                    : Enumerable.Empty<MonthRecord>().ToLookup(r => r.StudentId);

                // Collect ordered month keys
                var allMonths = records
                    .SelectMany(g => g)
                    .Select(r => new { r.Yr, r.Mo })
                    .Distinct()
                    .OrderBy(m => m.Yr).ThenBy(m => m.Mo)
                    .Select(m => $"{m.Yr:D4}-{m.Mo:D2}")
                    .ToList();

                var rows = students.Select(s =>
                {
                    var monthData = new Dictionary<string, MonthCounts>();
                    int tp = 0, ta = 0, tl = 0, th = 0;

                    foreach (var rec in records[s.StudentId])
                    {
                        var key = $"{rec.Yr:D4}-{rec.Mo:D2}";
                        if (!monthData.ContainsKey(key))
                            monthData[key] = new MonthCounts();
                        if      (rec.Status == "Present") { monthData[key].Present++; tp++; }
                        else if (rec.Status == "Absent")  { monthData[key].Absent++;  ta++; }
                        else if (rec.Status == "Late")    { monthData[key].Late++;    tl++; }
                        else if (rec.Status == "HalfDay") { monthData[key].HalfDay++; th++; }
                    }

                    return new MonthlyAttendanceRowDto
                    {
                        AdmissionNo  = s.AdmissionNo,
                        StudentName  = s.StudentName,
                        TotalPresent = tp,
                        TotalAbsent  = ta,
                        TotalLate    = tl,
                        TotalHalfDay = th,
                        MonthData    = monthData,
                    };
                });

                return new MonthlyAttendanceSheetDto
                {
                    ClassName    = meta?.ClassName   ?? "",
                    SectionName  = meta?.SectionName ?? "",
                    AcademicYear = academicYear      ?? "",
                    Months       = allMonths,
                    Rows         = rows,
                };
            }
        }

        public IEnumerable<DailyAttendanceSummaryRowDto> GetDailyAttendanceSummary(
            string tenantDbName, int schoolId,
            DateTime dateFrom, DateTime dateTo,
            int? classId, int? sectionId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<DailyAttendanceSummaryRowDto>(
                    @"SELECT
                        CONVERT(VARCHAR(10), sa.attendance_date, 105)  AttendanceDate,
                        c.class_name                                   ClassName,
                        ISNULL(sec.section_name, '-')                  SectionName,
                        COUNT(DISTINCT s.student_id)                   TotalStudents,
                        SUM(CASE WHEN sa.status = 'Present' THEN 1 ELSE 0 END) Present,
                        SUM(CASE WHEN sa.status = 'Absent'  THEN 1 ELSE 0 END) Absent,
                        SUM(CASE WHEN sa.status = 'Late'    THEN 1 ELSE 0 END) Late,
                        SUM(CASE WHEN sa.status = 'HalfDay' THEN 1 ELSE 0 END) HalfDay
                      FROM student_attendance sa
                      JOIN students  s   ON s.student_id   = sa.student_id
                      LEFT JOIN classes   c   ON c.class_id    = s.class_id
                      LEFT JOIN sections  sec ON sec.section_id = s.section_id
                      WHERE sa.school_id          = @schoolId
                        AND sa.attendance_date BETWEEN @dateFrom AND @dateTo
                        AND (@classId   IS NULL OR s.class_id   = @classId)
                        AND (@sectionId IS NULL OR s.section_id = @sectionId)
                        AND s.academic_year_id = (
                            SELECT TOP 1 academic_year_id FROM academic_years
                            WHERE school_id = @schoolId AND status = 'Active'
                            ORDER BY academic_year_id DESC)
                      GROUP BY sa.attendance_date, s.class_id, c.class_name,
                               s.section_id, sec.section_name
                      ORDER BY sa.attendance_date, c.class_name, sec.section_name",
                    new { schoolId, dateFrom, dateTo, classId, sectionId });
        }

        public IEnumerable<HomeworkStatementRowDto> GetHomeworkStatement(
            string tenantDbName, int schoolId,
            DateTime dateFrom, DateTime dateTo, int? classId, int? subjectId = null)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<HomeworkStatementRowDto>(
                    @"SELECT
                        CONVERT(VARCHAR(10), h.due_date,      105) DueDate,
                        CONVERT(VARCHAR(10), h.assigned_date, 105) AssignedDate,
                        ISNULL(c.class_name, '-')    ClassName,
                        ISNULL(sub.subject_name, '-') SubjectName,
                        h.title        Title,
                        ISNULL(h.description, '') Description,
                        h.created_by   AssignedBy
                      FROM homework h
                      LEFT JOIN classes  c   ON c.class_id    = h.class_id
                      LEFT JOIN subjects sub ON sub.subject_id = h.subject_id
                      WHERE h.school_id = @schoolId
                        AND h.status   != 'Cancelled'
                        AND h.due_date BETWEEN @dateFrom AND @dateTo
                        AND (@classId   IS NULL OR h.class_id   = @classId)
                        AND (@subjectId IS NULL OR h.subject_id = @subjectId)
                      ORDER BY sub.subject_name, h.due_date, c.class_name",
                    new { schoolId, dateFrom, dateTo, classId, subjectId });
        }

        public AcademicYearToppersDto GetAcademicYearToppers(
            string tenantDbName, int schoolId, int academicYearId,
            int? classId, int? sectionId, int topN)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var academicYear = conn.ExecuteScalar<string>(
                    "SELECT academic_year FROM academic_years WHERE academic_year_id = @academicYearId",
                    new { academicYearId }) ?? "";

                var examTypes = conn.Query<ExamTypeMeta>(
                    @"SELECT exam_type_id ExamTypeId, exam_type_name ExamTypeName
                      FROM exam_types
                      WHERE school_id = @schoolId AND academic_year_id = @academicYearId
                        AND status = 'Active'
                      ORDER BY ISNULL(display_order, 999), exam_type_name",
                    new { schoolId, academicYearId }).ToList();

                var students = conn.Query<TopperStudentMeta>(
                    @"SELECT s.student_id StudentId, s.admission_no AdmissionNo, s.student_name StudentName,
                             c.class_name ClassName, ISNULL(sec.section_name, '-') SectionName,
                             s.class_id ClassId, ISNULL(s.section_id, 0) SectionId
                      FROM students s
                      LEFT JOIN classes  c   ON c.class_id    = s.class_id
                      LEFT JOIN sections sec ON sec.section_id = s.section_id
                      WHERE s.school_id        = @schoolId
                        AND s.academic_year_id = @academicYearId
                        AND s.status IN ('Active','Y')
                        AND (@classId   IS NULL OR s.class_id   = @classId)
                        AND (@sectionId IS NULL OR s.section_id = @sectionId)
                      ORDER BY c.class_name, sec.section_name, s.student_name",
                    new { schoolId, academicYearId, classId, sectionId }).ToList();

                var ids = students.Select(s => s.StudentId).ToList();

                // Fetch all marks across all exam types in one query
                var allMarksByStudent = ids.Any()
                    ? conn.Query<TopperMarkRowWithExam>(
                        @"SELECT student_id StudentId, subject_id SubjectId, exam_type_id ExamTypeId,
                                 marks_obtained MarksObtained, max_marks MaxMarks, is_absent IsAbsent
                          FROM student_marks
                          WHERE school_id        = @schoolId
                            AND academic_year_id = @academicYearId
                            AND student_id IN @ids",
                        new { schoolId, academicYearId, ids })
                      .ToLookup(m => m.StudentId)
                    : Enumerable.Empty<TopperMarkRowWithExam>().ToLookup(m => m.StudentId);

                var examNames = examTypes.Select(e => e.ExamTypeName).ToList();

                // Build per-student totals
                var studentRows = students
                    .Where(s => allMarksByStudent[s.StudentId].Any())
                    .Select(s =>
                    {
                        var examTotals  = new Dictionary<string, string>();
                        decimal grandObt = 0, grandMax = 0;

                        foreach (var et in examTypes)
                        {
                            var examMarks = allMarksByStudent[s.StudentId]
                                .Where(m => m.ExamTypeId == et.ExamTypeId).ToList();

                            if (!examMarks.Any())
                            {
                                examTotals[et.ExamTypeName] = "—";
                                continue;
                            }

                            decimal eObt = 0, eMax = 0;
                            foreach (var m in examMarks)
                            {
                                eMax += m.MaxMarks;
                                if (!m.IsAbsent) eObt += m.MarksObtained;
                            }
                            examTotals[et.ExamTypeName] = $"{eObt}/{eMax}";
                            grandObt += eObt;
                            grandMax += eMax;
                        }

                        return new
                        {
                            s.StudentId, s.AdmissionNo, s.StudentName,
                            s.ClassName, s.SectionName, s.ClassId, s.SectionId,
                            TotalObtained = grandObt,
                            TotalMax      = grandMax,
                            Percentage    = grandMax > 0 ? Math.Round(grandObt / grandMax * 100, 1) : 0m,
                            ExamTotals    = examTotals,
                        };
                    }).ToList();

                var classes = studentRows
                    .GroupBy(r => new { r.ClassId, r.SectionId, r.ClassName, r.SectionName })
                    .OrderBy(g => g.Key.ClassName).ThenBy(g => g.Key.SectionName)
                    .Select(g =>
                    {
                        var sorted = g.OrderByDescending(r => r.Percentage)
                                      .ThenByDescending(r => r.TotalObtained)
                                      .ToList();
                        int displayRank = 1;
                        var ranked = sorted.Select((r, i) =>
                        {
                            if (i > 0 && r.Percentage != sorted[i - 1].Percentage)
                                displayRank = i + 1;
                            return new AcademicYearTopperRowDto
                            {
                                Rank          = displayRank,
                                AdmissionNo   = r.AdmissionNo,
                                StudentName   = r.StudentName,
                                TotalObtained = r.TotalObtained,
                                TotalMax      = r.TotalMax,
                                Percentage    = r.Percentage,
                                ExamTotals    = r.ExamTotals,
                            };
                        }).ToList();

                        var topRows = topN > 0
                            ? ranked.Where(r => r.Rank <= topN).ToList()
                            : ranked;

                        return new AcademicYearTopperClassDto
                        {
                            ClassName   = g.Key.ClassName,
                            SectionName = g.Key.SectionName,
                            Rows        = topRows,
                        };
                    })
                    .Where(g => g.Rows.Any());

                return new AcademicYearToppersDto
                {
                    AcademicYear = academicYear,
                    ExamNames    = examNames,
                    Classes      = classes,
                };
            }
        }

        private class SubjectMeta            { public int SubjectId { get; set; } public string SubjectName { get; set; } }
        private class ExamTypeMeta           { public int ExamTypeId { get; set; } public string ExamTypeName { get; set; } }
        // ── Detained Students ─────────────────────────────────────────────

        public IEnumerable<DetainedStudentRowDto> GetDetainedStudents(
            string tenantDbName, int schoolId, int? academicYearId, int? classId, int? sectionId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<DetainedStudentRowDto>(
                    @"SELECT s.student_id                       StudentId,
                             s.admission_no                     AdmissionNo,
                             s.student_name                     StudentName,
                             ISNULL(c.class_name,    '')        ClassName,
                             ISNULL(sec.section_name,'')        SectionName,
                             ISNULL(ay.academic_year,'')        AcademicYear,
                             ISNULL(s.father_name,   '')        FatherName,
                             ISNULL(s.father_mobile, '')        FatherMobile,
                             ISNULL(s.detained_reason,'')       DetainedReason
                      FROM   students s
                      LEFT JOIN classes       c   ON c.class_id         = s.class_id
                      LEFT JOIN sections      sec ON sec.section_id      = s.section_id
                      LEFT JOIN academic_years ay  ON ay.academic_year_id = s.academic_year_id
                      WHERE  s.school_id           = @schoolId
                        AND  ISNULL(s.is_detained, 0) = 1
                        AND  (@academicYearId IS NULL OR s.academic_year_id = @academicYearId)
                        AND  (@classId        IS NULL OR s.class_id         = @classId)
                        AND  (@sectionId      IS NULL OR s.section_id       = @sectionId)
                      ORDER BY ay.academic_year DESC, c.class_name, sec.section_name, s.student_name",
                    new { schoolId, academicYearId, classId, sectionId });
        }

        // ── Regular Absentees ─────────────────────────────────────────────

        public IEnumerable<RegularAbsenteeRowDto> GetRegularAbsentees(
            string tenantDbName, int schoolId,
            DateTime dateFrom, DateTime dateTo,
            int minDays, int? classId, int? sectionId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<RegularAbsenteeRowDto>(
                    @"SELECT s.student_id                                       StudentId,
                             s.admission_no                                     AdmissionNo,
                             s.student_name                                     StudentName,
                             ISNULL(c.class_name,    '')                        ClassName,
                             ISNULL(sec.section_name,'')                        SectionName,
                             ISNULL(s.father_name,   '')                        FatherName,
                             ISNULL(s.father_mobile, '')                        FatherMobile,
                             COUNT(*)                                           AbsentDays,
                             CONVERT(VARCHAR(10), MAX(sa.attendance_date), 105) LastAbsentDate,
                             s.status                                           Status
                      FROM   students s
                      JOIN   student_attendance sa
                             ON  sa.student_id       = s.student_id
                             AND sa.school_id        = s.school_id
                             AND sa.status           = 'Absent'
                             AND sa.attendance_date BETWEEN @dateFrom AND @dateTo
                      LEFT JOIN classes  c   ON c.class_id    = s.class_id
                      LEFT JOIN sections sec ON sec.section_id = s.section_id
                      WHERE  s.school_id = @schoolId
                        AND  (@classId   IS NULL OR s.class_id   = @classId)
                        AND  (@sectionId IS NULL OR s.section_id = @sectionId)
                      GROUP BY s.student_id, s.admission_no, s.student_name,
                               c.class_name, sec.section_name,
                               s.father_name, s.father_mobile, s.status,
                               s.class_id, sec.section_id
                      HAVING COUNT(*) >= @minDays
                      ORDER BY COUNT(*) DESC, c.class_name, sec.section_name, s.student_name",
                    new { schoolId, dateFrom, dateTo, minDays, classId, sectionId });
        }

        private class TopperMarkRowWithExam  { public long StudentId { get; set; } public int SubjectId { get; set; } public int ExamTypeId { get; set; } public decimal MarksObtained { get; set; } public decimal MaxMarks { get; set; } public bool IsAbsent { get; set; } }
        private class TopperStudentMeta  { public long StudentId { get; set; } public string AdmissionNo { get; set; } public string StudentName { get; set; } public string ClassName { get; set; } public string SectionName { get; set; } public int ClassId { get; set; } public int SectionId { get; set; } }
        private class TopperMarkRow      { public long StudentId { get; set; } public int SubjectId { get; set; } public decimal MarksObtained { get; set; } public decimal MaxMarks { get; set; } public bool IsAbsent { get; set; } }
        private class ClassMeta   { public string ClassName { get; set; } public string SectionName { get; set; } }
        private class StudentMeta { public long StudentId { get; set; } public string AdmissionNo { get; set; } public string StudentName { get; set; } }
        private class DayRecord   { public long StudentId { get; set; } public int DayNo { get; set; } public string Status { get; set; } }
        private class MonthRecord  { public long StudentId { get; set; } public int Yr { get; set; } public int Mo { get; set; } public string Status { get; set; } }
        private class DateRecord   { public long StudentId { get; set; } public string DateKey { get; set; } public string Status { get; set; } }
    }
}
