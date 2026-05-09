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

        // OTP template (registered)
        private const string OtpTemplateId = "1707177794812186006";

        // School SMS templates — register these DLT templates before going live
        public const string AbsentTemplateId = "PLACEHOLDER_ABSENT_TPL_ID";
        public const string FeeDueTemplateId  = "PLACEHOLDER_FEEDUE_TPL_ID";
        public const string CustomTemplateId  = "PLACEHOLDER_CUSTOM_TPL_ID";

        /// <summary>
        /// Sends a 6-digit OTP via SMS.
        /// </summary>
        public static string SendOtp(string mobile, string parentName, string otp)
        {
            var message = string.Format(
                "Dear {0}, your verification code for Ascent Info Solutions Parent App is {1}. Please do not share this code with anyone.",
                parentName, otp);
            return SendSms(mobile, message, OtpTemplateId);
        }

        /// <summary>
        /// Sends any SMS through the gateway.
        /// Returns the raw gateway response; a response starting with "ERROR=" means an exception occurred.
        /// </summary>
        public static string SendSms(string mobile, string message, string templateId)
        {
            try
            {
                var url = string.Format("{0}?username={1}&apikey={2}&senderid={3}&mobile={4}&message={5}&templateid={6}",
                    ApiUrl, Username, ApiKey, SenderId,
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
