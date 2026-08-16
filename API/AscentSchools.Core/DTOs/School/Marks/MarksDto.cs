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

    // ── Marks Grid (web — all of the class's mapped subjects) ──────────────────

    public class MarksGridDto
    {
        public IEnumerable<SubjectHeaderDto>    Subjects { get; set; }
        public IEnumerable<StudentMarksRowDto>  Rows     { get; set; }
    }

    /// <summary>
    /// A subject column. Max / activity config comes from the matching exam_master
    /// row (year+exam type+class+subject); falls back to max 100 / no activity when
    /// the exam isn't defined yet.
    /// </summary>
    public class SubjectHeaderDto
    {
        public int      SubjectId        { get; set; }
        public string   SubjectName      { get; set; }
        public int?     ExamId           { get; set; }   // exam_master.id (NULL when not defined)
        public decimal  MaxMarks         { get; set; }
        public decimal? ActivityMaxMarks { get; set; }
        public bool     HasActivity      { get; set; }
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
        public int      SubjectId        { get; set; }
        public decimal? MarksObtained    { get; set; }
        public decimal? ActivityMarks    { get; set; }
        public decimal  MaxMarks         { get; set; }
        public decimal? ActivityMaxMarks { get; set; }
        public bool     IsAbsent         { get; set; }
    }

    // ── Single-subject view (mobile — one subject at a time) ───────────────────

    public class SubjectMarksDto
    {
        public int      SubjectId        { get; set; }
        public string   SubjectName      { get; set; }
        public int?     ExamId           { get; set; }
        public decimal  MaxMarks         { get; set; }
        public decimal? ActivityMaxMarks { get; set; }
        public bool     HasActivity      { get; set; }
        public IEnumerable<StudentSubjectMarkDto> Students { get; set; }
    }

    public class StudentSubjectMarkDto
    {
        public long     StudentId     { get; set; }
        public string   StudentName   { get; set; }
        public string   AdmissionNo   { get; set; }
        public decimal? MarksObtained { get; set; }
        public decimal? ActivityMarks { get; set; }
        public bool     IsAbsent      { get; set; }
    }

    // ── Save Request (shared web + mobile) ─────────────────────────────────────

    public class SaveMarksRequest
    {
        public int ClassId        { get; set; }
        public int ExamTypeId     { get; set; }
        public int AcademicYearId { get; set; }
        public IEnumerable<StudentMarkEntry> Entries { get; set; }
    }

    /// <summary>Max / activity max travel with each entry (per-subject, from exam config).</summary>
    public class StudentMarkEntry
    {
        public long     StudentId        { get; set; }
        public int      SubjectId        { get; set; }
        public int?     ExamId           { get; set; }
        public decimal  MarksObtained    { get; set; }
        public decimal  MaxMarks         { get; set; }
        public decimal? ActivityMarks    { get; set; }
        public decimal? ActivityMaxMarks { get; set; }
        public bool     IsAbsent         { get; set; }
    }
}
