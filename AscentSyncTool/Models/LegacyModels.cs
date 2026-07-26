using System;

namespace AscentSyncTool.Models
{
    // ── Legacy SAS_FeeReceipts (subset used for export + display) ──────────────
    public class LegacyFeeReceipt
    {
        public string    FeeReciptID    { get; set; }
        public DateTime? FeeDat         { get; set; }
        public string    StuAdmnNo      { get; set; }
        public string    AcdYear        { get; set; }
        public string    ClassNam       { get; set; }   // class name (from receipts API)
        public string    FeeTyp         { get; set; }
        public string    PaymentTyp     { get; set; }   // term name
        public decimal?  FeeAmt         { get; set; }
        public string    PymntTyp       { get; set; }   // payment mode
        public string    RefNo          { get; set; }   // cheque no
        public string    RemarksDet     { get; set; }
        public string    FeeReceiptStat { get; set; }   // A / D
        public string    DeletResn      { get; set; }
    }

    // ── Legacy SAS_StudentMaster (subset used for export + display) ────────────
    public class LegacyStudent
    {
        public string    StuAmnNo       { get; set; }
        public string    StuName        { get; set; }
        public string    StuGender      { get; set; }
        public DateTime? StuDOB         { get; set; }
        public string    StuAcdYear     { get; set; }
        public string    StuClassNam    { get; set; }
        public string    StuSect        { get; set; }
        public string    StuClassRolNo  { get; set; }
        public string    StuFatherName  { get; set; }
        public string    StuMotherName  { get; set; }
        public string    StuMobile1     { get; set; }
        public string    StuMobile2     { get; set; }
        public string    StuAdarNo      { get; set; }
        public string    StuCaste       { get; set; }
        public string    StuCasteCode   { get; set; }
        public string    StuReligion    { get; set; }
        public string    StuStartClass  { get; set; }
        public string    MothrLang      { get; set; }
        public string    StuStatus      { get; set; }   // A / D
        public double?   StuUnqID       { get; set; }   // stable cross-year id (float in legacy) → student_unique_id
        // Extended fields (→ new-DB columns via the bulk API)
        public string    StuJoinTyp        { get; set; }
        public string    StuFatherOccup    { get; set; }
        public string    StuFatherEmpTyp   { get; set; }
        public string    StuMotherOccup    { get; set; }
        public DateTime? StuDOJ            { get; set; }
        public string    StuNationlity     { get; set; }
        public string    StuDoorNo         { get; set; }
        public string    StuAddrArea       { get; set; }
        public string    StuAddrCity       { get; set; }
        public string    StuAddrState      { get; set; }
        public string    StuAddrPerminent  { get; set; }
        public string    StuMailID         { get; set; }
        public decimal?  StuAnnualIncome   { get; set; }
        public string    StuFamilyChildres { get; set; }
        public string    StuDOBStat        { get; set; }
        public string    StuCasteStat      { get; set; }
        public string    StuTransportTyp   { get; set; }
        public DateTime? AdminDate         { get; set; }
        public string    StudentType       { get; set; }
        public string    BloodGrp          { get; set; }
        public string    JoinTerm          { get; set; }
        public string    StuFirstLang      { get; set; }
        public string    StuThirdLang      { get; set; }
        public string    StuUdiseNo        { get; set; }
        public DateTime? CrtDat         { get; set; }
    }
}
