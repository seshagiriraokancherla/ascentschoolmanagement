using AscentSchools.API.Filters;
using AscentSchools.API.Helpers;
using AscentSchools.API.Middleware;
using AscentSchools.Core.Models;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.Control;
using AscentSchools.Data.Repositories.School;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;

namespace AscentSchools.API.Controllers.Mobile
{
    [RoutePrefix("mobile/auth/teacher")]
    public class MobileTeacherAuthController : ApiController
    {
        // Google Play review bypass — fixed staff credentials for reviewer testing
        private const string ReviewTeacherUsername = "reviewteacher";
        private const string ReviewTeacherPassword = "review123";

        private readonly SchoolGroupRepository  _groups;
        private readonly SchoolAuthRepository   _auth;
        private readonly TenantConnectionFactory _db;

        public MobileTeacherAuthController()
        {
            _db     = new TenantConnectionFactory();
            _groups = new SchoolGroupRepository(_db);
            _auth   = new SchoolAuthRepository(_db);
        }

        // POST mobile/auth/teacher/login
        [HttpPost, Route("login"), AllowAnonymous]
        public HttpResponseMessage Login([FromBody] TeacherLoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return Fail(HttpStatusCode.BadRequest, "Username and Password are required.");

            var tenant = ResolveTenant();
            if (tenant == null) return Fail(HttpStatusCode.Unauthorized, "School not found. Check X-School-Code header.");

            var schoolId = GetSchoolId(tenant.DbName);
            if (schoolId == 0) return Fail(HttpStatusCode.ServiceUnavailable, "No active school branch found.");

            bool isReviewBypass = string.Equals(request.Username.Trim(), ReviewTeacherUsername, StringComparison.OrdinalIgnoreCase)
                                && string.Equals(request.Password, ReviewTeacherPassword, StringComparison.Ordinal);

            var user = _auth.GetByUsername(tenant.DbName, request.Username.Trim().ToLower());
            if (user == null)
                return Fail(HttpStatusCode.Unauthorized, "Invalid username or password.");

            if (user.Status != "Active")
                return Fail(HttpStatusCode.Unauthorized, "Account is deactivated.");

            // Review bypass skips hash check — password in DB may differ; real users always validated normally
            if (!isReviewBypass && user.PasswordHash != JwtHelper.HashRefreshToken(request.Password))
                return Fail(HttpStatusCode.Unauthorized, "Invalid username or password.");

            _auth.UpdateLastLogin(tenant.DbName, user.UserId);

            return BuildTeacherAuthResponse(tenant, user.UserId, user.FullName, schoolId);
        }

        // POST mobile/auth/teacher/refresh
        [HttpPost, Route("refresh"), AllowAnonymous]
        public HttpResponseMessage Refresh()
        {
            var rawToken = ResolveRefreshToken("teacherRefreshToken");
            if (string.IsNullOrWhiteSpace(rawToken))
                return Fail(HttpStatusCode.Unauthorized, "No refresh token.");

            var tenant = ResolveTenant();
            if (tenant == null) return Fail(HttpStatusCode.Unauthorized, "School not found.");

            var hash   = JwtHelper.HashRefreshToken(rawToken);
            var record = _auth.GetRefreshToken(tenant.DbName, hash);

            if (record == null || record.RevokedAt.HasValue || record.ExpiresAt < DateTime.UtcNow)
                return Fail(HttpStatusCode.Unauthorized, "Refresh token expired or revoked.");

            var user     = _auth.GetById(tenant.DbName, record.UserId);
            if (user == null || user.Status != "Active")
                return Fail(HttpStatusCode.Unauthorized, "Account inactive.");

            var schoolId = GetSchoolId(tenant.DbName);

            // Stable refresh token — slide the expiry forward instead of rotating, so a
            // device that fails to persist a rotated token is never logged out (matches the
            // parent flow). The web school app still rotates (it uses a different endpoint).
            _auth.ExtendRefreshToken(tenant.DbName, record.TokenId, JwtHelper.MobileRefreshTokenExpiry());

            return BuildTeacherAuthResponse(tenant, user.UserId, user.FullName, schoolId, rawToken);
        }

