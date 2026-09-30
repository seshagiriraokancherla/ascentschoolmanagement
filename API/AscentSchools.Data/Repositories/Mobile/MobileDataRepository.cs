using AscentSchools.Core.DTOs.Mobile.Data;
using AscentSchools.Core.DTOs.School.Events;
using AscentSchools.Core.DTOs.School.Media;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
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
                        sec.section_name SectionName,
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
                      LEFT JOIN classes        c   ON c.class_id          = s.class_id
                      LEFT JOIN sections       sec ON sec.section_id       = s.section_id
                      LEFT JOIN academic_years ay  ON ay.academic_year_id  = s.academic_year_id
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
                    HalfDayDays = records.Count(r => r.Status == "HalfDay"),
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

        // ── Exam timetable ───────────────────────────────────────────────
        // Read-only per-subject schedule for a class, sourced from exam_master.exam_date
        // (the same field Master Data → Exam Master already captures — no new table).
        // Only rows with a date set are returned; a subject staff haven't scheduled yet
        // simply doesn't appear (no placeholder "TBA" rows).

        public IEnumerable<ExamTimetableGroupDto> GetExamTimetable(
            string tenantDbName, int schoolId, int classId, int academicYearId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var rows = conn.Query<ExamTimetableRow>(
                    @"SELECT
                        et.exam_type_id   ExamTypeId,
                        et.exam_type_name ExamTypeName,
                        et.display_order  DisplayOrder,
                        sub.subject_name  SubjectName,
                        CONVERT(VARCHAR(10), em.exam_date, 120) ExamDate,
                        em.exam_time      ExamTime,
                        em.exam_remarks   ExamRemarks
                      FROM exam_master em
                      JOIN exam_types et  ON et.exam_type_id = em.exam_type_id
                      JOIN subjects   sub ON sub.subject_id  = em.subject_id
                      WHERE em.class_id         = @classId
                        AND em.academic_year_id = @academicYearId
                        AND em.school_id         = @schoolId
                        AND em.exam_status       = 'Active'
                        AND em.exam_date IS NOT NULL
                      ORDER BY em.exam_date, sub.subject_name",
                    new { classId, academicYearId, schoolId }).ToList();

                // Group by exam type, ordering groups by their earliest exam date (a real
                // timetable's natural order) rather than exam_types.display_order, which
                // doesn't necessarily track chronology when several exam types are active
                // at once (e.g. a make-up exam type alongside the main term exams).
                return rows
                    .GroupBy(r => new { r.ExamTypeId, r.ExamTypeName })
                    .OrderBy(g => g.Min(r => r.ExamDate))
                    .Select(g => new ExamTimetableGroupDto
                    {
                        ExamTypeId   = g.Key.ExamTypeId,
                        ExamTypeName = g.Key.ExamTypeName,
                        Subjects     = g.Select(r => new ExamTimetableSubjectDto
                        {
                            SubjectName = r.SubjectName,
                            ExamDate    = r.ExamDate,
                            ExamTime    = r.ExamTime,
                            ExamRemarks = r.ExamRemarks
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
                      ORDER BY h.assigned_date DESC, h.homework_id DESC",
                    new { classId, sectionId, schoolId, count }).ToList();

                if (!homeworks.Any()) return new List<HomeworkDto>();

                var ids = homeworks.Select(h => h.HomeworkId).ToArray();
                var attachments = conn.Query<AttachmentRow>(
                    @"SELECT attachment_id AttachmentId, homework_id HomeworkId,
                             file_name FileName, file_path FilePath, file_size_kb FileSizeKb
                      FROM homework_attachments
                      WHERE homework_id IN @ids",
                    new { ids }).ToLookup(a => a.HomeworkId);
                var media = LoadMedia(conn, "homework", ids.Select(i => (long)i));

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
                    }).ToList(),
                    Media         = media[h.HomeworkId].ToList()
                }).ToList();
            }
        }

        // ── Events ───────────────────────────────────────────────────────

        public IEnumerable<SchoolEventDto> GetEvents(string tenantDbName, int schoolId, int? classId, int count = 50)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var events = conn.Query<SchoolEventDto>(
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
                    new { schoolId, classId, count }).ToList();

                var media = LoadMedia(conn, "event", events.Select(e => (long)e.EventId));
                foreach (var e in events) e.Media = media[e.EventId].ToList();
                return events;
            }
        }

        // ── Birthdays ──────────────────────────────────────────────────────
        // month/day/includeFeb29 are computed by the caller from TimeHelper.IstToday()
        // (this project doesn't reference AscentSchools.API, so IST "today" can't be
        // computed here — must be passed in). includeFeb29 covers the once-every-4-years
        // gap: on Feb 28 of a non-leap year, a student born Feb 29 is included too.
        public IEnumerable<BirthdayStudentDto> GetBirthdaysToday(
            string tenantDbName, int schoolId, int? classId, int month, int day, bool includeFeb29)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                return conn.Query<BirthdayStudentDto>(
                    @"SELECT s.student_id                   StudentId,
                             s.admission_no                 AdmissionNo,
                             s.student_name                 StudentName,
                             ISNULL(c.class_name,   '')      ClassName,
                             ISNULL(sec.section_name,'')     SectionName,
                             CONVERT(VARCHAR(10), s.date_of_birth, 120) DateOfBirth
                      FROM   students s
                      LEFT JOIN classes  c   ON c.class_id    = s.class_id
                      LEFT JOIN sections sec ON sec.section_id = s.section_id
                      WHERE  s.school_id = @schoolId
                        AND  s.status IN ('Active', 'Y')
                        AND  s.date_of_birth IS NOT NULL
                        AND  s.academic_year_id = (SELECT TOP 1 academic_year_id FROM academic_years
                                                    WHERE school_id = @schoolId AND status = 'Active'
                                                    ORDER BY academic_year_id DESC)
                        AND  (@classId IS NULL OR s.class_id = @classId)
                        AND  (
                                (MONTH(s.date_of_birth) = @month AND DAY(s.date_of_birth) = @day)
                             OR (@includeFeb29 = 1 AND MONTH(s.date_of_birth) = 2 AND DAY(s.date_of_birth) = 29)
                             )
                      ORDER BY c.sequence_no, sec.section_name, s.student_name",
                    new { schoolId, classId, month, day, includeFeb29 }).ToList();
            }
        }

        // ── Announcements ─────────────────────────────────────────────────

        public IEnumerable<AnnouncementDto> GetAnnouncements(string tenantDbName, int schoolId, int? classId, int? sectionId = null, int count = 30)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var anns = conn.Query<AnnouncementDto>(
                    @"SELECT TOP (@count)
                        announcement_id AnnouncementId,
                        title           Title,
                        description     Description,
                        scope           Scope,
                        is_pinned       IsPinned,
                        created_at      CreatedAt
                      FROM announcements
                      WHERE school_id = @schoolId
                        AND status    = 'Active'
                        AND (scope = 'School' OR (scope = 'Class' AND class_id = @classId))
                        AND (section_id IS NULL OR section_id = @sectionId)
                      ORDER BY is_pinned DESC, created_at DESC",
                    new { schoolId, classId, sectionId, count }).ToList();

                var media = LoadMedia(conn, "announcement", anns.Select(a => (long)a.AnnouncementId));
                foreach (var a in anns) a.Media = media[a.AnnouncementId].ToList();
                return anns;
            }
        }

        // R2 uploads (media_uploads) for a set of entity ids, grouped by entity_id.
        private static ILookup<long, MediaUploadDto> LoadMedia(IDbConnection conn, string entityType, IEnumerable<long> ids)
        {
            var list = ids?.Distinct().ToList() ?? new List<long>();
            if (list.Count == 0) return Enumerable.Empty<MediaUploadDto>().ToLookup(m => m.EntityId);
            return conn.Query<MediaUploadDto>(
                @"SELECT upload_id UploadId, entity_type EntityType, entity_id EntityId,
                         file_name FileName, file_url FileUrl, file_type FileType, file_size_kb FileSizeKb
                  FROM media_uploads
                  WHERE entity_type = @entityType AND entity_id IN @ids
                  ORDER BY upload_id",
                new { entityType, ids = list }).ToLookup(m => m.EntityId);
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

        private class ExamTimetableRow
        {
            public int    ExamTypeId   { get; set; }
            public string ExamTypeName { get; set; }
            public int?   DisplayOrder { get; set; }
            public string SubjectName  { get; set; }
            public string ExamDate     { get; set; }
            public string ExamTime     { get; set; }
            public string ExamRemarks  { get; set; }
        }

        private class HomeworkRow
        {
            public int      HomeworkId    { get; set; }
            public string   Title         { get; set; }
            public string   Description   { get; set; }
            public string   SubjectName   { get; set; }
            public DateTime  AssignedDate  { get; set; }
            public DateTime? DueDate       { get; set; }   // retired — may be NULL
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
