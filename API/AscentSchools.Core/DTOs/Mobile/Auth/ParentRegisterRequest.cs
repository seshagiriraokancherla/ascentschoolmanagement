namespace AscentSchools.Core.DTOs.Mobile.Auth
{
    public class ParentRegisterRequest
    {
        public string FullName { get; set; }
        public string Mobile   { get; set; }
        public string Email    { get; set; }
        public string Pin      { get; set; }   // 4–6 digit PIN
    }
}
