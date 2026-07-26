using AscentSchools.API.Filters;
using AscentSchools.API.Middleware;
using AscentSchools.Core.DTOs.School.Announcements;
using AscentSchools.Core.DTOs.School.Attendance;
using AscentSchools.Core.DTOs.School.Homework;
using AscentSchools.Core.Models;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.Mobile
{
    [RoutePrefix("mobile/teacher")]
    [MobileTeacherAuth]
    public class MobileTeacherController : ApiController
    {
        private readonly AttendanceRepository    _attendance;
        private readonly HomeworkRepository      _homework;
        private readonly AnnouncementsRepository _announcements;
        private readonly TenantConnectionFactory _db;

        private TeacherContext Teacher => TeacherContext.Current;

        public MobileTeacherController()
        {
            _db            = new TenantConnectionFactory();
            _attendance    = new AttendanceRepository(_db);
            _homework      = new HomeworkRepository(_db);
            _announcements = new AnnouncementsRepository(_db);
        }

        // ── GET /mobile/teacher/classes ───────────────────────────────────────

        [HttpGet, Route("classes")]
        public HttpResponseMessage GetClasses()
        {
            using (var conn = _db.GetTenantConnection(Teacher.DbName))
            {
                var classes = conn.Query<TeacherClassDto>(
                    @"SELECT class_id ClassId, class_name ClassName
                      FROM classes
                      WHERE school_id = @schoolId AND status = 'Active'
                      ORDER BY class_name",
                    new { Teacher.SchoolId });

                return Ok(classes);
            }
        }

        // ── GET /mobile/teacher/sections?classId= ─────────────────────────────

        [HttpGet, Route("sections")]
        public HttpResponseMessage GetSections([FromUri] int classId)
        {
            if (classId <= 0) return Fail(HttpStatusCode.BadRequest, "classId is required.");

            using (var conn = _db.GetTenantConnection(Teacher.DbName))
            {
                var sections = conn.Query<TeacherSectionDto>(
                    @"SELECT section_id SectionId, section_name SectionName
                      FROM sections
                      WHERE class_id = @classId AND school_id = @schoolId AND status = 'Active'
                      ORDER BY section_name",
                    new { classId, Teacher.SchoolId });

                return Ok(sections);
            }
        }

        // ── GET /mobile/teacher/attendance?classId=&sectionId=&date= ─────────
        // Returns all students with their attendance status for the given date.
        // Status = null when attendance has not been marked yet for that day.

        [HttpGet, Route("attendance")]
        public HttpResponseMessage GetAttendance(
            [FromUri] int classId, [FromUri] int sectionId, [FromUri] string date)
        {
            if (classId <= 0 || sectionId <= 0)
                return Fail(HttpStatusCode.BadRequest, "classId and sectionId are required.");
            if (string.IsNullOrWhiteSpace(date) || !DateTime.TryParse(date, out _))
                return Fail(HttpStatusCode.BadRequest, "A valid date (yyyy-MM-dd) is required.");

            var grid = _attendance.GetAttendanceGrid(Teacher.DbName, Teacher.SchoolId, classId, sectionId, date);
            return Ok(grid);
        }

        // ── POST /mobile/teacher/attendance ──────────────────────────────────

        [HttpPost, Route("attendance")]
        public HttpResponseMessage SaveAttendance([FromBody] TeacherSaveAttendanceRequest request)
        {
            if (request == null || request.ClassId <= 0)
                return Fail(HttpStatusCode.BadRequest, "classId is required.");
            if (string.IsNullOrWhiteSpace(request.Date) || !DateTime.TryParse(request.Date, out _))
                return Fail(HttpStatusCode.BadRequest, "A valid date (yyyy-MM-dd) is required.");
            if (request.Entries == null || !request.Entries.Any())
                return Fail(HttpStatusCode.BadRequest, "No entries provided.");

            var validStatuses = new[] { "Present", "Absent", "Late", "HalfDay", "Holiday" };
            if (request.Entries.Any(e => !validStatuses.Contains(e.Status)))
                return Fail(HttpStatusCode.BadRequest, "Status must be Present, Absent, Late, HalfDay or Holiday.");

            var saveReq = new SaveAttendanceRequest
            {
                ClassId = request.ClassId,
                Date    = request.Date,
                Entries = request.Entries.Select(e => new AttendanceEntryDto
                {
                    StudentId = e.StudentId,
                    Status    = e.Status,
                    Remarks   = e.Remarks,
                }),
            };

            _attendance.SaveAttendance(Teacher.DbName, Teacher.SchoolId, Teacher.FullName, saveReq);
            return Ok(true, "Attendance saved.");
        }

        // ── GET /mobile/teacher/homework?classId= ────────────────────────────

        [HttpGet, Route("homework")]
        public HttpResponseMessage GetHomework([FromUri] int classId)
        {
            if (classId <= 0) return Fail(HttpStatusCode.BadRequest, "classId is required.");
            var list = _homework.GetHomework(Teacher.DbName, Teacher.SchoolId, classId);
            return Ok(list);
        }

        // ── POST /mobile/teacher/homework ─────────────────────────────────────

        [HttpPost, Route("homework")]
        public HttpResponseMessage CreateHomework([FromBody] TeacherCreateHomeworkRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Title))
                return Fail(HttpStatusCode.BadRequest, "Title is required.");
            if (request.ClassId <= 0)
                return Fail(HttpStatusCode.BadRequest, "classId is required.");

            var assignedDate = DateTime.TryParse(request.AssignedDate, out var ad) ? ad : DateTime.Today;

            var saveReq = new SaveHomeworkRequest
            {
                Title        = request.Title.Trim(),
                Description  = request.Description?.Trim(),
                SubjectId    = null,
                ClassId      = request.ClassId,
                AssignedDate = assignedDate,
                DueDate      = null,
            };

            var id = _homework.CreateHomework(Teacher.DbName, Teacher.SchoolId, Teacher.FullName, saveReq);
            return Request.CreateResponse(HttpStatusCode.Created,
                ApiResponse<object>.Ok(new { homeworkId = id }, "Homework created."));
        }

        // ── GET /mobile/teacher/announcements?classId= ───────────────────────
        // Class announcements (plus school-wide) for the selected class.

        [HttpGet, Route("announcements")]
        public HttpResponseMessage GetAnnouncements([FromUri] int classId)
        {
            if (classId <= 0) return Fail(HttpStatusCode.BadRequest, "classId is required.");
            var list = _announcements.GetAnnouncements(Teacher.DbName, Teacher.SchoolId, classId);
            return Ok(list);
        }

        // ── POST /mobile/teacher/announcements ───────────────────────────────
        // Teacher posts to their class (optionally a single section).

        [HttpPost, Route("announcements")]
        public HttpResponseMessage CreateAnnouncement([FromBody] TeacherCreateAnnouncementRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Title))
                return Fail(HttpStatusCode.BadRequest, "Title is required.");
            if (request.ClassId <= 0)
                return Fail(HttpStatusCode.BadRequest, "classId is required.");

            var saveReq = new SaveAnnouncementRequest
            {
                Title       = request.Title.Trim(),
                Description = request.Description?.Trim(),
                Scope       = "Class",
                ClassId     = request.ClassId,
                SectionId   = request.SectionId > 0 ? request.SectionId : (int?)null,
                IsPinned    = false,
            };

            var id = _announcements.CreateAnnouncement(Teacher.DbName, Teacher.SchoolId, Teacher.FullName, saveReq);

            new AscentSchools.API.Helpers.PushNotifier().NotifyClass(
                Teacher.DbName, Teacher.GroupId, Teacher.SchoolId,
                saveReq.ClassId, saveReq.SectionId,
                "New Announcement", saveReq.Title, "announcement", id);

            return Request.CreateResponse(HttpStatusCode.Created,
                ApiResponse<object>.Ok(new { announcementId = id }, "Announcement posted."));
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private HttpResponseMessage Ok<T>(T data, string message = null) =>
            Request.CreateResponse(HttpStatusCode.OK, ApiResponse<T>.Ok(data, message));

        private HttpResponseMessage Fail(HttpStatusCode code, string message) =>
            Request.CreateResponse(code, new { success = false, message });
    }

    // ── DTOs (teacher data) ──────────────────────────────────────────────────

    public class TeacherClassDto
    {
        public int    ClassId   { get; set; }
        public string ClassName { get; set; }
    }

    public class TeacherSectionDto
    {
        public int    SectionId   { get; set; }
        public string SectionName { get; set; }
    }

    public class TeacherSaveAttendanceRequest
    {
        public int    ClassId   { get; set; }
        public int    SectionId { get; set; }
        public string Date      { get; set; }
        public IEnumerable<TeacherAttendanceEntryDto> Entries { get; set; }
    }

    public class TeacherAttendanceEntryDto
    {
        public long   StudentId { get; set; }
        public string Status    { get; set; }
        public string Remarks   { get; set; }
    }

    public class TeacherCreateHomeworkRequest
    {
        public int    ClassId      { get; set; }
        public string Title        { get; set; }
        public string Description  { get; set; }
        public string AssignedDate { get; set; }
    }

    public class TeacherCreateAnnouncementRequest
    {
        public int    ClassId     { get; set; }
        public int?   SectionId   { get; set; }   // optional; NULL/0 = whole class
        public string Title       { get; set; }
        public string Description { get; set; }
    }
}
