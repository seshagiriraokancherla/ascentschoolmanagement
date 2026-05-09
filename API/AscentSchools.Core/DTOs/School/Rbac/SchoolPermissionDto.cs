namespace AscentSchools.Core.DTOs.School.Rbac
{
    public class SchoolPermissionDto
    {
        public int    PermissionId   { get; set; }
        public string PermissionCode { get; set; }  // e.g. STUDENT_FEE.COLLECT
        public string Description    { get; set; }
    }
}
