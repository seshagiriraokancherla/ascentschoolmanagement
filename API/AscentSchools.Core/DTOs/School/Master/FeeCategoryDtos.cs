namespace AscentSchools.Core.DTOs.School.Master
{
    public class FeeCategoryDto
    {
        public int    FeeCategoryId  { get; set; }
        public string CategoryName   { get; set; }
        public int?   AcademicYearId { get; set; }
        public string Description    { get; set; }
        public string Status         { get; set; }
        public int    SchoolId       { get; set; }
    }

    public class SaveFeeCategoryRequest
    {
        public string CategoryName   { get; set; }
        public int?   AcademicYearId { get; set; }
        public string Description    { get; set; }
        public string Status         { get; set; }
    }
}
