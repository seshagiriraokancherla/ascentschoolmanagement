using AscentSchools.Core.DTOs.School.Settings;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AscentSchools.API.Helpers
{
    /// <summary>
    /// Cloudflare R2 (S3-compatible) helpers. Generates SigV4 presigned PUT URLs so the
    /// browser/app uploads files directly to R2 (no bytes through our API/IIS) — required to
    /// support large (1 GB) videos. Region is "auto" for R2. No AWS SDK dependency.
    /// </summary>
    public static class R2Service
    {
        private const string Region  = "auto";
        private const string Service = "s3";

        public static string PublicUrl(R2ConfigInternal cfg, string key)
            => cfg.PublicBaseUrl.TrimEnd('/') + "/" + key;

        /// <summary>Presigned PUT URL for a direct browser→R2 upload (path-style, UNSIGNED-PAYLOAD, host-only signed header).</summary>
        public static string PresignPut(R2ConfigInternal cfg, string key, int expiresSeconds = 600)
        {
            var host   = cfg.AccountId + ".r2.cloudflarestorage.com";
            var method = "PUT";

            var now       = DateTime.UtcNow;
            var amzDate   = now.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
            var dateStamp = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

            var canonicalUri = "/" + UriEncode(cfg.BucketName, false) + "/" + UriEncode(key, true);
            var credential   = cfg.AccessKeyId + "/" + dateStamp + "/" + Region + "/" + Service + "/aws4_request";

            var query = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "X-Amz-Algorithm",     "AWS4-HMAC-SHA256" },
                { "X-Amz-Credential",    credential },
                { "X-Amz-Date",          amzDate },
                { "X-Amz-Expires",       expiresSeconds.ToString(CultureInfo.InvariantCulture) },
                { "X-Amz-SignedHeaders", "host" },
            };
            var canonicalQuery = string.Join("&",
                query.Select(kv => UriEncode(kv.Key, false) + "=" + UriEncode(kv.Value, false)));

            var canonicalHeaders = "host:" + host + "\n";
            const string signedHeaders = "host";

            var canonicalRequest = string.Join("\n",
                method, canonicalUri, canonicalQuery, canonicalHeaders, signedHeaders, "UNSIGNED-PAYLOAD");

            var scope = dateStamp + "/" + Region + "/" + Service + "/aws4_request";
            var stringToSign = string.Join("\n",
                "AWS4-HMAC-SHA256", amzDate, scope, Hex(Sha256(canonicalRequest)));

            var signingKey = SigningKey(cfg.SecretAccessKey, dateStamp);
            var signature  = Hex(HmacSha256(signingKey, Encoding.UTF8.GetBytes(stringToSign)));

            return "https://" + host + canonicalUri + "?" + canonicalQuery + "&X-Amz-Signature=" + signature;
        }

        // ── SigV4 helpers ──────────────────────────────────────────────────────
        private static string UriEncode(string value, bool isPath)
        {
            var sb = new StringBuilder();
            foreach (var b in Encoding.UTF8.GetBytes(value ?? ""))
            {
                char c = (char)b;
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') ||
                    c == '-' || c == '_' || c == '.' || c == '~')
                    sb.Append(c);
                else if (c == '/' && isPath)
                    sb.Append('/');
                else
                    sb.Append('%').Append(((int)b).ToString("X2"));
            }
            return sb.ToString();
        }

        private static byte[] Sha256(string s)
        {
            using (var h = SHA256.Create()) return h.ComputeHash(Encoding.UTF8.GetBytes(s));
        }

        private static byte[] HmacSha256(byte[] key, byte[] data)
        {
            using (var h = new HMACSHA256(key)) return h.ComputeHash(data);
        }

        private static string Hex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        private static byte[] SigningKey(string secret, string dateStamp)
        {
            var kDate    = HmacSha256(Encoding.UTF8.GetBytes("AWS4" + secret), Encoding.UTF8.GetBytes(dateStamp));
            var kRegion  = HmacSha256(kDate, Encoding.UTF8.GetBytes(Region));
            var kService = HmacSha256(kRegion, Encoding.UTF8.GetBytes(Service));
            return HmacSha256(kService, Encoding.UTF8.GetBytes("aws4_request"));
        }
    }
}
