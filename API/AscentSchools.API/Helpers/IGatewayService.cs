using AscentSchools.Core.DTOs.School.Fee;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;

namespace AscentSchools.API.Helpers
{
    /// <summary>Shared bits used by every controller that creates gateway orders.</summary>
    public static class GatewayOrderRefs
    {
        private static int _seq;

        /// <summary>
        /// Unique order reference ≤ 40 chars, letters/digits/hyphen only. Becomes the Razorpay
        /// "receipt" and the Easebuzz txnid — Easebuzz rejects a repeated txnid, so a
        /// per-second timestamp alone is not enough.
        /// </summary>
        public static string NewReceiptRef(string prefix, int schoolId)
        {
            var seq = (Interlocked.Increment(ref _seq) & 0xFFF) % 1000;
            return $"{prefix}{schoolId}-{DateTime.UtcNow:yyyyMMddHHmmssfff}{seq:000}";
        }

        /// <summary>Short product description, e.g. "School Fee" (Easebuzz productinfo).</summary>
        public static string ProductInfo(string feeTypeCategory) =>
            string.IsNullOrWhiteSpace(feeTypeCategory) ? "School Fee" : $"{feeTypeCategory} Fee";

        /// <summary>
        /// Absolute URL of the Easebuzz return endpoint (surl/furl) on THIS API, so it works
        /// in prod (https://edu-care.in/api/...) and local dev alike.
        /// </summary>
        public static string EasebuzzReturnUrl(HttpRequestMessage request)
        {
            var root = request.GetRequestContext()?.VirtualPathRoot ?? "/";
            return request.RequestUri.GetLeftPart(UriPartial.Authority)
                 + root.TrimEnd('/') + "/school/fees/payment-return/easebuzz";
        }
    }

    // GatewayCredentials (the school's stored keys) lives in Core — GatewayDtos.cs — because
    // GatewayRepository returns it.

    /// <summary>Who is paying — required by gateways such as Easebuzz, ignored by Razorpay.</summary>
    public class GatewayCustomer
    {
        /// <summary>Used when the student has no valid email — Easebuzz requires one.</summary>
        public const string FallbackEmail = "noreply@edu-care.in";

        public string Name  { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        /// <summary>Parent name/mobile (father first, then mother), falling back to the student's name.</summary>
        public static GatewayCustomer From(PaymentCustomerDto s)
        {
            if (s == null) return new GatewayCustomer();
            return new GatewayCustomer
            {
                Name  = !string.IsNullOrWhiteSpace(s.FatherName) ? s.FatherName : s.StudentName,
                Email = s.Email,
                Phone = !string.IsNullOrWhiteSpace(s.FatherMobile) ? s.FatherMobile : s.MotherMobile,
            };
        }
    }

    /// <summary>Everything needed to create one order/transaction on a gateway.</summary>
    public class GatewayOrderContext
    {
        public decimal Amount      { get; set; }
        public string  ReceiptRef  { get; set; }   // our own reference (Razorpay "receipt"; Easebuzz txnid)
        public string  ProductInfo { get; set; }   // short description, e.g. "School Fee"

        /// <summary>
        /// Key-value pairs embedded in the gateway order so a webhook can locate the tenant
        /// (group_id, school_id). Razorpay: order notes. Easebuzz: udf1 / udf2.
        /// </summary>
        public Dictionary<string, string> Notes { get; set; }

        public GatewayCustomer Customer  { get; set; }

        /// <summary>Absolute URL the gateway posts the result to (Easebuzz surl/furl).</summary>
        public string ReturnUrl { get; set; }
    }

    public class ExternalOrderResult
    {
        public bool   Success         { get; set; }
        public string ExternalOrderId { get; set; }   // Razorpay order_id / Easebuzz txnid
        public string ClientToken     { get; set; }   // Easebuzz access_key (null for Razorpay)
        public string Error           { get; set; }
    }

    /// <summary>Outcome of verifying a client-side payment callback.</summary>
    public class PaymentVerification
    {
        public bool   Verified  { get; set; }
        public string PaymentId { get; set; }   // gateway payment id (Razorpay pay_xxx / Easebuzz easepayid)

        /// <summary>
        /// True when the gateway definitively says the payment did NOT succeed (bad signature,
        /// failure, user-cancelled) — the order is marked Failed. False with Verified=false means
        /// "can't tell yet" (still pending / gateway unreachable) — the order stays Pending so the
        /// webhook or the reconciliation page can complete it later.
        /// </summary>
        public bool   Definitive { get; set; }
        public string Error      { get; set; }
    }

    /// <summary>
    /// Live status of a gateway order's payment, fetched directly from the gateway
    /// (used for reconciling orders whose client callback / webhook never landed).
    /// </summary>
    public class OrderPaymentStatus
    {
        public bool    Success        { get; set; }   // the gateway API call itself succeeded
        public string  Error          { get; set; }   // populated when Success == false
        public bool    Found          { get; set; }   // a payment attempt exists for this order
        public bool    Captured       { get; set; }   // money actually collected (Razorpay captured / Easebuzz success)
        public string  PaymentId      { get; set; }   // gateway payment id (e.g. pay_xxx / easepayid)
        public string  Status         { get; set; }   // gateway's own status word, for display
        public decimal AmountInRupees { get; set; }   // captured/attempted amount in rupees
        public string  Method         { get; set; }   // upi / card / netbanking …
    }

    /// <summary>
    /// Abstraction layer for payment gateways.
    /// Add new gateways by implementing this interface and registering in GatewayServiceFactory.
    /// </summary>
    public interface IGatewayService
    {
        string GatewayName { get; }

        /// <summary>Creates an order/session on the gateway.</summary>
        ExternalOrderResult CreateExternalOrder(GatewayCredentials creds, GatewayOrderContext ctx);

        /// <summary>
        /// Verifies the result the client got back from the gateway checkout.
        /// Must be called server-side — never trust the client callback alone.
        /// </summary>
        PaymentVerification VerifyPayment(
            GatewayCredentials creds, string externalOrderId, decimal expectedAmount,
            string paymentId, string signature, IDictionary<string, string> gatewayResponse);

        /// <summary>Verifies the signature on an incoming webhook payload (Razorpay-style HMAC).</summary>
        bool VerifyWebhookSignature(string payload, string signature, string webhookSecret);

        /// <summary>
        /// Fetches the payment(s) for a previously created order directly from the gateway,
        /// so a server can reconcile an order whose client callback / webhook never completed.
        /// </summary>
        OrderPaymentStatus FetchOrderPayment(GatewayCredentials creds, string externalOrderId);
    }
}
