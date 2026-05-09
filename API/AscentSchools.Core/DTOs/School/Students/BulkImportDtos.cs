namespace AscentSchools.Core.DTOs.School.Students
{
    using System.Collections.Generic;

    // ── Promotion ─────────────────────────────────────────────────────────────

    public class PromoteStudentsRequest
    {
        public int  FromAcademicYearId { get; set; }
        public int  FromClassId        { get; set; }
        public int? FromSectionId      { get; set; }  // null = entire class
        public int  ToAcademicYearId   { get; set; }
        public int  ToClassId          { get; set; }
        public int? ToSectionId        { get; set; }  // null = auto-match by section name
    }

    public class PromoteStudentsResult
    {
        public int Total    { get; set; }
        public int Promoted { get; set; }
        public int Skipped  { get; set; }  // already promoted (row exists for target year)
    }

    public class PromotePreviewDto
    {
        public long   StudentId   { get; set; }
        public string AdmissionNo { get; set; }
        public string StudentName { get; set; }
        public string ClassName   { get; set; }
        public string SectionName { get; set; }
    }

    public class PromotePreviewResponse
    {
        public System.Collections.Generic.IEnumerable<PromotePreviewDto> Students      { get; set; }
        public int                                                        DetainedCount { get; set; }
    }

    // ── Student bulk import ───────────────────────────────────────────────────

    public class StudentBulkRow
    {
        public int    RowNumber     { get; set; }
        public string AdmissionNo   { get; set; }
        public string StudentName   { get; set; }
        public string Gender        { get; set; }  // Male / Female / Other
        public string DateOfBirth   { get; set; }  // dd/MM/yyyy or yyyy-MM-dd
        public string AcademicYear  { get; set; }  // e.g. "2024-25"
        public string ClassName     { get; set; }  // e.g. "Class 1"
        public string SectionName   { get; set; }  // e.g. "A"  (optional)
        public string RollNo        { get; set; }
        public string FeeCategory   { get; set; }  // e.g. "General"
        public string FatherName    { get; set; }
        public string MotherName    { get; set; }
        public string FatherMobile  { get; set; }
        public string MotherMobile  { get; set; }
        public string AadharNo      { get; set; }
        public string Caste         { get; set; }
        public string CasteCode     { get; set; }
        public string Religion      { get; set; }
        public string JoiningClass  { get; set; }
        public string MotherTongue  { get; set; }
        public string Status        { get; set; }  // Active / Inactive (default Active)
    }

    public class BulkStudentImportRequest
    {
        public List<StudentBulkRow> Rows { get; set; } = new List<StudentBulkRow>();
    }

    // ── Fee structure bulk import ─────────────────────────────────────────────

    public class FeeStructureBulkRow
    {
        public int    RowNumber    { get; set; }
        public string AcademicYear { get; set; }  // e.g. "2024-25"
        public string ClassName    { get; set; }  // e.g. "Class 1"
        public string FeeCategory  { get; set; }  // e.g. "General"
        public string FeeType      { get; set; }  // e.g. "Tuition Fee"
        public string Term         { get; set; }  // e.g. "April" or "Term 1"
        public string Amount       { get; set; }  // string → parsed as decimal on server
    }

    public class BulkFeeStructureImportRequest
    {
        public List<FeeStructureBulkRow> Rows { get; set; } = new List<FeeStructureBulkRow>();
    }

    // ── Shared result types ───────────────────────────────────────────────────

    public class BulkRowError
    {
        public int    Row          { get; set; }
        public string AdmissionNo  { get; set; }  // student import only
        public string Identifier   { get; set; }  // fee import: "ClassName / FeeType / Term"
        public string Reason       { get; set; }
    }

    public class BulkImportResult
    {
        public int              Total    { get; set; }
        public int              Imported { get; set; }
        public int              Failed   { get; set; }
        public List<BulkRowError> Errors { get; set; } = new List<BulkRowError>();
    }
}
