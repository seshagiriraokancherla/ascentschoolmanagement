namespace AscentSchools.Core.DTOs.School.Master
{
    public class TermDto
    {
        public int    TermId         { get; set; }
        public string TermName       { get; set; }
        public string YearName       { get; set; }
        public int?   OrderNo        { get; set; }
        public string Description    { get; set; }
        public int?   AcademicYearId { get; set; }
        public string Status         { get; set; }
        public int    SchoolId       { get; set; }
    }

    public class SaveTermRequest
    {
        public string TermName       { get; set; }
        public string YearName       { get; set; }
        public int?   OrderNo        { get; set; }
        public string Description    { get; set; }
        public int?   AcademicYearId { get; set; }
        public string Status         { get; set; }
    }
}
