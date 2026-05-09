namespace AscentSchools.Core.DTOs.School.Rbac
{
    public class SchoolRoleDto
    {
        public int     RoleId          { get; set; }
        public string  RoleName        { get; set; }
        public string  Description     { get; set; }
        public int?    SchoolId        { get; set; }   // null = group-wide role
        public int     PermissionCount { get; set; }
    }
}
