namespace AscentSchools.Core.DTOs.School.Master
{
    public class FeeTypeDto
    {
        public int    FeeTypeId      { get; set; }
        public string FeeTypeName    { get; set; }
        public int?   AcademicYearId { get; set; }
        public int?   SequenceNo     { get; set; }
        public string Description    { get; set; }
        public string Status         { get; set; }
        public int    SchoolId       { get; set; }
    }

    public class SaveFeeTypeRequest
    {
        public string FeeTypeName    { get; set; }
        public int?   AcademicYearId { get; set; }
        public int?   SequenceNo     { get; set; }
        public string Description    { get; set; }
        public string Status         { get; set; }
    }
}
