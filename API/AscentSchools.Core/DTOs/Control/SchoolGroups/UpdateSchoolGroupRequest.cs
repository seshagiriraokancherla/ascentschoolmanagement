namespace AscentSchools.Core.DTOs.Control.SchoolGroups
{
    public class UpdateSchoolGroupRequest
    {
        public string GroupName   { get; set; }
        public string Description { get; set; }
        public string Status      { get; set; }
        public string DbName      { get; set; }
        public string DbUsername  { get; set; }
        public string DbPassword  { get; set; }
        public string LoginCode   { get; set; }   // 4-digit numeric code for the single-app login
    }
}
