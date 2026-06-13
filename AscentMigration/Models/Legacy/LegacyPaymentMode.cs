using System;

namespace AscentMigration.Models.Legacy
{
    public class LegacyPaymentMode
    {
        public string    TraID     { get; set; }
        public string    TraTyp    { get; set; }
        public string    TraDesc   { get; set; }
        public string    TraStatus { get; set; }
        public string    CrtBy     { get; set; }
        public DateTime? CrtDat    { get; set; }
        public string    BranchID  { get; set; }
        public string    MachID    { get; set; }
    }
}
