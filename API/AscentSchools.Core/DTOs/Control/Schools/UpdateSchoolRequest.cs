namespace AscentSchools.Core.DTOs.Control.Schools
{
    public class UpdateSchoolRequest
    {
        public string SchoolName    { get; set; }
        public string SchoolCaption { get; set; }
        public string Address       { get; set; }
        public string City          { get; set; }
        public string State         { get; set; }
        public string Mobile        { get; set; }
        public string Email         { get; set; }
        public string Status        { get; set; }
    }
}
