namespace AscentSchools.Core.DTOs.School.Master
{
    public class SectionDto
    {
        public int    SectionId   { get; set; }
        public int    ClassId     { get; set; }
        public string SectionName { get; set; }
        public string Description { get; set; }
        public int?   SequenceNo  { get; set; }
        public string Status      { get; set; }
        public int    SchoolId    { get; set; }
    }

    public class SaveSectionRequest
    {
        public int    ClassId     { get; set; }
        public string SectionName { get; set; }
        public string Description { get; set; }
        public int?   SequenceNo  { get; set; }
        public string Status      { get; set; }
    }
}
