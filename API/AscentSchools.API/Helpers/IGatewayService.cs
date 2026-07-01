using System.Collections.Generic;

namespace AscentSchools.API.Helpers
{
    public class ExternalOrderResult
    {
        public bool   Success         { get; set; }
        public string ExternalOrderId { get; set; }
        public string Error           { get; set; }
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
        public string  PaymentId      { get; set; }   // gateway payment id (e.g. pay_xxx)
        public string  Status         { get; set; }   // captured / authorized / failed / created
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

        /// <summary>
        /// Creates an order/session on the gateway.
        /// notes: key-value pairs embedded in the gateway order for webhook identification
        ///        (use to store group_id, school_id so webhooks can locate the tenant).
        /// </summary>
        ExternalOrderResult CreateExternalOrder(
            string keyId,
            string keySecret,
            decimal amount,
            string receiptRef,
            Dictionary<string, string> notes = null);

        /// <summary>
        /// Verifies the payment signature returned by the gateway JS callback.
        /// Must be called server-side — never trust the JS callback alone.
        /// </summary>
        bool VerifyPaymentSignature(string orderId, string paymentId, string signature, string keySecret);

        /// <summary>
        /// Verifies the HMAC signature on incoming webhook payloads.
        /// </summary>
        bool VerifyWebhookSignature(string payload, string signature, string webhookSecret);

        /// <summary>
        /// Fetches the payment(s) for a previously created order directly from the gateway,
        /// so a server can reconcile an order whose JS callback / webhook never completed.
        /// Uses the same API key/secret as order creation — no dashboard access needed.
        /// </summary>
        OrderPaymentStatus FetchOrderPayment(string keyId, string keySecret, string externalOrderId);
    }
}
