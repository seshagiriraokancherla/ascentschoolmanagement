using System;

namespace AscentMigration.Models.Legacy
{
    public class LegacyBusMaster
    {
        public string    BusMasterID  { get; set; }
        public string    RouteID      { get; set; }
        public string    PaymentTyp   { get; set; }
        public string    TermDet      { get; set; }
        public decimal?  Amt          { get; set; }
        public string    BusNam       { get; set; }
        public string    AcdmYear     { get; set; }
        public string    CategoryNam  { get; set; }
        public string    TraStatus    { get; set; }
        public string    CrtBy        { get; set; }
        public DateTime? CrtDate      { get; set; }
        public string    BranchID     { get; set; }
        public string    MachID       { get; set; }
        public string    DeleteBy     { get; set; }
    }
}
