namespace AscentSchools.Core.DTOs.Branding
{
    /// <summary>
    /// Returned by public GET /branding endpoint before login.
    /// React uses this to apply school colors and logo via AntD ConfigProvider.
    /// </summary>
    public class BrandingResponse
    {
        public string DisplayName        { get; set; }
        public string Tagline            { get; set; }
        public string LogoPath           { get; set; }
        public string FaviconPath        { get; set; }
        public string PrimaryColor       { get; set; }
        public string SecondaryColor     { get; set; }
        public string HeaderBgColor      { get; set; }
        public string NavTextColor       { get; set; }
        public string LoginBgPath        { get; set; }
        public string ReceiptFooterText  { get; set; }
    }
}
