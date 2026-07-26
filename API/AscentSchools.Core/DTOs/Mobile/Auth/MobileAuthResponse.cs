namespace AscentSchools.Core.DTOs.Mobile.Auth
{
    public class MobileAuthResponse
    {
        public string AccessToken { get; set; }
        // Raw refresh token — returned so the mobile app can store it itself
        // (EncryptedSharedPreferences) and send it back explicitly on refresh,
        // instead of relying on the HttpOnly cookie surviving app kill.
        public string RefreshToken { get; set; }
        public string TokenType   { get; set; }   // "student" or "parent"
        public string FullName    { get; set; }
        public string ClassName   { get; set; }
        public string SectionName { get; set; }
        public string AdmissionNo { get; set; }
        public long   StudentId   { get; set; }
        public int    ParentId    { get; set; }
    }

    public class ChildDto
    {
        public int    LinkId      { get; set; }
        public long   StudentId   { get; set; }
        public string StudentName { get; set; }
        public string ClassName   { get; set; }
        public string AdmissionNo { get; set; }
        public string SchoolCode  { get; set; }
        public bool   IsActive    { get; set; }
    }
}
