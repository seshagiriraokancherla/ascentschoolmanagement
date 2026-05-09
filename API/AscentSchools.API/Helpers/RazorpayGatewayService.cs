using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace AscentSchools.API.Helpers
{
    /// <summary>
    /// Razorpay gateway implementation.
    /// Uses Razorpay REST API directly (no SDK dependency) for portability.
    /// Docs: https://razorpay.com/docs/payments/server-integration/
    /// </summary>
    public class RazorpayGatewayService : IGatewayService
    {
        // Static HttpClient — best practice for .NET (avoids socket exhaustion)
        private static readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("https://api.razorpay.com/v1/")
        };

        public string GatewayName => "Razorpay";

        public ExternalOrderResult CreateExternalOrder(
            string keyId,
            string keySecret,
            decimal amount,
            string receiptRef,
            Dictionary<string, string> notes = null)
        {
            try
            {
                var amountInPaise = (long)(amount * 100);

                var payload = new
                {
                    amount          = amountInPaise,
                    currency        = "INR",
                    receipt         = receiptRef,
                    payment_capture = 1,
                    notes           = notes ?? new Dictionary<string, string>()
                };

                var json    = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{keyId}:{keySecret}"));
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "orders")
                {
                    Content = content
                };
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

                // Synchronous call — Web API runs on thread pool, no deadlock risk
                var response = _http.SendAsync(httpRequest).GetAwaiter().GetResult();
                var body     = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (!response.IsSuccessStatusCode)
                    return new ExternalOrderResult { Success = false, Error = $"Razorpay API error ({(int)response.StatusCode}): {body}" };

                var obj = JObject.Parse(body);
                var orderId = obj["id"]?.ToString();

                if (string.IsNullOrWhiteSpace(orderId))
                    return new ExternalOrderResult { Success = false, Error = "Razorpay returned no order ID." };

                return new ExternalOrderResult { Success = true, ExternalOrderId = orderId };
            }
            catch (Exception ex)
            {
                return new ExternalOrderResult { Success = false, Error = ex.Message };
            }
        }

        public bool VerifyPaymentSignature(string orderId, string paymentId, string signature, string keySecret)
        {
            // Razorpay signature: HMAC-SHA256("{orderId}|{paymentId}", keySecret)
            var message  = $"{orderId}|{paymentId}";
            var expected = ComputeHmacSha256(message, keySecret);
            return string.Equals(expected, signature, StringComparison.OrdinalIgnoreCase);
        }

        public bool VerifyWebhookSignature(string payload, string signature, string webhookSecret)
        {
            // Razorpay webhook: HMAC-SHA256(rawBody, webhookSecret)
            var expected = ComputeHmacSha256(payload, webhookSecret);
            return string.Equals(expected, signature, StringComparison.OrdinalIgnoreCase);
        }

        private static string ComputeHmacSha256(string message, string secret)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secret);
            var msgBytes = Encoding.UTF8.GetBytes(message);
            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hash = hmac.ComputeHash(msgBytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }
    }
}
