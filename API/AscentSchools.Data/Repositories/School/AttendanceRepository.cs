using AscentSchools.Core.DTOs.School.Attendance;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AscentSchools.Data.Repositories.School
{
    public class AttendanceRepository
    {
        private readonly IConnectionFactory _db;
        public AttendanceRepository(IConnectionFactory db) { _db = db; }

        // ── Get attendance grid for a class on a date ─────────────────────────
        // Returns all active students in the class with their status for that date (null if not yet marked).

        // sectionId is optional: null → whole class across all sections (used by the
        // class-level Delete Attendance view). When provided, scopes to that section.
        public AttendanceGridDto GetAttendanceGrid(string tenantDbName, int schoolId, int classId, int? sectionId, string date)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var className = sectionId.HasValue
                    ? conn.QueryFirstOrDefault<string>(
                        @"SELECT c.class_name + ' - ' + sec.section_name
                          FROM classes c
                          JOIN sections sec ON sec.section_id = @sectionId
                          WHERE c.class_id = @classId AND c.school_id = @schoolId",
                        new { classId, sectionId, schoolId }) ?? ""
                    : conn.QueryFirstOrDefault<string>(
                        @"SELECT c.class_name
                          FROM classes c
                          WHERE c.class_id = @classId AND c.school_id = @schoolId",
                        new { classId, schoolId }) ?? "";

                // Scope to the current academic year. Without this, the multi-year student
                // model (one Active row per student per year, classes/sections shared across
                // years) returns prior-year/promoted students too → wrong roster.
                var students = conn.Query<StudentRow>(
                    @"SELECT s.student_id StudentId, s.student_name StudentName, s.admission_no AdmissionNo,
                             ISNULL(sec.section_name, '-') SectionName
                      FROM students s
                      LEFT JOIN sections sec ON sec.section_id = s.section_id
                      WHERE s.class_id = @classId AND (@sectionId IS NULL OR s.section_id = @sectionId)
                        AND s.school_id = @schoolId AND s.status = 'Active'
                        AND s.academic_year_id = (
                            SELECT TOP 1 academic_year_id FROM academic_years
                            WHERE school_id = @schoolId AND status = 'Active'
                            ORDER BY academic_year_id DESC)
                      ORDER BY sec.section_name, s.student_name",
                    new { classId, sectionId, schoolId }).ToList();

                var existing = conn.Query<AttendanceRow>(
                    @"SELECT student_id StudentId, status Status, remarks Remarks
                      FROM student_attendance
                      WHERE school_id = @schoolId AND attendance_date = @date
                        AND student_id IN (
                            SELECT student_id FROM students
                            WHERE class_id = @classId AND (@sectionId IS NULL OR section_id = @sectionId)
                              AND school_id = @schoolId
                              AND academic_year_id = (
                                  SELECT TOP 1 academic_year_id FROM academic_years
                                  WHERE school_id = @schoolId AND status = 'Active'
                                  ORDER BY academic_year_id DESC)
                        )",
                    new { schoolId, date, classId, sectionId })
                    .ToDictionary(a => a.StudentId);

                var rows = students.Select(s =>
                {
                    existing.TryGetValue(s.StudentId, out var att);
                    return new AttendanceStudentDto
                    {
                        StudentId   = s.StudentId,
                        StudentName = s.StudentName,
                        AdmissionNo = s.AdmissionNo,
                        SectionName = s.SectionName,
                        Status      = att?.Status,
                        Remarks     = att?.Remarks,
                    };
                });

                return new AttendanceGridDto
                {
                    ClassId   = classId,
                    ClassName = className,
                    Date      = date,
                    IsMarked  = existing.Count > 0,
                    Students  = rows,
                };
            }
        }

        // ── Save attendance (upsert per entry) ────────────────────────────────

        public void SaveAttendance(string tenantDbName, int schoolId, string markedBy, SaveAttendanceRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                foreach (var entry in req.Entries)
                {
                    conn.Execute(
                        @"MERGE student_attendance AS target
                          USING (VALUES (@studentId, @date, @schoolId))
                                AS source (student_id, attendance_date, school_id)
                          ON  target.student_id       = source.student_id
                          AND target.attendance_date  = source.attendance_date
                          AND target.school_id        = source.school_id
                          WHEN MATCHED THEN
                              UPDATE SET status = @status, remarks = @remarks,
                                         marked_by = @markedBy, marked_at = GETDATE()
                          WHEN NOT MATCHED THEN
                              INSERT (student_id, attendance_date, status, remarks, school_id, marked_by)
                              VALUES (@studentId, @date, @status, @remarks, @schoolId, @markedBy);",
                        new
                        {
                            entry.StudentId,
                            date      = req.Date,
                            schoolId,
                            status    = entry.Status ?? "Present",
                            remarks   = entry.Remarks,
                            markedBy,
                        });
                }
            }
        }

        // ── Delete attendance for a class+section on a date ───────────────────
        // Hard-deletes the rows for current-academic-year students of that class+section
        // (same scoping the mark grid uses, so prior-year stray rows aren't touched).
        // Returns the number of rows deleted.

        // sectionId is optional: null → delete for the whole class (every section).
        public int DeleteAttendance(string tenantDbName, int schoolId, int classId, int? sectionId, string date)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                return conn.Execute(
                    @"DELETE FROM student_attendance
                      WHERE school_id = @schoolId AND attendance_date = @date
                        AND student_id IN (
                            SELECT student_id FROM students
                            WHERE class_id = @classId AND (@sectionId IS NULL OR section_id = @sectionId)
                              AND school_id = @schoolId
                              AND academic_year_id = (
                                  SELECT TOP 1 academic_year_id FROM academic_years
                                  WHERE school_id = @schoolId AND status = 'Active'
                                  ORDER BY academic_year_id DESC)
                        )",
                    new { schoolId, date, classId, sectionId });
            }
        }

        // ── Monthly summary (for reports / dashboard) ─────────────────────────

        public IEnumerable<AttendanceMonthlySummaryDto> GetMonthlySummary(
            string tenantDbName, int schoolId, int classId, int sectionId, int month, int year)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<AttendanceMonthlySummaryDto>(
                    @"SELECT s.student_name StudentName, s.admission_no AdmissionNo,
                             SUM(CASE WHEN a.status = 'Present'  THEN 1 ELSE 0 END) PresentDays,
                             SUM(CASE WHEN a.status = 'Absent'   THEN 1 ELSE 0 END) AbsentDays,
                             SUM(CASE WHEN a.status = 'Late'     THEN 1 ELSE 0 END) LateDays,
                             SUM(CASE WHEN a.status = 'HalfDay'  THEN 1 ELSE 0 END) HalfDayDays,
                             COUNT(a.attendance_id) TotalMarked
                      FROM students s
                      LEFT JOIN student_attendance a
                             ON a.student_id = s.student_id
                            AND a.school_id  = s.school_id
                            AND MONTH(a.attendance_date) = @month
                            AND YEAR(a.attendance_date)  = @year
                      WHERE s.class_id   = @classId
                        AND s.section_id = @sectionId
                        AND s.school_id  = @schoolId
                        AND s.status     = 'Active'
                        AND s.academic_year_id = (
                            SELECT TOP 1 academic_year_id FROM academic_years
                            WHERE school_id = @schoolId AND status = 'Active'
                            ORDER BY academic_year_id DESC)
                      GROUP BY s.student_id, s.student_name, s.admission_no
                      ORDER BY s.student_name",
                    new { schoolId, classId, sectionId, month, year });
        }

        // ── Internal row types ────────────────────────────────────────────────

        private class StudentRow
        {
            public long   StudentId   { get; set; }
            public string StudentName { get; set; }
            public string AdmissionNo { get; set; }
            public string SectionName { get; set; }
        }

        private class AttendanceRow
        {
            public long   StudentId { get; set; }
            public string Status    { get; set; }
            public string Remarks   { get; set; }
        }
    }
}
