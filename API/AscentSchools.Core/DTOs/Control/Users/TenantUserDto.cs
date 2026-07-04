using System;

namespace AscentSchools.Core.DTOs.Control.Users
{
    public class TenantUserDto
    {
        public int      UserId      { get; set; }
        public string   Username    { get; set; }
        public string   FullName    { get; set; }
        public string   Email       { get; set; }
        public string   Mobile      { get; set; }
        public string   Status      { get; set; }
        public DateTime CreatedAt   { get; set; }
        public string   Roles       { get; set; }   // comma-separated role names for display
        public int?     EmployeeId  { get; set; }   // linked staff_id (NULL for the admin)
        public string   StaffName   { get; set; }   // from staff, for display
        public string   Designation { get; set; }   // from staff, for display
    }
}
