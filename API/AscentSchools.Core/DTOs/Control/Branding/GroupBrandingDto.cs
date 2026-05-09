namespace AscentSchools.Core.DTOs.Control.Branding
{
    public class GroupBrandingDto
    {
        public int    GroupId            { get; set; }
        public string LogoPath          { get; set; }
        public string FaviconPath       { get; set; }
        public string PrimaryColor      { get; set; }
        public string SecondaryColor    { get; set; }
        public string HeaderBgColor     { get; set; }
        public string NavTextColor      { get; set; }
        public string DisplayName       { get; set; }
        public string Tagline           { get; set; }
        public string ReceiptFooterText { get; set; }
        public string LoginBgPath       { get; set; }
    }
}
