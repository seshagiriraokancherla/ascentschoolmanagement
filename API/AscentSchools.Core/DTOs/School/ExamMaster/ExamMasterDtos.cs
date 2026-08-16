using System;
using System.Collections.Generic;

namespace AscentSchools.Core.DTOs.School.ExamMaster
{
    /// <summary>One exam_master row (an exam defined for a class + subject).</summary>
    public class ExamMasterDto
    {
        public int       Id              { get; set; }
        public string    ExamName        { get; set; }
        public int?      ExamTypeId      { get; set; }
        public string    ExamTypeName    { get; set; }
        public int?      ClassId         { get; set; }
        public string    ClassName       { get; set; }
        public int?      SubjectId       { get; set; }
        public string    SubjectName     { get; set; }
        public int?      AcademicYearId  { get; set; }
        public string    ExamCategory    { get; set; }
        public DateTime? ExamDate        { get; set; }
        public int?      ExamTotalMarks  { get; set; }
        public int?      ExamMinMarks    { get; set; }
        public int?      SubMaxMarks     { get; set; }
        public int?      SubjectMinMarks { get; set; }
        public decimal?  ActivityMaxMarks { get; set; }
        public string    ExamRemarks     { get; set; }
        public int?      GradeTypeId     { get; set; }
        public string    GradeName       { get; set; }
        public string    ExamStatus      { get; set; }
    }

    /// <summary>
    /// Create one exam per selected subject (all share the header fields).
    /// Existing rows for the same year+exam type+class+subject are updated.
    /// </summary>
    public class CreateExamMasterRequest
    {
        public string    ExamName        { get; set; }
        public int       ExamTypeId      { get; set; }
        public int       ClassId         { get; set; }
        public int       AcademicYearId  { get; set; }
        public List<int> SubjectIds      { get; set; }
        public string    ExamCategory    { get; set; }
        public DateTime? ExamDate        { get; set; }
        public int?      ExamTotalMarks  { get; set; }
        public int?      ExamMinMarks    { get; set; }
        public int?      SubMaxMarks     { get; set; }
        public int?      SubjectMinMarks { get; set; }
        public decimal?  ActivityMaxMarks { get; set; }
        public string    ExamRemarks     { get; set; }
        public int?      GradeTypeId     { get; set; }
        public string    ExamStatus      { get; set; }
    }

    // ── Bulk import (CSV) ──────────────────────────────────────────────────────
    // All references are BY NAME. GradeType is optional (blank = none). A row whose
    // (year, exam type, class, subject) already exists is skipped. Result reuses
    // AscentSchools.Core.DTOs.School.Students.BulkImportResult.
    public class ExamMasterBulkRow
    {
        public string   AcademicYear { get; set; }   // e.g. "2026-27"
        public string   ExamType     { get; set; }   // exam type name
        public string   Class        { get; set; }   // class name
        public string   Subject      { get; set; }   // subject name
        public string   ExamName     { get; set; }
        public string   Category     { get; set; }
        public string   ExamDate     { get; set; }   // parseable date, optional
        public int?     TotalMarks   { get; set; }   // exam_total_marks
        public int?     ExamMinMarks { get; set; }
        public int?     SubjectMax   { get; set; }   // sub_max_marks
        public int?     SubjectMin   { get; set; }   // subject_min_marks
        public decimal? ActivityMax  { get; set; }   // activity_max_marks
        public string   GradeType    { get; set; }   // grade_name, optional
        public string   Remarks      { get; set; }
        public string   Status       { get; set; }
    }

    public class BulkExamMasterRequest
    {
        public List<ExamMasterBulkRow> Rows { get; set; }
    }

    /// <summary>Update a single exam_master row.</summary>
    public class UpdateExamMasterRequest
    {
        public string    ExamName        { get; set; }
        public int       ExamTypeId      { get; set; }
        public int       ClassId         { get; set; }
        public int       AcademicYearId  { get; set; }
        public int       SubjectId       { get; set; }
        public string    ExamCategory    { get; set; }
        public DateTime? ExamDate        { get; set; }
        public int?      ExamTotalMarks  { get; set; }
        public int?      ExamMinMarks    { get; set; }
        public int?      SubMaxMarks     { get; set; }
        public int?      SubjectMinMarks { get; set; }
        public decimal?  ActivityMaxMarks { get; set; }
        public string    ExamRemarks     { get; set; }
        public int?      GradeTypeId     { get; set; }
        public string    ExamStatus      { get; set; }
    }
}
