namespace AscentSchools.Core.DTOs.Control.Users
{
    public class CreateTenantUserRequest
    {
        public string FullName  { get; set; }
        public string Username  { get; set; }
        public string Password  { get; set; }
        public string Email     { get; set; }
        public string Mobile    { get; set; }
        public int    RoleId     { get; set; }
        public int[]  SchoolIds  { get; set; }   // branches to assign the role to
        public int?   EmployeeId { get; set; }   // staff_id this login belongs to (required by school app; NULL for the control-app admin)
    }
}
