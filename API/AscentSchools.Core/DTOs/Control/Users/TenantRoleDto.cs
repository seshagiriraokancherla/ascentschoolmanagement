namespace AscentSchools.Core.DTOs.Control.Users
{
    public class TenantRoleDto
    {
        public int    RoleId      { get; set; }
        public string RoleName    { get; set; }
        public string Description { get; set; }
        public int?   SchoolId    { get; set; }   // NULL = group-wide role
    }
}
