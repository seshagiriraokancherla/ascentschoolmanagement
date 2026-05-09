namespace AscentSchools.Core.DTOs.School.Master
{
    public class AcademicYearDto
    {
        public int    AcademicYearId            { get; set; }
        public string AcademicYear              { get; set; }
        public string StartMonth                { get; set; }
        public string EndMonth                  { get; set; }
        public string Status                    { get; set; }
        public string RegistrationFeeFrequency  { get; set; }
        public string TransportFeeFrequency     { get; set; }
        public string HostelFeeFrequency        { get; set; }
        public string BoardingType              { get; set; }
        public string NewAdmissionsEnabled      { get; set; }
        public int    SchoolId                  { get; set; }
    }

    public class SaveAcademicYearRequest
    {
        public string AcademicYear              { get; set; }
        public string StartMonth                { get; set; }
        public string EndMonth                  { get; set; }
        public string Status                    { get; set; }
        public string RegistrationFeeFrequency  { get; set; }
        public string TransportFeeFrequency     { get; set; }
        public string HostelFeeFrequency        { get; set; }
        public string BoardingType              { get; set; }
        public string NewAdmissionsEnabled      { get; set; }
    }
}
