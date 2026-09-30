using System.Collections.Generic;

namespace AscentSchools.Core.DTOs.Mobile.Data
{
    public class MarksResultDto
    {
        public int    AcademicYearId   { get; set; }
        public string AcademicYearName { get; set; }
        public IEnumerable<ExamResultDto> Exams { get; set; }
    }

    public class ExamResultDto
    {
        public int    ExamTypeId   { get; set; }
        public string ExamTypeName { get; set; }
        public IEnumerable<SubjectMarkDto> Marks { get; set; }
        public decimal TotalObtained { get; set; }
        public decimal TotalMax      { get; set; }
        public decimal Percentage    { get; set; }
    }

    public class SubjectMarkDto
    {
        public string  SubjectName    { get; set; }
        public decimal MarksObtained  { get; set; }
        public decimal MaxMarks       { get; set; }
        public bool    IsAbsent       { get; set; }
    }

    // Exam timetable — read-only per-subject schedule for a class+exam type, sourced from
    // exam_master.exam_date (the same field Master Data → Exam Master already captures).
    public class ExamTimetableGroupDto
    {
        public int    ExamTypeId   { get; set; }
        public string ExamTypeName { get; set; }
        public IEnumerable<ExamTimetableSubjectDto> Subjects { get; set; }
    }

    public class ExamTimetableSubjectDto
    {
        public string SubjectName  { get; set; }
        public string ExamDate     { get; set; }   // ISO date string (yyyy-MM-dd) — avoids timezone shifts
        public string ExamTime     { get; set; }   // "HH:mm" 24-hour, or null if not set
        public string ExamRemarks  { get; set; }   // exam_master.exam_remarks — syllabus/portion notes staff enter in Exam Master
    }
}
