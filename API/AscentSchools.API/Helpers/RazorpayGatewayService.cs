using AscentSchools.Core.DTOs.School.Fee;
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

        public ExternalOrderResult CreateExternalOrder(GatewayCredentials creds, GatewayOrderContext ctx)
        {
            try
            {
                var amountInPaise = (long)(ctx.Amount * 100);

                var payload = new
                {
                    amount          = amountInPaise,
                    currency        = "INR",
                    receipt         = ctx.ReceiptRef,
                    payment_capture = 1,
                    notes           = ctx.Notes ?? new Dictionary<string, string>()
                };

                var json    = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{creds.KeyId}:{creds.KeySecret}"));
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

        public OrderPaymentStatus FetchOrderPayment(GatewayCredentials creds, string externalOrderId)
        {
            try
            {
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{creds.KeyId}:{creds.KeySecret}"));
                var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"orders/{externalOrderId}/payments");
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

                var response = _http.SendAsync(httpRequest).GetAwaiter().GetResult();
                var body     = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (!response.IsSuccessStatusCode)
                    return new OrderPaymentStatus { Success = false, Error = $"Razorpay API error ({(int)response.StatusCode}): {body}" };

                var items = JObject.Parse(body)["items"] as JArray;
                if (items == null || items.Count == 0)
                    return new OrderPaymentStatus { Success = true, Found = false };

                // Prefer a captured payment; otherwise take the most recent attempt
                // (surfaces authorized / failed so the UI can show why it isn't reconcilable).
                JObject chosen = null;
                foreach (var it in items)
                {
                    if ((string)it["status"] == "captured") { chosen = (JObject)it; break; }
                }
                if (chosen == null) chosen = (JObject)items[items.Count - 1];

                var amountPaise = chosen["amount"]?.Value<long>() ?? 0;
                return new OrderPaymentStatus
                {
                    Success        = true,
                    Found          = true,
                    Captured       = (string)chosen["status"] == "captured",
                    PaymentId      = chosen["id"]?.ToString(),
                    Status         = chosen["status"]?.ToString(),
                    AmountInRupees = amountPaise / 100m,
                    Method         = chosen["method"]?.ToString(),
                };
            }
            catch (Exception ex)
            {
                return new OrderPaymentStatus { Success = false, Error = ex.Message };
            }
        }

        public PaymentVerification VerifyPayment(
            GatewayCredentials creds, string externalOrderId, decimal expectedAmount,
            string paymentId, string signature, IDictionary<string, string> gatewayResponse)
        {
            if (string.IsNullOrWhiteSpace(paymentId) || string.IsNullOrWhiteSpace(signature))
                return new PaymentVerification { Verified = false, Definitive = false, Error = "PaymentId and Signature are required." };

            // Razorpay signature: HMAC-SHA256("{orderId}|{paymentId}", keySecret)
            var expected = ComputeHmacSha256($"{externalOrderId}|{paymentId}", creds.KeySecret);
            var ok       = string.Equals(expected, signature, StringComparison.OrdinalIgnoreCase);
            return new PaymentVerification
            {
                Verified   = ok,
                Definitive = !ok,
                PaymentId  = paymentId,
                Error      = ok ? null : "Payment signature verification failed.",
            };
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
