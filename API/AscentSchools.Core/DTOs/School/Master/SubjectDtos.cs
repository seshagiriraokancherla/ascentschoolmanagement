namespace AscentSchools.Core.DTOs.School.Master
{
    public class SubjectDto
    {
        public int    SubjectId      { get; set; }
        public string SubjectName    { get; set; }
        public string ShortName      { get; set; }
        public string SubjectType    { get; set; }
        public string Description    { get; set; }
        public string Remarks        { get; set; }
        public int?   AcademicYearId { get; set; }
        public string Status         { get; set; }
        public int    SchoolId       { get; set; }
    }

    public class SaveSubjectRequest
    {
        public string SubjectName    { get; set; }
        public string ShortName      { get; set; }
        public string SubjectType    { get; set; }
        public string Description    { get; set; }
        public string Remarks        { get; set; }
        public int?   AcademicYearId { get; set; }
        public string Status         { get; set; }
    }
}
