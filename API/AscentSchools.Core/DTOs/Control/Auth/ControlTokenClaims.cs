namespace AscentSchools.Core.DTOs.Control.Auth
{
    public class ControlTokenClaims
    {
        public int    UserId   { get; set; }
        public string FullName { get; set; }
        public string Role     { get; set; }
    }
}
