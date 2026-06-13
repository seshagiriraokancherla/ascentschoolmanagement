using System;

namespace AscentMigration.Models.Legacy
{
    public class LegacyAcademicYear
    {
        public string AcademicYear  { get; set; }
        public string StartMonth    { get; set; }
        public string EndMonth      { get; set; }
        public string AcdStatus     { get; set; }
        public string BranchID      { get; set; }
        public string RegFeeType    { get; set; }
        public string TransFeeTyp   { get; set; }
        public string NewAdminsStat { get; set; }
        public DateTime? AgeAsOnDat { get; set; }
    }
}
