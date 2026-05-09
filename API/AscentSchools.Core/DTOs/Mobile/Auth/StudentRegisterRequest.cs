namespace AscentSchools.Core.DTOs.Mobile.Auth
{
    public class StudentRegisterRequest
    {
        public string AdmissionNo { get; set; }
        public string Pin         { get; set; }   // 4–6 digit PIN
        public string Mobile      { get; set; }
        public string Email       { get; set; }
    }
}
