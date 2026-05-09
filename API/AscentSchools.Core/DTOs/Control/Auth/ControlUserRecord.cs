namespace AscentSchools.Core.DTOs.Control.Auth
{
    // Internal-use record returned by ControlUserRepository for login validation.
    // Not exposed in API responses.
    public class ControlUserRecord
    {
        public int    UserId       { get; set; }
        public string FullName     { get; set; }
        public string Username     { get; set; }
        public string PasswordHash { get; set; }
        public string Role         { get; set; }
        public string Status       { get; set; }
    }
}
