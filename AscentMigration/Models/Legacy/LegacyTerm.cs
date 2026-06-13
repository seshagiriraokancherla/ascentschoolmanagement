using System;

namespace AscentMigration.Models.Legacy
{
    public class LegacyTerm
    {
        public string    TrmID       { get; set; }
        public string    TrmMnthData { get; set; }
        public string    YearNam     { get; set; }
        public int?      OrdrNo      { get; set; }
        public string    Descrpt     { get; set; }
        public string    AcdYear     { get; set; }
        public string    FeeCategory { get; set; }
        public string    TraStatus   { get; set; }
        public string    CrtBy       { get; set; }
        public DateTime? CrtDat      { get; set; }
        public string    BranchID    { get; set; }
        public string    MachID      { get; set; }
        public string    DeletBy     { get; set; }
    }
}
