using AscentSchools.Core.DTOs.Mobile.Data;
using AscentSchools.Core.DTOs.School.Events;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AscentSchools.Data.Repositories.Mobile
{
    public class MobileDataRepository
    {
        private readonly IConnectionFactory _db;
        public MobileDataRepository(IConnectionFactory db) { _db = db; }

        // ── Student profile ───────────────────────────────────────────────

        public StudentProfileDto GetStudentProfile(string tenantDbName, long studentId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QueryFirstOrDefault<StudentProfileDto>(
                    @"SELECT
                        s.student_id     StudentId,
                        s.admission_no   AdmissionNo,
                        s.student_name   FullName,
                        c.class_name     ClassName,
                        s.section        Section,
                        CONVERT(VARCHAR(10), s.date_of_birth, 120) DateOfBirth,
                        s.gender         Gender,
                        s.blood_group    BloodGroup,
                        s.father_name    FatherName,
                        s.mother_name    MotherName,
                        ISNULL(s.father_mobile, s.mother_mobile) Mobile,
                        s.email          Email,
                        ISNULL(s.permanent_address,
                            LTRIM(ISNULL(s.door_no+', ','') + ISNULL(s.address_area+', ','')
                                + ISNULL(s.address_city+', ','') + ISNULL(s.address_state,'')))
                                         Address,
                        s.photo_path     PhotoPath,
                        ay.academic_year AcademicYear
                      FROM students s
                      LEFT JOIN classes        c  ON c.class_id         = s.class_id
                      LEFT JOIN academic_years ay ON ay.academic_year_id = s.academic_year_id
                      WHERE s.student_id = @studentId",
                    new { studentId });
        }

        // ── Attendance ────────────────────────────────────────────────────

        public AttendanceSummaryDto GetAttendance(string tenantDbName, long studentId, int month, int year)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var records = conn.Query<AttendanceRecordDto>(
                    @"SELECT attendance_date Date, status Status, remarks Remarks
                      FROM student_attendance
                      WHERE student_id = @studentId
                        AND MONTH(attendance_date) = @month
                        AND YEAR(attendance_date)  = @year
                      ORDER BY attendance_date",
                    new { studentId, month, year }).ToList();

                return new AttendanceSummaryDto
                {
                    TotalDays   = records.Count,
                    PresentDays = records.Count(r => r.Status == "Present"),
                    AbsentDays  = records.Count(r => r.Status == "Absent"),
                    LateDays    = records.Count(r => r.Status == "Late"),
                    Records     = records
                };
            }
        }

        // ── Marks ─────────────────────────────────────────────────────────

        public IEnumerable<MarksResultDto> GetMarks(string tenantDbName, long studentId, int academicYearId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var rows = conn.Query<MarksRow>(
                    @"SELECT
                        ay.academic_year_id AcademicYearId,
                        ay.academic_year    AcademicYearName,
                        et.exam_type_id     ExamTypeId,
                        et.exam_type_name   ExamTypeName,
                        et.display_order    DisplayOrder,
                        sub.subject_name    SubjectName,
                        sm.marks_obtained   MarksObtained,
                        sm.max_marks        MaxMarks,
                        sm.is_absent        IsAbsent
                      FROM student_marks sm
                      JOIN exam_types     et  ON et.exam_type_id     = sm.exam_type_id
                      JOIN subjects       sub ON sub.subject_id       = sm.subject_id
                      JOIN academic_years ay  ON ay.academic_year_id  = sm.academic_year_id
                      WHERE sm.student_id = @studentId
                        AND sm.academic_year_id = @academicYearId
                      ORDER BY et.display_order, sub.subject_name",
                    new { studentId, academicYearId }).ToList();

                return rows
                    .GroupBy(r => new { r.AcademicYearId, r.AcademicYearName })
                    .Select(yg => new MarksResultDto
                    {
                        AcademicYearId   = yg.Key.AcademicYearId,
                        AcademicYearName = yg.Key.AcademicYearName,
                        Exams = yg.GroupBy(r => new { r.ExamTypeId, r.ExamTypeName, r.DisplayOrder })
                                  .OrderBy(eg => eg.Key.DisplayOrder)
                                  .Select(eg =>
                                  {
                                      var markList = eg.Select(m => new SubjectMarkDto
                                      {
                                          SubjectName   = m.SubjectName,
                                          MarksObtained = m.MarksObtained,
                                          MaxMarks      = m.MaxMarks,
                                          IsAbsent      = m.IsAbsent
                                      }).ToList();

                                      var obtained = markList.Where(m => !m.IsAbsent).Sum(m => m.MarksObtained);
                                      var max      = markList.Sum(m => m.MaxMarks);

                                      return new ExamResultDto
                                      {
                                          ExamTypeId    = eg.Key.ExamTypeId,
                                          ExamTypeName  = eg.Key.ExamTypeName,
                                          Marks         = markList,
                                          TotalObtained = obtained,
                                          TotalMax      = max,
                                          Percentage    = max > 0 ? Math.Round(obtained / max * 100, 2) : 0
                                      };
                                  }).ToList()
                    }).ToList();
            }
        }

        // ── Homework ──────────────────────────────────────────────────────

        public IEnumerable<HomeworkDto> GetHomework(string tenantDbName, int classId, int? sectionId, int schoolId, int count = 20)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var homeworks = conn.Query<HomeworkRow>(
                    @"SELECT TOP (@count)
                        h.homework_id   HomeworkId,
                        h.title         Title,
                        h.description   Description,
                        sub.subject_name SubjectName,
                        h.assigned_date AssignedDate,
                        h.due_date      DueDate,
                        h.attachment_url AttachmentUrl
                      FROM homework h
                      LEFT JOIN subjects sub ON sub.subject_id = h.subject_id
                      WHERE h.class_id  = @classId
                        AND h.school_id = @schoolId
                        AND h.status    = 'Active'
                        AND (h.section_id IS NULL OR h.section_id = @sectionId)
                      ORDER BY h.due_date DESC",
                    new { classId, sectionId, schoolId, count }).ToList();

                if (!homeworks.Any()) return new List<HomeworkDto>();

                var ids = homeworks.Select(h => h.HomeworkId).ToArray();
                var attachments = conn.Query<AttachmentRow>(
                    @"SELECT attachment_id AttachmentId, homework_id HomeworkId,
                             file_name FileName, file_path FilePath, file_size_kb FileSizeKb
                      FROM homework_attachments
                      WHERE homework_id IN @ids",
                    new { ids }).ToLookup(a => a.HomeworkId);

                return homeworks.Select(h => new HomeworkDto
                {
                    HomeworkId    = h.HomeworkId,
                    Title         = h.Title,
                    Description   = h.Description,
                    SubjectName   = h.SubjectName,
                    AssignedDate  = h.AssignedDate,
                    DueDate       = h.DueDate,
                    AttachmentUrl = h.AttachmentUrl,
                    Attachments   = attachments[h.HomeworkId].Select(a => new AttachmentDto
                    {
                        AttachmentId = a.AttachmentId,
                        FileName     = a.FileName,
                        FilePath     = a.FilePath,
                        FileSizeKb   = a.FileSizeKb
                    }).ToList()
                }).ToList();
            }
        }

        // ── Events ───────────────────────────────────────────────────────

        public IEnumerable<SchoolEventDto> GetEvents(string tenantDbName, int schoolId, int? classId, int count = 50)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<SchoolEventDto>(
                    @"SELECT TOP (@count)
                        event_id      EventId,
                        title         Title,
                        description   Description,
                        CONVERT(VARCHAR(10), event_date, 120) EventDate,
                        media_type    MediaType,
                        media_url      MediaUrl,
                        thumbnail_url  ThumbnailUrl,
                        attachment_url AttachmentUrl,
                        scope          Scope,
                        is_pinned     IsPinned
                      FROM school_events
                      WHERE school_id = @schoolId
                        AND status    = 'Active'
                        AND (scope = 'School' OR (scope = 'Class' AND class_id = @classId))
                      ORDER BY is_pinned DESC, event_date DESC",
                    new { schoolId, classId, count });
        }

        // ── Announcements ─────────────────────────────────────────────────

        public IEnumerable<AnnouncementDto> GetAnnouncements(string tenantDbName, int schoolId, int? classId, int count = 30)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<AnnouncementDto>(
                    @"SELECT TOP (@count)
                        announcement_id AnnouncementId,
                        title           Title,
                        description     Description,
                        scope           Scope,
                        is_pinned       IsPinned,
                        attachment_url  AttachmentUrl,
                        created_at      CreatedAt
                      FROM announcements
                      WHERE school_id = @schoolId
                        AND status    = 'Active'
                        AND (scope = 'School' OR (scope = 'Class' AND class_id = @classId))
                      ORDER BY is_pinned DESC, created_at DESC",
                    new { schoolId, classId, count });
        }

        // ── Internal row types ────────────────────────────────────────────

        private class MarksRow
        {
            public int     AcademicYearId   { get; set; }
            public string  AcademicYearName { get; set; }
            public int     ExamTypeId       { get; set; }
            public string  ExamTypeName     { get; set; }
            public int?    DisplayOrder     { get; set; }
            public string  SubjectName      { get; set; }
            public decimal MarksObtained    { get; set; }
            public decimal MaxMarks         { get; set; }
            public bool    IsAbsent         { get; set; }
        }

        private class HomeworkRow
        {
            public int      HomeworkId    { get; set; }
            public string   Title         { get; set; }
            public string   Description   { get; set; }
            public string   SubjectName   { get; set; }
            public DateTime AssignedDate  { get; set; }
            public DateTime DueDate       { get; set; }
            public string   AttachmentUrl { get; set; }
        }

        private class AttachmentRow
        {
            public int    AttachmentId { get; set; }
            public int    HomeworkId   { get; set; }
            public string FileName     { get; set; }
            public string FilePath     { get; set; }
            public int?   FileSizeKb   { get; set; }
        }
    }
}
