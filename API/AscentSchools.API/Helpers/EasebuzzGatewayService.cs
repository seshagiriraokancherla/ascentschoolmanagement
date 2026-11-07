using AscentSchools.Core.DTOs.School.Fee;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace AscentSchools.API.Helpers
{
    /// <summary>
    /// Easebuzz gateway implementation — direct REST calls, no SDK (same approach as Razorpay).
    /// Docs: https://docs.easebuzz.in/docs/payment-gateway/8ec545c331e6f-initiate-payment-api
    ///
    /// Credentials: KeyId = merchant Key, KeySecret = Salt, Environment = "test" | "prod".
    ///
    /// Flow: CreateExternalOrder calls Initiate Payment → access_key (ClientToken). The browser
    /// (EaseCheckout iframe) or Android SDK opens checkout with that access_key.
    ///
    /// Verification is done SERVER-TO-SERVER through the Transaction API (v2.1 retrieve), never
    /// from what the client reports: the callback / webhook / return-URL post is treated only as
    /// a trigger, and the status + amount are re-read from Easebuzz with our own Salt-signed
    /// request. This is the authoritative check and does not depend on the response's
    /// reverse-hash field order.
    /// </summary>
    public class EasebuzzGatewayService : IGatewayService
    {
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        public string GatewayName => "Easebuzz";

        private static bool   IsTest(GatewayCredentials c) => string.Equals(c?.Environment, "test", StringComparison.OrdinalIgnoreCase);
        private static string PayBase(GatewayCredentials c)  => IsTest(c) ? "https://testpay.easebuzz.in"       : "https://pay.easebuzz.in";
        private static string DashBase(GatewayCredentials c) => IsTest(c) ? "https://testdashboard.easebuzz.in" : "https://dashboard.easebuzz.in";

        // ── Initiate Payment ──────────────────────────────────────────────

        public ExternalOrderResult CreateExternalOrder(GatewayCredentials creds, GatewayOrderContext ctx)
        {
            try
            {
                var customer = ctx.Customer ?? new GatewayCustomer();

                var phone = Regex.Replace(customer.Phone ?? "", @"\D", "");
                if (phone.Length > 10) phone = phone.Substring(phone.Length - 10);
                if (phone.Length < 10)
                    return Fail("A valid parent mobile number is required for online payment. Update the student's Father/Mother mobile and try again.");

                // Easebuzz validates each field against a pattern; every value is trimmed and the
                // exact string sent is the one hashed (a stray space = "Hash mismatch").
                var txnid       = Clean(ctx.ReceiptRef, @"[^a-zA-Z0-9_\-]", 40);
                var amount      = ctx.Amount.ToString("0.00", CultureInfo.InvariantCulture);
                var productinfo = Clean(ctx.ProductInfo, @"[^a-zA-Z0-9 \-]", 45, "Fees");
                var firstname   = Clean(customer.Name, @"[^a-zA-Z0-9&\-._ ()/,@]", 150, "Parent");
                var email       = IsValidEmail(customer.Email) ? customer.Email.Trim() : GatewayCustomer.FallbackEmail;

                // udf1/udf2 carry the tenant so the webhook can find the school (udf8-10 must stay empty).
                var udf1 = NoteValue(ctx.Notes, "group_id");
                var udf2 = NoteValue(ctx.Notes, "school_id");
                string udf3 = "", udf4 = "", udf5 = "", udf6 = "", udf7 = "", udf8 = "", udf9 = "", udf10 = "";

                // key|txnid|amount|productinfo|firstname|email|udf1|...|udf10|salt
                var hash = Sha512(string.Join("|", creds.KeyId, txnid, amount, productinfo, firstname, email,
                    udf1, udf2, udf3, udf4, udf5, udf6, udf7, udf8, udf9, udf10, creds.KeySecret));

                var form = new Dictionary<string, string>
                {
                    { "key",         creds.KeyId },
                    { "txnid",       txnid },
                    { "amount",      amount },
                    { "productinfo", productinfo },
                    { "firstname",   firstname },
                    { "phone",       phone },
                    { "email",       email },
                    { "surl",        ctx.ReturnUrl },
                    { "furl",        ctx.ReturnUrl },
                    { "udf1", udf1 }, { "udf2", udf2 }, { "udf3", udf3 }, { "udf4", udf4 }, { "udf5", udf5 },
                    { "udf6", udf6 }, { "udf7", udf7 }, { "udf8", udf8 }, { "udf9", udf9 }, { "udf10", udf10 },
                    { "hash",        hash },
                };

                var body = PostForm(PayBase(creds) + "/payment/initiateLink", form);
                var obj  = JObject.Parse(body);

                if (obj["status"]?.ToString() != "1")
                {
                    var err = obj["error_desc"]?.ToString();
                    var data = obj["data"]?.ToString();
                    return Fail($"Easebuzz: {err ?? data ?? body}"
                        + (err != null && err.IndexOf("merchant key", StringComparison.OrdinalIgnoreCase) >= 0
                            ? " (check the Key and that Environment matches it — test keys only work in Test, live keys only in Live)."
                            : ""));
                }

                var accessKey = obj["data"]?.ToString();
                if (string.IsNullOrWhiteSpace(accessKey))
                    return Fail("Easebuzz returned no access key.");

                return new ExternalOrderResult { Success = true, ExternalOrderId = txnid, ClientToken = accessKey };
            }
            catch (Exception ex)
            {
                return Fail(ex.Message);
            }
        }

        // ── Transaction API (v2.1 retrieve) ───────────────────────────────

        public OrderPaymentStatus FetchOrderPayment(GatewayCredentials creds, string externalOrderId)
        {
            try
            {
                var form = new Dictionary<string, string>
                {
                    { "key",   creds.KeyId },
                    { "txnid", externalOrderId },
                    { "hash",  Sha512($"{creds.KeyId}|{externalOrderId}|{creds.KeySecret}") },   // key|txnid|salt
                };

                var obj = JObject.Parse(PostForm(DashBase(creds) + "/transaction/v2.1/retrieve", form));
                var msg = obj["msg"];

                if (obj["status"]?.Value<bool>() != true)
                {
                    var text = msg?.ToString() ?? "";
                    // "Transaction not found." = the parent never got as far as a payment attempt.
                    if (text.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0)
                        return new OrderPaymentStatus { Success = true, Found = false };
                    return new OrderPaymentStatus { Success = false, Error = $"Easebuzz: {text}" };
                }

                // msg is a list of attempts for this txnid — prefer a successful one, else the latest.
                var attempts = (msg as JArray)?.OfType<JObject>().ToList()
                               ?? (msg is JObject single ? new List<JObject> { single } : new List<JObject>());
                if (attempts.Count == 0)
                    return new OrderPaymentStatus { Success = true, Found = false };

                var chosen = attempts.FirstOrDefault(a => IsSuccess((string)a["status"])) ?? attempts.Last();

                // Defense in depth: the record must belong to this merchant + this txnid.
                if (!string.Equals((string)chosen["txnid"], externalOrderId, StringComparison.Ordinal) ||
                    (chosen["key"] != null && !string.Equals((string)chosen["key"], creds.KeyId, StringComparison.Ordinal)))
                    return new OrderPaymentStatus { Success = false, Error = "Easebuzz returned a transaction that does not match this order." };

                decimal.TryParse((string)chosen["amount"], NumberStyles.Any, CultureInfo.InvariantCulture, out var amount);
                var status = (string)chosen["status"];

                return new OrderPaymentStatus
                {
                    Success        = true,
                    Found          = true,
                    Captured       = IsSuccess(status),
                    PaymentId      = (string)chosen["easepayid"],
                    Status         = status,
                    AmountInRupees = amount,
                    Method         = (string)chosen["mode"] ?? (string)chosen["card_type"],
                };
            }
            catch (Exception ex)
            {
                return new OrderPaymentStatus { Success = false, Error = ex.Message };
            }
        }

        // ── Verify (client callback) ──────────────────────────────────────

        public PaymentVerification VerifyPayment(
            GatewayCredentials creds, string externalOrderId, decimal expectedAmount,
            string paymentId, string signature, IDictionary<string, string> gatewayResponse)
        {
            // The client's response is only a trigger — but if it names a different txnid, refuse.
            if (gatewayResponse != null && gatewayResponse.TryGetValue("txnid", out var clientTxn) &&
                !string.IsNullOrWhiteSpace(clientTxn) && clientTxn != externalOrderId)
                return new PaymentVerification { Verified = false, Definitive = false, Error = "Payment response does not match this order." };

            var status = FetchOrderPayment(creds, externalOrderId);

            // The checkout can report success a moment before the Transaction API reflects it —
            // when the client says success, give Easebuzz a few seconds to catch up.
            var clientSaysSuccess = gatewayResponse != null && gatewayResponse.TryGetValue("status", out var cs) && IsSuccess(cs);
            for (var attempt = 0; clientSaysSuccess && attempt < 3
                     && !(status.Success && status.Found && (status.Captured || IsFinalFailure(status.Status))); attempt++)
            {
                System.Threading.Thread.Sleep(2000);
                status = FetchOrderPayment(creds, externalOrderId);
            }

            if (!status.Success)
                return new PaymentVerification { Verified = false, Definitive = false,
                    Error = $"Could not confirm the payment with Easebuzz yet: {status.Error}" };

            if (status.Found && status.Captured)
            {
                if (Math.Abs(status.AmountInRupees - expectedAmount) > 0.01m)
                    return new PaymentVerification { Verified = false, Definitive = false, PaymentId = status.PaymentId,
                        Error = $"Paid amount ₹{status.AmountInRupees:0.00} does not match the order ₹{expectedAmount:0.00}. The school will verify it." };

                return new PaymentVerification { Verified = true, PaymentId = status.PaymentId };
            }

            // failure / userCancelled / bounced / dropped are final; pending / initiated / not-found may still complete.
            var final = status.Found && IsFinalFailure(status.Status);
            return new PaymentVerification
            {
                Verified   = false,
                Definitive = final,
                PaymentId  = status.PaymentId,
                Error      = final
                    ? $"Payment was not completed (Easebuzz status: {status.Status})."
                    : "Payment is not confirmed yet. If money was deducted, the receipt will be generated automatically once Easebuzz confirms it.",
            };
        }

        /// <summary>
        /// Easebuzz webhooks are not HMAC-signed with a separate secret; they are confirmed by
        /// re-reading the transaction via FetchOrderPayment instead. Not used for Easebuzz.
        /// </summary>
        public bool VerifyWebhookSignature(string payload, string signature, string webhookSecret) => false;

        // ── helpers ───────────────────────────────────────────────────────

        private static bool IsSuccess(string status) =>
            string.Equals(status, "success", StringComparison.OrdinalIgnoreCase);

        private static bool IsFinalFailure(string status)
        {
            var s = (status ?? "").ToLowerInvariant();
            return s == "failure" || s == "usercancelled" || s == "bounced" || s == "dropped";
        }

        private static string PostForm(string url, Dictionary<string, string> form)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(form) };
            req.Headers.Accept.ParseAdd("application/json");
            // Synchronous call — Web API runs on the thread pool, no deadlock risk (same as Razorpay).
            var resp = _http.SendAsync(req).GetAwaiter().GetResult();
            var body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Easebuzz API error ({(int)resp.StatusCode}): {body}");
            return body;
        }

        private static string Sha512(string text)
        {
            using (var sha = SHA512.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private static string Clean(string value, string disallowedPattern, int maxLen, string fallback = "")
        {
            var v = Regex.Replace(value ?? "", disallowedPattern, " ");
            v = Regex.Replace(v, @"\s+", " ").Trim();
            if (v.Length > maxLen) v = v.Substring(0, maxLen).Trim();
            return v.Length == 0 ? fallback : v;
        }

        private static bool IsValidEmail(string email) =>
            !string.IsNullOrWhiteSpace(email) &&
            Regex.IsMatch(email.Trim(), @"^[^@\s<>()\[\],;:""]+@([a-zA-Z0-9\-]+\.)+[a-zA-Z]{2,}$");

        private static string NoteValue(Dictionary<string, string> notes, string key) =>
            notes != null && notes.TryGetValue(key, out var v) ? (v ?? "") : "";

        private static ExternalOrderResult Fail(string error) =>
            new ExternalOrderResult { Success = false, Error = error };
    }
}
