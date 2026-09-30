using System.Collections.Generic;

namespace AscentSchools.Core.DTOs.School.Marks
{
    /// <summary>
    /// Everything needed to print the marks cards of one section for one exam.
    /// Totals, grades and rank are computed server-side so a single-student print
    /// still carries the student's rank within the whole section.
    /// </summary>
    public class MarksCardDataDto
    {
        public string AcademicYear  { get; set; }
        public string ExamTypeName  { get; set; }
        public string ClassName     { get; set; }
        public string SectionName   { get; set; }
        public string ScaleName     { get; set; }   // total-grade scale chosen on the exam rows (null = not set)
        public bool   ScaleHasBands { get; set; }   // scale set AND it has bands
        public bool   ScaleConflict { get; set; }   // this class's exam rows point at DIFFERENT scales
        public List<MarksCardSubjectDto> Subjects { get; set; }
        public List<MarksCardStudentDto> Students { get; set; }

        /// <summary>Attendance columns: June (start year) → March (end year), always 10 months.</summary>
        public List<MarksCardMonthDto> AttendanceMonths { get; set; }
        public int TotalWorkingDays { get; set; }
    }

    /// <summary>
    /// One attendance month. WorkingDays = distinct days attendance was marked for the
    /// class+section that month (Holiday rows excluded); null when nothing was marked.
    /// </summary>
    public class MarksCardMonthDto
    {
        public int    Year        { get; set; }
        public int    Month       { get; set; }
        public string Label       { get; set; }   // Jun, Jul, … Mar
        public int?   WorkingDays { get; set; }
    }

    public class MarksCardSubjectDto
    {
        public int      SubjectId        { get; set; }
        public string   SubjectName      { get; set; }
        public decimal  MaxMarks         { get; set; }   // written / main
        public decimal? ActivityMaxMarks { get; set; }   // internal (null = none)
        public bool     HasActivity      { get; set; }
        public bool     HasGradeType     { get; set; }   // exam_master row has a grade type
    }

    public class MarksCardStudentDto
    {
        public long    StudentId   { get; set; }
        public string  StudentName { get; set; }
        public string  AdmissionNo { get; set; }
        public string  FatherName  { get; set; }
        public List<MarksCardLineDto> Lines { get; set; }
        public bool    HasMarks    { get; set; }   // at least one subject entered or absent
        public decimal Total       { get; set; }
        public decimal MaxTotal    { get; set; }
        public string  TotalGrade  { get; set; }
        public int?    Rank        { get; set; }   // within the section; null when no marks

        /// <summary>
        /// Present days per month, aligned with MarksCardDataDto.AttendanceMonths
        /// (Present + Late = 1, HalfDay = 0.5); null for a month with no working days.
        /// </summary>
        public List<decimal?> PresentDays  { get; set; }
        public decimal        TotalPresent { get; set; }
    }

    public class MarksCardLineDto
    {
        public int      SubjectId     { get; set; }
        public decimal? ActivityMarks { get; set; }   // internal
        public decimal? MarksObtained { get; set; }   // written
        public bool     IsAbsent      { get; set; }
        public bool     Entered       { get; set; }
        public decimal? SubjectTotal  { get; set; }   // null when absent / not entered
        public string   Grade         { get; set; }
    }
}
