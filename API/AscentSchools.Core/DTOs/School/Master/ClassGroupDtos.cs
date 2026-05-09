namespace AscentSchools.Core.DTOs.School.Master
{
    public class ClassGroupDto
    {
        public int    ClassGroupId { get; set; }
        public string GroupName    { get; set; }
        public string Description  { get; set; }
        public string Prefix       { get; set; }
        public string Status       { get; set; }
        public int    SchoolId     { get; set; }
    }

    public class SaveClassGroupRequest
    {
        public string GroupName   { get; set; }
        public string Description { get; set; }
        public string Prefix      { get; set; }
        public string Status      { get; set; }
    }
}
