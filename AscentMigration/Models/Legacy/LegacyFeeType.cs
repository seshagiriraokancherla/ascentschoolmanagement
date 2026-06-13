using System;

namespace AscentMigration.Models.Legacy
{
    public class LegacyFeeType
    {
        public string    FeeTypID     { get; set; }
        public string    FeeTyp       { get; set; }
        public string    AcdYear      { get; set; }
        public string    NofPayment   { get; set; }
        public int?      SeqNo        { get; set; }
        public string    FeeDesc      { get; set; }
        public string    FeeTypStatus { get; set; }
        public string    CrtBy        { get; set; }
        public DateTime? CrtDat       { get; set; }
        public string    BranchID     { get; set; }
        public string    MachID       { get; set; }
    }
}
