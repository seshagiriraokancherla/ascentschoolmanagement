using System.Collections.Generic;

namespace AscentSchools.Core.DTOs.Mobile.Data
{
    // ── Fee Summary ───────────────────────────────────────────────────────────

    public class MobileFeeSummaryDto
    {
        public int?    AcademicYearId     { get; set; }
        public string  AcademicYear       { get; set; }
        public decimal TotalAmount        { get; set; }
        public decimal PaidAmount         { get; set; }
        public decimal OutstandingAmount  { get; set; }
        public IEnumerable<MobileFeeLineItemDto> LineItems { get; set; }
    }

    public class MobileFeeLineItemDto
    {
        public int?    FeeTypeId   { get; set; }
        public string  FeeTypeName { get; set; }
        public int?    TermId      { get; set; }
        public string  TermName    { get; set; }
        public decimal Amount      { get; set; }
        public decimal PaidAmount  { get; set; }
        public decimal Outstanding { get; set; }
        public bool    IsPaid      { get; set; }
    }

    // ── Create Order ──────────────────────────────────────────────────────────

    public class MobileCreateOrderRequest
    {
        public int   AcademicYearId { get; set; }
        public int   PaymentModeId  { get; set; }
        public List<MobileFeeOrderItem> Items { get; set; }
    }

    public class MobileFeeOrderItem
    {
        public int     FeeTypeId        { get; set; }
        public int?    TermId           { get; set; }
        public decimal Amount           { get; set; }
        public decimal ConcessionAmount { get; set; }
    }

    public class MobileOrderResponse
    {
        public int    GatewayOrderId  { get; set; }
        public string ExternalOrderId { get; set; }
        public string KeyId           { get; set; }
        public long   AmountInPaise   { get; set; }
        public string Currency        { get; set; }
    }

    // ── Verify Payment ────────────────────────────────────────────────────────

    public class MobileVerifyRequest
    {
        public int    GatewayOrderId { get; set; }
        public string PaymentId      { get; set; }
        public string OrderId        { get; set; }
        public string Signature      { get; set; }
    }

    public class MobilePaymentResultDto
    {
        public int     ReceiptId  { get; set; }
        public string  ReceiptNo  { get; set; }
        public decimal Amount     { get; set; }
        public string  Message    { get; set; }
    }
}
