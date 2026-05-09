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
    }
}
