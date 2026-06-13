using System;

namespace AscentMigration.Models.Legacy
{
    public class LegacyFeeMaster
    {
        public string    FeeMasterID   { get; set; }
        public string    FeeCategory   { get; set; }
        public string    ClassNam      { get; set; }
        public string    FeeTypID      { get; set; }
        public string    FeeTyp        { get; set; }
        public string    NofPayments   { get; set; }
        public decimal?  Amt           { get; set; }
        public string    Descrpt       { get; set; }
        public string    FeeMasterStat { get; set; }
        public string    AcdYear       { get; set; }
        public string    CrtBy         { get; set; }
        public DateTime? CrtDat        { get; set; }
        public string    BranchID      { get; set; }
        public string    MachID        { get; set; }
        public string    PymntTyp      { get; set; }
        public string    AdmsnTyp      { get; set; }
        public string    DelbyData     { get; set; }
    }
}
