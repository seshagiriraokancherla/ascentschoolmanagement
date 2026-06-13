using System;

namespace AscentMigration.Models.Legacy
{
    public class LegacyFeeReceipt
    {
        public string    FeeReciptID     { get; set; }
        public DateTime? FeeDat          { get; set; }
        public string    RefNo           { get; set; }
        public string    StuAdmnNo       { get; set; }
        public string    FeeTyp          { get; set; }
        public decimal?  FeeAmt          { get; set; }
        public string    FeeMasterID     { get; set; }
        public string    FeeID           { get; set; }
        public string    RemarksDet      { get; set; }
        public string    PaymentTyp      { get; set; }
        public string    ClassNam        { get; set; }
        public string    AcdYear         { get; set; }
        public string    FeeReceiptStat  { get; set; }
        public string    CrtBy           { get; set; }
        public DateTime? CrtDat          { get; set; }
        public string    BranchID        { get; set; }
        public string    MachID          { get; set; }
        public string    DeletBy         { get; set; }
        public int       Id_new          { get; set; }
        public string    DeletResn       { get; set; }
        public string    PymntTyp        { get; set; }
        public DateTime? DelDate         { get; set; }
    }
}
