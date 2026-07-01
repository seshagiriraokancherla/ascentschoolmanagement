using System;
using System.Collections.Generic;

namespace AscentSchools.Core.DTOs.School.Fee
{
    /// <summary>
    /// Returned to the frontend — contains only key_id (secret is NEVER exposed).
    /// </summary>
    public class GatewayConfigDto
    {
        public int    GatewayConfigId { get; set; }
        public string GatewayName     { get; set; }
        public string DisplayName     { get; set; }
        public string KeyId           { get; set; }
        public bool   IsActive        { get; set; }
    }

    /// <summary>
    /// Returned to the settings page. KeySecret is intentionally omitted from GET —
    /// the UI shows a placeholder "••••••" and only sends a new value if the field is edited.
    /// WebhookSecret is also masked; UI similarly sends new value only when changed.
    /// </summary>
    public class GatewayConfigSettingsDto : GatewayConfigDto
    {
        public bool HasWebhookSecret { get; set; }   // true if webhook_secret is stored; value not returned
    }

    public class SaveGatewayConfigRequest
    {
        public string GatewayName    { get; set; }
        public string DisplayName    { get; set; }
        public string KeyId          { get; set; }
        public string KeySecret      { get; set; }      // blank = keep existing
        public string WebhookSecret  { get; set; }      // blank = keep existing
        public bool?  IsActive       { get; set; }
    }

    /// <summary>Request body for POST /mobile/fees/payment-orders (parent portal). Student identity comes from JWT.</summary>
    public class MobileParentOrderRequest
    {
        public int    AcademicYearId  { get; set; }
        public string FeeTypeCategory { get; set; }
        public List<CollectFeeItem> Items { get; set; }
    }

    /// <summary>Request body for POST /school/fees/payment-orders.</summary>
    public class CreatePaymentOrderRequest
    {
        public long      StudentId        { get; set; }
        public int?      StudentUniqueId  { get; set; }
        public string    FeeTypeCategory  { get; set; }
        public int       AcademicYearId   { get; set; }
        public int       PaymentModeId    { get; set; }
        public DateTime? PaymentDate      { get; set; }
        public string    Remarks          { get; set; }
        public List<CollectFeeItem> Items { get; set; }
    }

    /// <summary>
    /// Returned after a gateway order is created.
    /// Frontend uses ExternalOrderId + KeyId to open the Razorpay checkout.
    /// </summary>
    public class CreatePaymentOrderResponse
    {
        public int    GatewayOrderId  { get; set; }     // internal DB id for verify endpoint
        public string ExternalOrderId { get; set; }     // Razorpay order_id
        public string KeyId           { get; set; }
        public long   AmountInPaise   { get; set; }     // amount × 100 — Razorpay uses paise
        public string Currency        { get; set; }
        public string GatewayName     { get; set; }
    }

    /// <summary>
    /// Sent by the frontend after Razorpay checkout success callback.
    /// Contains the three fields Razorpay provides in handler(response).
    /// </summary>
    public class VerifyPaymentRequest
    {
        public string PaymentId { get; set; }   // razorpay_payment_id
        public string OrderId   { get; set; }   // razorpay_order_id
        public string Signature { get; set; }   // razorpay_signature
    }

    /// <summary>
    /// One row in the admin "Pending Online Payments" reconciliation list.
    /// Live gateway status is fetched separately per row (Check status button).
    /// </summary>
    public class PendingPaymentOrderDto
    {
        public int     GatewayOrderId  { get; set; }
        public string  ExternalOrderId { get; set; }
        public decimal Amount          { get; set; }
        public int     AcademicYearId  { get; set; }
        public string  FeeTypeCategory { get; set; }   // parsed from the stored payload
        public string  CreatedBy       { get; set; }
        public string  CreatedAt       { get; set; }   // preformatted "yyyy-MM-dd HH:mm" (UTC)
        public long    StudentId       { get; set; }
        public string  StudentName     { get; set; }
        public string  AdmissionNo     { get; set; }
    }

    /// <summary>
    /// A pending order with its live gateway status already attached — returned by the
    /// bulk "Scan" so the UI can show only captured/reconcilable rows without per-row clicks.
    /// </summary>
    public class ScannedPaymentOrderDto : PendingPaymentOrderDto
    {
        public bool    Found         { get; set; }
        public string  GatewayStatus { get; set; }   // captured / authorized / failed / created
        public string  PaymentId     { get; set; }
        public string  Method        { get; set; }
        public decimal GatewayAmount { get; set; }   // amount at the gateway (rupees)
        public bool    Reconcilable  { get; set; }
        public string  Note          { get; set; }
    }

    /// <summary>
    /// Live status of a pending order fetched from the gateway (Check status button).
    /// </summary>
    public class PaymentOrderStatusDto
    {
        public int     GatewayOrderId { get; set; }
        public string  OrderStatus    { get; set; }   // our DB status (Pending/Paid/Failed)
        public bool    Found          { get; set; }   // a payment attempt exists at the gateway
        public string  GatewayStatus  { get; set; }   // captured / authorized / failed / created
        public string  PaymentId      { get; set; }
        public string  Method         { get; set; }   // upi / card / netbanking …
        public decimal Amount         { get; set; }   // amount at the gateway (rupees)
        public bool    Reconcilable   { get; set; }   // captured, amount matches, not yet Paid
        public string  Note           { get; set; }   // human-readable explanation for the UI
    }
}
