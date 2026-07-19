using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace AscentSchools.API.Helpers
{
    /// <summary>
    /// Sends push notifications via the Firebase Cloud Messaging HTTP v1 API.
    ///
    /// The legacy server-key API is retired, so v1 requires an OAuth2 bearer token
    /// minted from the Firebase service-account JSON. To avoid an SDK dependency
    /// (same house style as R2Service's manual SigV4), this hand-rolls the flow:
    ///   1. build an RS256-signed JWT asserting the service account,
    ///   2. exchange it at Google's token endpoint for an access token (cached ~55m),
    ///   3. POST each message to /v1/projects/{projectId}/messages:send.
    ///
    /// Path to the service-account JSON comes from Web.config "Fcm:ServiceAccountPath".
    /// When unconfigured / missing, IsConfigured is false and SendAsync is a no-op
    /// (push simply doesn't fire — it never breaks the calling create).
    /// </summary>
    public class FcmPushService
    {
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        private static readonly object _lock = new object();
        private static ServiceAccount _sa;      // cached parsed service account
        private static bool _saLoaded;
        private static string _accessToken;
        private static DateTime _tokenExpiryUtc = DateTime.MinValue;

        public bool IsConfigured => LoadServiceAccount() != null;

        /// <summary>Best-effort send to many tokens. Never throws. Returns count accepted by FCM.</summary>
        public async Task<int> SendAsync(IEnumerable<string> tokens, string title, string body,
                                         IDictionary<string, string> data)
        {
            var sa = LoadServiceAccount();
            if (sa == null) return 0;

            var list = tokens?.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList();
            if (list == null || list.Count == 0) return 0;

            string accessToken;
            try { accessToken = await GetAccessTokenAsync(sa); }
            catch { return 0; }
            if (string.IsNullOrEmpty(accessToken)) return 0;

            var url  = $"https://fcm.googleapis.com/v1/projects/{sa.ProjectId}/messages:send";
            int sent = 0;

            foreach (var token in list)
            {
                try
                {
                    var payload = new
                    {
                        message = new
                        {
                            token,
                            notification = new { title, body },
                            data,
                            android = new
                            {
                                priority = "high",
                                notification = new { channel_id = "ascent_default" }
                            }
                        }
                    };
                    var json = JsonConvert.SerializeObject(payload,
                        new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                    using (var req = new HttpRequestMessage(HttpMethod.Post, url))
                    {
                        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                        req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                        var resp = await _http.SendAsync(req);
                        if (resp.IsSuccessStatusCode) sent++;
                    }
                }
                catch { /* skip this token, keep going */ }
            }
            return sent;
        }

        // ── OAuth2 access token (JWT bearer grant) ──────────────────────────────

        private static async Task<string> GetAccessTokenAsync(ServiceAccount sa)
        {
            lock (_lock)
            {
                if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiryUtc)
                    return _accessToken;
            }

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var header  = Base64Url(Encoding.UTF8.GetBytes("{\"alg\":\"RS256\",\"typ\":\"JWT\"}"));
            var claims  = Base64Url(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
            {
                iss   = sa.ClientEmail,
                scope = "https://www.googleapis.com/auth/firebase.messaging",
                aud   = sa.TokenUri,
                iat   = now,
                exp   = now + 3600
            })));
            var signingInput = header + "." + claims;
            var signature    = Base64Url(SignRs256(Encoding.UTF8.GetBytes(signingInput), sa.PrivateKeyPem));
            var assertion    = signingInput + "." + signature;

            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"),
                new KeyValuePair<string, string>("assertion",  assertion)
            });

            var response = await _http.PostAsync(sa.TokenUri, form);
            if (!response.IsSuccessStatusCode) return null;

            var respBody = await response.Content.ReadAsStringAsync();
            var tok = JsonConvert.DeserializeObject<TokenResponse>(respBody);
            if (tok == null || string.IsNullOrEmpty(tok.access_token)) return null;

            lock (_lock)
            {
                _accessToken     = tok.access_token;
                // Refresh a couple of minutes before the real expiry.
                _tokenExpiryUtc  = DateTime.UtcNow.AddSeconds(Math.Max(60, tok.expires_in - 120));
            }
            return tok.access_token;
        }

        private static byte[] SignRs256(byte[] data, string privateKeyPem)
        {
            var rsaParams = ParsePkcs8RsaPrivateKey(privateKeyPem);
            using (var rsa = new RSACng())
            {
                rsa.ImportParameters(rsaParams);
                return rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
        }

        // ── Service-account loading ─────────────────────────────────────────────

        private ServiceAccount LoadServiceAccount()
        {
            if (_saLoaded) return _sa;
            lock (_lock)
            {
                if (_saLoaded) return _sa;
                _saLoaded = true;
                try
                {
                    var configured = ConfigurationManager.AppSettings["Fcm:ServiceAccountPath"];
                    if (string.IsNullOrWhiteSpace(configured)) return _sa = null;

                    var path = configured.StartsWith("~")
                        ? HostingEnvironment.MapPath(configured)
                        : configured;
                    if (string.IsNullOrEmpty(path) || !File.Exists(path)) return _sa = null;

                    var raw = JsonConvert.DeserializeObject<ServiceAccountJson>(File.ReadAllText(path));
                    if (raw == null || string.IsNullOrEmpty(raw.private_key) ||
                        string.IsNullOrEmpty(raw.project_id) || string.IsNullOrEmpty(raw.client_email))
                        return _sa = null;

                    return _sa = new ServiceAccount
                    {
                        ProjectId     = raw.project_id,
                        ClientEmail   = raw.client_email,
                        TokenUri      = string.IsNullOrEmpty(raw.token_uri) ? "https://oauth2.googleapis.com/token" : raw.token_uri,
                        PrivateKeyPem = raw.private_key
                    };
                }
                catch { return _sa = null; }
            }
        }

        // ── PKCS#8 private-key parser (no SDK) ───────────────────────────────────
        // Google service-account keys are PEM PKCS#8 ("-----BEGIN PRIVATE KEY-----").
        // Unwrap PrivateKeyInfo -> inner PKCS#1 RSAPrivateKey -> RSAParameters.

        private static RSAParameters ParsePkcs8RsaPrivateKey(string pem)
        {
            var base64 = pem
                .Replace("-----BEGIN PRIVATE KEY-----", "")
                .Replace("-----END PRIVATE KEY-----", "")
                .Replace("\r", "").Replace("\n", "").Trim();
            var der = Convert.FromBase64String(base64);

            // PrivateKeyInfo ::= SEQUENCE { version INTEGER, algorithm SEQUENCE, privateKey OCTET STRING }
            var outer = new DerReader(der);
            outer.ReadSequenceHeader();
            outer.ReadInteger();               // version
            outer.SkipSequence();              // AlgorithmIdentifier
            var pkcs1 = outer.ReadOctetString();

            // RSAPrivateKey ::= SEQUENCE { version, n, e, d, p, q, dp, dq, iq }
            var inner = new DerReader(pkcs1);
            inner.ReadSequenceHeader();
            inner.ReadInteger();               // version
            var n  = inner.ReadIntegerBytes();
            var e  = inner.ReadIntegerBytes();
            var d  = inner.ReadIntegerBytes();
            var p  = inner.ReadIntegerBytes();
            var q  = inner.ReadIntegerBytes();
            var dp = inner.ReadIntegerBytes();
            var dq = inner.ReadIntegerBytes();
            var iq = inner.ReadIntegerBytes();

            int modLen  = n.Length;                 // key size in bytes (leading zero already trimmed)
            int halfLen = (modLen + 1) / 2;
            return new RSAParameters
            {
                Modulus  = n,
                Exponent = e,
                D        = Pad(d,  modLen),
                P        = Pad(p,  halfLen),
                Q        = Pad(q,  halfLen),
                DP       = Pad(dp, halfLen),
                DQ       = Pad(dq, halfLen),
                InverseQ = Pad(iq, halfLen),
            };
        }

        private static byte[] Pad(byte[] b, int len)
        {
            if (b.Length == len) return b;
            if (b.Length > len)  // shouldn't happen, but guard
            {
                var t = new byte[len];
                Array.Copy(b, b.Length - len, t, 0, len);
                return t;
            }
            var r = new byte[len];
            Array.Copy(b, 0, r, len - b.Length, b.Length);
            return r;
        }

        private static string Base64Url(byte[] bytes) =>
            Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        // Minimal DER reader for the two SEQUENCEs we need.
        private class DerReader
        {
            private readonly byte[] _b;
            private int _pos;
            public DerReader(byte[] b) { _b = b; _pos = 0; }

            public void ReadSequenceHeader() { Expect(0x30); ReadLength(); }
            public void SkipSequence()       { Expect(0x30); int len = ReadLength(); _pos += len; }
            public byte[] ReadOctetString()  { Expect(0x04); int len = ReadLength(); return Take(len); }
            public byte[] ReadInteger()      { Expect(0x02); int len = ReadLength(); return Take(len); }
            public byte[] ReadIntegerBytes() => TrimLeadingZero(ReadInteger());

            private void Expect(byte tag) { if (_b[_pos++] != tag) throw new FormatException("Unexpected DER tag."); }
            private int ReadLength()
            {
                int first = _b[_pos++];
                if (first < 0x80) return first;
                int count = first & 0x7F, len = 0;
                for (int i = 0; i < count; i++) len = (len << 8) | _b[_pos++];
                return len;
            }
            private byte[] Take(int len) { var r = new byte[len]; Array.Copy(_b, _pos, r, 0, len); _pos += len; return r; }
            private static byte[] TrimLeadingZero(byte[] b)
            {
                if (b.Length > 1 && b[0] == 0x00) { var r = new byte[b.Length - 1]; Array.Copy(b, 1, r, 0, r.Length); return r; }
                return b;
            }
        }

        private class ServiceAccount
        {
            public string ProjectId     { get; set; }
            public string ClientEmail   { get; set; }
            public string TokenUri      { get; set; }
            public string PrivateKeyPem { get; set; }
        }

        private class ServiceAccountJson
        {
            public string project_id   { get; set; }
            public string client_email { get; set; }
            public string token_uri    { get; set; }
            public string private_key  { get; set; }
        }

        private class TokenResponse
        {
            public string access_token { get; set; }
            public int    expires_in   { get; set; }
        }
    }
}
