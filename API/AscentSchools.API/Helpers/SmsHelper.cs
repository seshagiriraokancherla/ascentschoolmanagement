using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace AscentSchools.API.Helpers
{
    public static class SmsHelper
    {
        private const string ApiUrl   = "https://smslogin.mobi/v3/api.php";
        private const string Username = "mushkin";
        private const string ApiKey   = "c6cacc7b6d643993b770";
        private const string SenderId = "ASNTNF";

        // OTP template (registered) — OTP always uses the hardcoded global account above.
        private const string OtpTemplateId = "1707177794812186006";

        // NOTE: School SMS (Absent / FeeDue / Custom / future types) no longer uses
        // hardcoded constants. The SMS Center reads each school's own account + DLT
        // template ids from the sms_configs / sms_templates tables and calls the
        // per-account SendSms overload below. OTP is intentionally left global.

        /// <summary>
        /// Sends a 6-digit OTP via SMS using the global account. Unchanged.
        /// </summary>
        public static string SendOtp(string mobile, string parentName, string otp)
        {
            var message = string.Format(
                "Dear {0}, your verification code for Ascent Info Solutions Parent App is {1}. Please do not share this code with anyone.",
                parentName, otp);
            return SendSms(mobile, message, OtpTemplateId);
        }

        /// <summary>
        /// Sends an SMS through the GLOBAL hardcoded account (used by OTP only).
        /// Returns the raw gateway response; a response starting with "ERROR=" means an exception occurred.
        /// </summary>
        public static string SendSms(string mobile, string message, string templateId)
            => SendSms(ApiUrl, Username, ApiKey, SenderId, mobile, message, templateId);

        /// <summary>
        /// Sends an SMS through a per-school account (used by the SMS Center).
        /// Returns the raw gateway response; a response starting with "ERROR=" means an exception occurred.
        /// </summary>
        public static string SendSms(string apiUrl, string username, string apiKey, string senderId,
                                     string mobile, string message, string templateId)
        {
            try
            {
                var url = string.Format("{0}?username={1}&apikey={2}&senderid={3}&mobile={4}&message={5}&templateid={6}",
                    apiUrl, username, apiKey, senderId,
                    Uri.EscapeDataString(mobile),
                    Uri.EscapeDataString(message),
                    templateId);

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                ServicePointManager.ServerCertificateValidationCallback = (object s, X509Certificate c, X509Chain ch, SslPolicyErrors e) => true;

                using (var client = new WebClient())
                    return client.DownloadString(url);
            }
            catch (Exception ex)
            {
                return "ERROR=" + ex.Message;
            }
        }
    }
}
