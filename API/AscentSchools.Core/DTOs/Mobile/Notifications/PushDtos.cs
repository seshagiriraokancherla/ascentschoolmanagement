namespace AscentSchools.Core.DTOs.Mobile.Notifications
{
    /// <summary>Sent by the mobile app to register / unregister its FCM token.</summary>
    public class RegisterPushTokenRequest
    {
        public string FcmToken      { get; set; }
        public string ApplicationId { get; set; }   // e.g. "in.educare.stannsasf"
        public string Platform      { get; set; }   // "android"
    }
}
