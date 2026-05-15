namespace AscentSchools.Core.DTOs.School.Master
{
    public class FeePeriodDto
    {
        public int    FeePeriodId    { get; set; }
        public int    SchoolId       { get; set; }
        public int?   AcademicYearId { get; set; }
        public int    MonthNo        { get; set; }
        public int    YearNo         { get; set; }
        public string PeriodLabel    { get; set; }
        public int?   SequenceNo     { get; set; }
        public string Status         { get; set; }
    }

    public class SaveFeePeriodRequest
    {
        public int?   AcademicYearId { get; set; }
        public int    MonthNo        { get; set; }
        public int    YearNo         { get; set; }
        public string PeriodLabel    { get; set; }
        public int?   SequenceNo     { get; set; }
        public string Status         { get; set; }
    }
}
