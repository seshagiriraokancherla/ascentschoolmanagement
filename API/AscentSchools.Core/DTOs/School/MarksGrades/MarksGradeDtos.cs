using System.Collections.Generic;

namespace AscentSchools.Core.DTOs.School.MarksGrades
{
    /// <summary>
    /// A grading scale (marks_grade_master) — a reusable named set of bands for a
    /// student's TOTAL marks, e.g. "Out of 300". School-wide: not tied to a class,
    /// section or academic year. Each exam row picks one (exam_master.marks_grade_master_id).
    /// </summary>
    public class MarksGradeScaleDto
    {
        public int    Id          { get; set; }
        public string ScaleName   { get; set; }
        public string Description { get; set; }
        public string Status      { get; set; }
        public int    BandCount   { get; set; }
        public int    ExamCount   { get; set; }   // exams using it (a used scale can't be deleted)
    }

    /// <summary>One band of a scale (raw marks, not %).</summary>
    public class MarksGradeBandDto
    {
        public int      Id          { get; set; }
        public decimal  MinMarks    { get; set; }
        public decimal  MaxMarks    { get; set; }
        public string   Grade       { get; set; }
        public string   GradePoint  { get; set; }
        public string   Description { get; set; }
    }

    /// <summary>Create or rename a scale.</summary>
    public class SaveMarksGradeScaleRequest
    {
        public string ScaleName   { get; set; }
        public string Description { get; set; }
        public string Status      { get; set; }
    }

    /// <summary>Full replacement of a scale's bands (delete + insert).</summary>
    public class SaveMarksGradeBandsRequest
    {
        public List<MarksGradeBand> Bands { get; set; }
    }

    public class MarksGradeBand
    {
        public decimal? MinMarks    { get; set; }
        public decimal? MaxMarks    { get; set; }
        public string   Grade       { get; set; }
        public string   GradePoint  { get; set; }
        public string   Description { get; set; }
    }
}
