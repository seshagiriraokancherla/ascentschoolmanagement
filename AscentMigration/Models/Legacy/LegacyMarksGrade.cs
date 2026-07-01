using System;

namespace AscentMigration.Models.Legacy
{
    public class LegacyMarksGrade
    {
        public string    MrksGradId { get; set; }
        public string    GradName   { get; set; }
        public string    SubjID     { get; set; }
        public double?   MaxMrks    { get; set; }
        public double?   MinMrks    { get; set; }
        public string    Grad       { get; set; }
        public string    Rrmrks     { get; set; }
        public string    TraStatus  { get; set; }
        public DateTime? CrtDat     { get; set; }
        public string    CrtBy      { get; set; }
        public string    BranchID   { get; set; }
        public string    MachID     { get; set; }
        public string    DeletBy    { get; set; }
    }
}
