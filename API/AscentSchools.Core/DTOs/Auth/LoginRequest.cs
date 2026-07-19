namespace AscentSchools.Core.DTOs.Auth
{
    public class LoginRequest
    {
        public string Username  { get; set; }
        public string Password  { get; set; }
        public int?   SchoolId  { get; set; }   // Optional: restrict to a specific branch on login
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; }
        public string NewPassword     { get; set; }
    }
}
