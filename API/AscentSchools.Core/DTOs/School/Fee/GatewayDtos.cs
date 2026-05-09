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

    /// <summary>Request body for POST /school/fees/payment-orders.</summary>
    public class CreatePaymentOrderRequest
    {
        public long      StudentId       { get; set; }
        public int       AcademicYearId  { get; set; }
        public int       PaymentModeId   { get; set; }
        public DateTime? PaymentDate     { get; set; }
        public string    Remarks         { get; set; }
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
}
