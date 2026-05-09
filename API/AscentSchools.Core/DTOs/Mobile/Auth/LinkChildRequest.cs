namespace AscentSchools.Core.DTOs.Mobile.Auth
{
    public class LinkChildRequest
    {
        /// <summary>Admission number of the child to link.</summary>
        public string AdmissionNo { get; set; }
        /// <summary>School code / subdomain (e.g. "test") sent as X-Subdomain header or body field.</summary>
        public string SchoolCode  { get; set; }
    }
}
