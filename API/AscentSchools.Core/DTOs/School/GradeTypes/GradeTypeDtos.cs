using System.Collections.Generic;

namespace AscentSchools.Core.DTOs.School.GradeTypes
{
    /// <summary>A grade band (e.g. 90-100 → A+), optionally scoped to a subject.</summary>
    public class GradeTypeDto
    {
        public int     Id          { get; set; }
        public string  GradeName   { get; set; }
        public int?    SubjectId   { get; set; }
        public string  SubjectName { get; set; }
        public double? MinMarks    { get; set; }
        public double? MaxMarks    { get; set; }
        public string  Grade       { get; set; }
        public string  Remarks     { get; set; }
        public string  Status      { get; set; }
    }

    public class SaveGradeTypeRequest
    {
        public string  GradeName { get; set; }
        public int?    SubjectId { get; set; }
        public double? MinMarks  { get; set; }
        public double? MaxMarks  { get; set; }
        public string  Grade     { get; set; }
        public string  Remarks   { get; set; }
        public string  Status    { get; set; }
    }

    // ── Bulk import (CSV) ──────────────────────────────────────────────────────
    // Subject is matched BY NAME (blank = all subjects); result reuses
    // AscentSchools.Core.DTOs.School.Students.BulkImportResult.

    public class GradeTypeBulkRow
    {
        public string  GradeName   { get; set; }
        public string  SubjectName { get; set; }   // blank = all subjects
        public double? MinMarks    { get; set; }
        public double? MaxMarks    { get; set; }
        public string  Grade       { get; set; }
        public string  Remarks     { get; set; }
        public string  Status      { get; set; }
    }

    public class BulkGradeTypeRequest
    {
        public List<GradeTypeBulkRow> Rows { get; set; }
    }
}
