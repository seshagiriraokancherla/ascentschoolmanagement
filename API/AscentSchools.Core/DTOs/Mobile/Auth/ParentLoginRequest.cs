namespace AscentSchools.Core.DTOs.Mobile.Auth
{
    public class ParentLoginRequest
    {
        /// <summary>Mobile number or email address used at registration.</summary>
        public string Identifier { get; set; }
        public string Pin        { get; set; }
    }
}
