namespace AscentSchools.Core.DTOs.Mobile.Auth
{
    public class ResetPinRequest
    {
        public string Identifier { get; set; }   // mobile or email
        public string OtpCode    { get; set; }
        public string NewPin     { get; set; }
    }
}
