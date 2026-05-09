namespace AscentSchools.Core.DTOs.Auth
{
    public class LoginResponse
    {
        public string   AccessToken  { get; set; }
        public int      UserId       { get; set; }
        public string   FullName     { get; set; }
        public int      GroupId      { get; set; }
        public int?     SchoolId     { get; set; }
        public string[] Permissions  { get; set; }
        // Refresh token is NOT returned in body — set as HttpOnly cookie by the API
    }
}
