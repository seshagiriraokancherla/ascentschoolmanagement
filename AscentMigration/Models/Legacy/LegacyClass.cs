using System;

namespace AscentMigration.Models.Legacy
{
    public class LegacyClass
    {
        public string    ClassID     { get; set; }
        public string    ClassNam    { get; set; }
        public string    BranchNam   { get; set; }
        public string    ClassDesc   { get; set; }
        public string    ClassStat   { get; set; }
        public string    CrtBy       { get; set; }
        public DateTime? CrtDat      { get; set; }
        public string    BranchID    { get; set; }
        public string    MachID      { get; set; }
        public string    CategoryTyp { get; set; }
        public string    PrefixStat  { get; set; }
        public string    PrefixCod   { get; set; }
        public int?      SeqN        { get; set; }
    }
}
