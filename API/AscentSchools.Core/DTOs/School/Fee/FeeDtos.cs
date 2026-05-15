using System;
using System.Collections.Generic;

namespace AscentSchools.Core.DTOs.School.Fee
{
    // ── Fee Structure ─────────────────────────────────────────────────────

    public class FeeStructureDto
    {
        public int      FeeStructureId    { get; set; }
        public int?     FeeTypeId         { get; set; }
        public string   FeeTypeName       { get; set; }
        public int?     SequenceNo        { get; set; }
        public int?     TermId            { get; set; }
        public string   TermName          { get; set; }
        public int?     OrderNo           { get; set; }
        public int?     FeePeriodId       { get; set; }
        public string   PeriodLabel       { get; set; }
        public int?     PeriodSequenceNo  { get; set; }
        public string   PaymentType       { get; set; }   // Term / Monthly
        public string   AdmissionType     { get; set; }   // Old / New / null
        public decimal? Amount            { get; set; }
    }

    public class SaveFeeStructureRequest
    {
        public int?   ClassId        { get; set; }
        public int?   FeeCategoryId  { get; set; }
        public int?   AcademicYearId { get; set; }
        public string AdmissionType  { get; set; }   // Old / New / null
        public string PaymentType    { get; set; }   // Term / Monthly
        public List<FeeStructureItem> Items { get; set; }
    }

    public class FeeStructureItem
    {
        public int?    FeeTypeId    { get; set; }
        public int?    TermId       { get; set; }
        public int?    FeePeriodId  { get; set; }
        public string  PaymentType  { get; set; }
        public string  AdmissionType { get; set; }
        public decimal Amount        { get; set; }
    }

    // ── Student Fee Summary (dues) ─────────────────────────────────────────

    public class StudentFeeSummaryDto
    {
        public long   StudentId       { get; set; }
        public string StudentName     { get; set; }
        public string AdmissionNo     { get; set; }
        public string ClassName       { get; set; }
        public int?   ClassId         { get; set; }
        public string FeeCategoryName { get; set; }
        public int?   FeeCategoryId   { get; set; }
        public int?   AcademicYearId  { get; set; }
        public string AcademicYear    { get; set; }
        public List<FeeLineItemDto> LineItems { get; set; }
    }

    public class FeeLineItemDto
    {
        public int?    FeeTypeId       { get; set; }
        public string  FeeTypeName     { get; set; }
        public int?    SequenceNo      { get; set; }
        public int?    TermId          { get; set; }
        public string  TermName        { get; set; }
        public int?    OrderNo         { get; set; }
        public int?    FeePeriodId     { get; set; }
        public string  PeriodLabel     { get; set; }
        public string  PaymentType     { get; set; }
        public int?    BusRouteId      { get; set; }  // transport items only
        public int?    HostelId        { get; set; }  // hostel items only
        public decimal StructureAmount   { get; set; }
        public decimal PaidAmount        { get; set; }
        public decimal ConcessionAmount  { get; set; }
        public decimal Outstanding       { get; set; }
    }

    // ── Cross-year fee summary (by student_unique_id) ─────────────────────

    public class CrossYearFeeSummaryDto
    {
        public int    StudentUniqueId { get; set; }
        public string StudentName     { get; set; }
        public string AdmissionNo     { get; set; }
        public string JoinType        { get; set; }
        public List<YearFeeSummaryDto> Years { get; set; }
    }

    public class YearFeeSummaryDto
    {
        public long   StudentId          { get; set; }
        public int    AcademicYearId     { get; set; }
        public string AcademicYear       { get; set; }
        public string ClassName          { get; set; }
        public string FeeCategoryName    { get; set; }
        public string ClassGroupPrefix   { get; set; }
        public bool   IsCurrent          { get; set; }
        public decimal TotalStructure    { get; set; }
        public decimal TotalPaid         { get; set; }
        public decimal TotalOutstanding  { get; set; }
        public List<FeeLineItemDto> LineItems { get; set; }
    }

    // ── Collect Fee ────────────────────────────────────────────────────────

    public class CollectFeeRequest
    {
        public long      StudentId        { get; set; }
        public int?      StudentUniqueId  { get; set; }
        public string    FeeTypeCategory  { get; set; }  // Admission/School/Transport/Hostel/Other
        public int?      AcademicYearId   { get; set; }
        public int?      PaymentModeId    { get; set; }
        public DateTime? PaymentDate      { get; set; }
        public string    ChequeNo         { get; set; }
        public DateTime? ChequeDate       { get; set; }
        public string    BankName         { get; set; }
        public string    Remarks          { get; set; }
        public List<CollectFeeItem> Items { get; set; }
    }

    public class CollectFeeItem
    {
        public int?    FeeTypeId        { get; set; }
        public int?    TermId           { get; set; }
        public int?    FeePeriodId      { get; set; }
        public int?    BusRouteId       { get; set; }  // transport items only
        public int?    HostelId         { get; set; }  // hostel items only
        public decimal Amount           { get; set; }
        public decimal ConcessionAmount { get; set; }
    }

    // ── Receipt ───────────────────────────────────────────────────────────

    public class FeeReceiptListDto
    {
        public int      ReceiptId       { get; set; }
        public string   ReceiptNo       { get; set; }
        public long     StudentId       { get; set; }
        public string   StudentName     { get; set; }
        public string   AdmissionNo     { get; set; }
        public string   ClassName       { get; set; }
        public DateTime PaymentDate     { get; set; }
        public decimal  TotalAmount     { get; set; }
        public string   PaymentModeName { get; set; }
        public string   Status          { get; set; }
        public int      SchoolId        { get; set; }
    }

    public class FeeReceiptDto
    {
        public int      ReceiptId        { get; set; }
        public string   ReceiptNo        { get; set; }
        public long     StudentId        { get; set; }
        public string   StudentName      { get; set; }
        public string   AdmissionNo      { get; set; }
        public string   ClassName        { get; set; }
        public string   FatherName       { get; set; }
        public string   AcademicYear     { get; set; }
        public DateTime PaymentDate      { get; set; }
        public decimal  TotalAmount      { get; set; }
        public string   PaymentModeName  { get; set; }
        public string   ChequeNo         { get; set; }
        public DateTime? ChequeDate      { get; set; }
        public string   BankName         { get; set; }
        public string   Status           { get; set; }
        public string   CancelReason     { get; set; }
        public string   Remarks          { get; set; }
        public string   CreatedBy        { get; set; }
        public DateTime CreatedAt        { get; set; }
        public int      SchoolId         { get; set; }
        public List<FeeReceiptItemDto> Items { get; set; }
    }

    public class FeeReceiptItemDto
    {
        public int     ItemId           { get; set; }
        public string  FeeTypeName      { get; set; }  // null for transport/hostel items
        public string  RouteName        { get; set; }  // populated for transport receipt items
        public string  HostelName       { get; set; }  // populated for hostel receipt items
        public string  TermName         { get; set; }
        public decimal Amount           { get; set; }
        public decimal ConcessionAmount { get; set; }
        public decimal NetAmount        { get; set; }
    }

    public class CancelReceiptRequest
    {
        public string CancelReason { get; set; }
    }
}
