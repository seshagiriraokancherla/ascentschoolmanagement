namespace AscentSchools.Core.DTOs.Control.Auth
{
    public class ControlLoginResponse
    {
        public string AccessToken { get; set; }
        public int    UserId      { get; set; }
        public string FullName    { get; set; }
        public string Role        { get; set; }
        // Refresh token is NOT returned in body — set as HttpOnly cookie by the API
    }
}