        // POST mobile/auth/teacher/logout
        [HttpPost, Route("logout"), AllowAnonymous]
        public HttpResponseMessage Logout()
        {
            var rawToken = GetRefreshCookie("teacherRefreshToken");
            if (!string.IsNullOrWhiteSpace(rawToken))
            {
                var tenant = ResolveTenant();
                if (tenant?.GroupId > 0 && !string.IsNullOrWhiteSpace(tenant.DbName))
                {
                    var record = _auth.GetRefreshToken(tenant.DbName, JwtHelper.HashRefreshToken(rawToken));
                    if (record != null) _auth.RevokeRefreshToken(tenant.DbName, record.TokenId);
                }
            }

            var response = Request.CreateResponse(HttpStatusCode.OK, ApiResponse.Ok("Logged out."));
            ClearCookie(response, "teacherRefreshToken", "/");
            return response;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private HttpResponseMessage BuildTeacherAuthResponse(
            TenantInfo tenant, int userId, string fullName, int schoolId, string rawRefresh = null)
        {
            var accessToken = JwtHelper.GenerateMobileTeacherToken(
                userId, schoolId, tenant.GroupId, tenant.DbName, fullName);

            if (rawRefresh == null)
            {
                rawRefresh = JwtHelper.GenerateRefreshToken();
                var hash   = JwtHelper.HashRefreshToken(rawRefresh);
                _auth.CreateRefreshToken(tenant.DbName, userId, hash, JwtHelper.MobileRefreshTokenExpiry());
            }

            var response = Request.CreateResponse(HttpStatusCode.OK,
                ApiResponse<TeacherAuthResponse>.Ok(new TeacherAuthResponse
                {
                    AccessToken  = accessToken,
                    RefreshToken = rawRefresh,
                    TokenType    = "Bearer",
                    FullName     = fullName,
                    UserId       = userId,
                    SchoolId     = schoolId,
                }));

            SetCookie(response, "teacherRefreshToken", rawRefresh, "/");
            return response;
        }

        private TenantInfo ResolveTenant()
        {
            IEnumerable<string> vals;
            var subdomain = Request.Headers.TryGetValues("X-School-Code", out vals) ? vals.FirstOrDefault() : null;
            if (string.IsNullOrWhiteSpace(subdomain))
                subdomain = Request.Headers.TryGetValues("X-Subdomain", out vals) ? vals.FirstOrDefault() : null;
            if (string.IsNullOrWhiteSpace(subdomain)) return null;
            return _groups.GetTenantBySubdomain(subdomain);
        }

        private int GetSchoolId(string dbName)
        {
            int? groupId;
            using (var conn = _db.GetMasterConnection())
                groupId = conn.QueryFirstOrDefault<int?>(
                    "SELECT group_id FROM school_groups WHERE db_name = @dbName AND status = 'Active'",
                    new { dbName });

            if (groupId.HasValue)
            {
                using (var conn = _db.GetMasterConnection())
                {
                    var id = conn.QueryFirstOrDefault<int?>(
                        "SELECT TOP 1 school_id FROM schools WHERE group_id = @groupId AND status = 'Active'",
                        new { groupId = groupId.Value });
                    if (id.HasValue) return id.Value;
                }
            }

            using (var conn = _db.GetTenantConnection(dbName))
                return conn.QueryFirstOrDefault<int>(
                    "SELECT TOP 1 school_id FROM students WHERE school_id IS NOT NULL", null);
        }

        private string GetRefreshCookie(string name)
        {
            if (Request.Headers.TryGetValues("Cookie", out var cookieVals))
            {
                foreach (var cookieHeader in cookieVals)
                {
                    foreach (var part in cookieHeader.Split(';'))
                    {
                        var kv = part.Trim().Split(new[] { '=' }, 2);
                        if (kv.Length == 2 && kv[0].Trim() == name)
                            return Uri.UnescapeDataString(kv[1].Trim());
                    }
                }
            }
            return null;
        }

        // X-Refresh-Token header first (mobile app holds the token itself — the
        // HttpOnly cookie is unreliable across app kill), then the cookie (web).
        private string ResolveRefreshToken(string cookieName)
        {
            if (Request.Headers.TryGetValues("X-Refresh-Token", out var vals))
            {
                var header = vals.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(header)) return header;
            }
            return GetRefreshCookie(cookieName);
        }

        private void SetCookie(HttpResponseMessage response, string name, string value, string path)
        {
            var cookie = new CookieHeaderValue(name, value)
            {
                HttpOnly = true,
                Secure   = false, // true in production
                Path     = path,
                Expires  = DateTimeOffset.UtcNow.AddDays(JwtHelper.MobileRefreshDays),
            };
            response.Headers.AddCookies(new[] { cookie });
        }

        private void ClearCookie(HttpResponseMessage response, string name, string path)
        {
            var cookie = new CookieHeaderValue(name, "")
            {
                HttpOnly = true,
                Path     = path,
                Expires  = DateTimeOffset.UtcNow.AddDays(-1),
            };
            response.Headers.AddCookies(new[] { cookie });
        }

        private HttpResponseMessage Fail(HttpStatusCode code, string message) =>
            Request.CreateResponse(code, new { success = false, message });
    }

    // ── DTOs (teacher auth) ──────────────────────────────────────────────────

    public class TeacherLoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class TeacherAuthResponse
    {
        public string AccessToken { get; set; }
        // Raw refresh token — stored by the mobile app and sent back explicitly
        // on refresh (see MobileAuthResponse.RefreshToken).
        public string RefreshToken { get; set; }
        public string TokenType   { get; set; }
        public string FullName    { get; set; }
        public int    UserId      { get; set; }
        public int    SchoolId    { get; set; }
    }
}
