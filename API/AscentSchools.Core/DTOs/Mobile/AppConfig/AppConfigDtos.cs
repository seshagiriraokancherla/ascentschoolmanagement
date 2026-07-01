namespace AscentSchools.Core.DTOs.Mobile.AppConfig
{
    /// <summary>Raw app_config row (master DB) for one applicationId + platform.</summary>
    public class AppConfigDto
    {
        public string ApplicationId            { get; set; }
        public string Platform                 { get; set; }
        public int    MinSupportedVersionCode  { get; set; }
        public int    LatestVersionCode        { get; set; }
        public string UpdateMessage            { get; set; }
        public string StoreUrl                 { get; set; }
        /// <summary>When false, the app must NOT auto-prompt for updates on launch
        /// (the server returns UpdateRequired=UpdateAvailable=false regardless of version).</summary>
        public bool   AutoUpdateEnabled        { get; set; }
    }

    /// <summary>
    /// Response for GET /mobile/app/config. The server computes the two booleans
    /// from the version the app reports, so the client just reacts:
    ///   updateRequired  -> hard block (full-screen, non-dismissible)
    ///   updateAvailable -> soft nudge (dismissible)
    /// </summary>
    public class AppVersionStatusDto
    {
        public bool   UpdateRequired           { get; set; }
        public bool   UpdateAvailable          { get; set; }
        public int    MinSupportedVersionCode  { get; set; }
        public int    LatestVersionCode        { get; set; }
        public string Message                  { get; set; }
        public string StoreUrl                 { get; set; }
    }
}
