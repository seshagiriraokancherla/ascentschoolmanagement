using System.Collections.Generic;

namespace AscentSchools.Core.DTOs.School.Marks
{
    // ── Exam Types ────────────────────────────────────────────────────────────

    public class ExamTypeDto
    {
        public int    ExamTypeId      { get; set; }
        public string ExamTypeName    { get; set; }
        public int?   AcademicYearId  { get; set; }
        public int?   DisplayOrder    { get; set; }
        public string Status          { get; set; }
    }

    public class SaveExamTypeRequest
    {
        public string ExamTypeName   { get; set; }
        public int?   AcademicYearId { get; set; }
        public int?   DisplayOrder   { get; set; }
    }

    // ── Marks Grid ────────────────────────────────────────────────────────────

    public class MarksGridDto
    {
        public IEnumerable<SubjectHeaderDto>    Subjects { get; set; }
        public IEnumerable<StudentMarksRowDto>  Rows     { get; set; }
    }

    public class SubjectHeaderDto
    {
        public int    SubjectId   { get; set; }
        public string SubjectName { get; set; }
    }

    public class StudentMarksRowDto
    {
        public long   StudentId   { get; set; }
        public string StudentName { get; set; }
        public string AdmissionNo { get; set; }
        public IEnumerable<MarkCellDto> Marks { get; set; }
    }

    public class MarkCellDto
    {
        public int      SubjectId      { get; set; }
        public decimal? MarksObtained  { get; set; }
        public decimal  MaxMarks       { get; set; }
        public bool     IsAbsent       { get; set; }
    }

    // ── Save Request ──────────────────────────────────────────────────────────

    public class SaveMarksRequest
    {
        public int   ClassId         { get; set; }
        public int   ExamTypeId      { get; set; }
        public int   AcademicYearId  { get; set; }
        public decimal MaxMarks      { get; set; }
        public IEnumerable<StudentMarkEntry> Entries { get; set; }
    }

    public class StudentMarkEntry
    {
        public long    StudentId      { get; set; }
        public int     SubjectId      { get; set; }
        public decimal MarksObtained  { get; set; }
        public bool    IsAbsent       { get; set; }
    }
}
