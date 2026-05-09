namespace AscentSchools.Core.DTOs.Control.SchoolGroups
{
    public class CreateSchoolGroupRequest
    {
        public string GroupName   { get; set; }
        public string Subdomain   { get; set; }   // Must be unique — used in tenant routing
        public string Description { get; set; }
    }
}
