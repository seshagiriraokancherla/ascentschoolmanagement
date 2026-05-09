namespace AscentSchools.Core.DTOs.School.Auth
{
    // Internal-use record for login validation — not exposed in API responses.
    public class SchoolUserRecord
    {
        public int    UserId       { get; set; }
        public string Username     { get; set; }
        public string FullName     { get; set; }
        public string PasswordHash { get; set; }
        public string Status       { get; set; }
        public int?   SchoolId     { get; set; }   // NULL = access to all branches
    }
}
