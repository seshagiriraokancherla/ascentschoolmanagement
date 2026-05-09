namespace AscentSchools.Core.DTOs.School.Rbac
{
    public class CreateSchoolRoleRequest
    {
        public string RoleName    { get; set; }
        public string Description { get; set; }
        public int?   SchoolId    { get; set; }   // null = group-wide
    }
}
