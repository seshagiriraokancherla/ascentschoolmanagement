using AscentSchools.API.Filters;
using AscentSchools.API.Helpers;
using AscentSchools.API.Middleware;
using AscentSchools.Core.DTOs.Auth;
using AscentSchools.Core.Models;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.Control;
using AscentSchools.Data.Repositories.School;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    [RoutePrefix("school/auth")]
    public class SchoolAuthController : ApiController
    {
        private readonly SchoolGroupRepository _groups;
        private readonly SchoolAuthRepository  _auth;

        public SchoolAuthController()
        {
            var db  = new TenantConnectionFactory();
            _groups = new SchoolGroupRepository(db);
            _auth   = new SchoolAuthRepository(db);
        }

        // POST school/auth/login
        [HttpPost, Route("login"), AllowAnonymous]
        public HttpResponseMessage Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return Request.CreateResponse(HttpStatusCode.BadRequest, ApiResponse.Fail("Username and password are required."));

            var subdomain = GetSubdomain();
            if (string.IsNullOrWhiteSpace(subdomain))
                return Request.CreateResponse(HttpStatusCode.BadRequest, ApiResponse.Fail("X-Subdomain header is required."));

            var tenant = _groups.GetTenantBySubdomain(subdomain);
            if (tenant == null)
                return Request.CreateResponse(HttpStatusCode.Unauthorized, ApiResponse.Fail("School not found or inactive."));

            if (string.IsNullOrWhiteSpace(tenant.DbName))
                return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, ApiResponse.Fail("Tenant database not yet provisioned. Contact support."));

            var user = _auth.GetByUsername(tenant.DbName, request.Username);
            if (user == null || user.Status != "Active")
                return Request.CreateResponse(HttpStatusCode.Unauthorized, ApiResponse.Fail("Invalid credentials."));

            if (user.PasswordHash != JwtHelper.HashRefreshToken(request.Password))
                return Request.CreateResponse(HttpStatusCode.Unauthorized, ApiResponse.Fail("Invalid credentials."));

            // Determine school branch for this session
            int schoolId;
            if (user.SchoolId.HasValue)
            {
                schoolId = user.SchoolId.Value;
            }
            else
            {
                var schoolIds = _auth.GetUserSchoolIds(tenant.DbName, user.UserId).ToList();
                if (schoolIds.Count == 0)
                    return Request.CreateResponse(HttpStatusCode.Forbidden, ApiResponse.Fail("User has no branch assignments."));

                schoolId = request.SchoolId.HasValue && schoolIds.Contains(request.SchoolId.Value)
                    ? request.SchoolId.Value
                    : schoolIds[0];
            }

            var permissions = _auth.GetUserPermissions(tenant.DbName, user.UserId, schoolId).ToArray();

            _auth.UpdateLastLogin(tenant.DbName, user.UserId);

            return BuildAuthResponse(tenant.DbName, user.UserId, user.FullName, tenant.GroupId, schoolId, permissions);
        }

        // POST school/auth/refresh  — schoolId passed as query string on page reload
        [HttpPost, Route("refresh"), AllowAnonymous]
        public HttpResponseMessage Refresh([FromUri] int? schoolId = null)
        {
            var rawToken = GetRefreshCookie();
            if (string.IsNullOrWhiteSpace(rawToken))
                return Request.CreateResponse(HttpStatusCode.Unauthorized, ApiResponse.Fail("No refresh token."));

            var subdomain = GetSubdomain();
            if (string.IsNullOrWhiteSpace(subdomain))
                return Request.CreateResponse(HttpStatusCode.BadRequest, ApiResponse.Fail("X-Subdomain header is required."));

            var tenant = _groups.GetTenantBySubdomain(subdomain);
            if (tenant == null)
                return Request.CreateResponse(HttpStatusCode.Unauthorized, ApiResponse.Fail("School not found."));

            if (string.IsNullOrWhiteSpace(tenant.DbName))
                return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, ApiResponse.Fail("Tenant database not yet provisioned. Contact support."));

            var hash   = JwtHelper.HashRefreshToken(rawToken);
            var record = _auth.GetRefreshToken(tenant.DbName, hash);

            if (record == null || record.RevokedAt.HasValue || record.ExpiresAt < DateTime.UtcNow)
                return Request.CreateResponse(HttpStatusCode.Unauthorized, ApiResponse.Fail("Refresh token expired or revoked."));

            var user = _auth.GetById(tenant.DbName, record.UserId);
            if (user == null || user.Status != "Active")
                return Request.CreateResponse(HttpStatusCode.Unauthorized, ApiResponse.Fail("User inactive."));

            // Determine schoolId for the new token
            int resolvedSchoolId;
            if (user.SchoolId.HasValue)
            {
                resolvedSchoolId = user.SchoolId.Value;
            }
            else
            {
                var schoolIds = _auth.GetUserSchoolIds(tenant.DbName, user.UserId).ToList();
                if (schoolIds.Count == 0)
                    return Request.CreateResponse(HttpStatusCode.Forbidden, ApiResponse.Fail("User has no branch assignments."));

                resolvedSchoolId = schoolId.HasValue && schoolIds.Contains(schoolId.Value)
                    ? schoolId.Value
                    : schoolIds[0];
            }

            var permissions = _auth.GetUserPermissions(tenant.DbName, user.UserId, resolvedSchoolId).ToArray();

            // Rotate refresh token (sliding expiry)
            var newRaw  = JwtHelper.GenerateRefreshToken();
            var newHash = JwtHelper.HashRefreshToken(newRaw);
            _auth.RevokeRefreshToken(tenant.DbName, record.TokenId, newHash);
            _auth.CreateRefreshToken(tenant.DbName, user.UserId, newHash, JwtHelper.RefreshTokenExpiry());

            return BuildAuthResponse(tenant.DbName, user.UserId, user.FullName, tenant.GroupId, resolvedSchoolId, permissions, newRaw);
        }

        // POST school/auth/logout
        [HttpPost, Route("logout"), AllowAnonymous]
        public HttpResponseMessage Logout()
        {
            var rawToken = GetRefreshCookie();
            if (!string.IsNullOrWhiteSpace(rawToken))
            {
                var subdomain = GetSubdomain();
                if (subdomain != null)
                {
                    var tenant = _groups.GetTenantBySubdomain(subdomain);
                    if (tenant != null && !string.IsNullOrWhiteSpace(tenant.DbName))
                    {
                        var record = _auth.GetRefreshToken(tenant.DbName, JwtHelper.HashRefreshToken(rawToken));
                        if (record != null) _auth.RevokeRefreshToken(tenant.DbName, record.TokenId);
                    }
                }
            }

            var response = Request.CreateResponse(HttpStatusCode.OK, ApiResponse.Ok("Logged out."));
            ClearRefreshCookie(response);
            return response;
        }

        // POST school/auth/change-password — self-service (any authenticated school user).
        [HttpPost, Route("change-password"), SchoolAuth]
        public HttpResponseMessage ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
                return Request.CreateResponse(HttpStatusCode.BadRequest, ApiResponse.Fail("Current and new password are required."));
            if (request.NewPassword.Length < 6)
                return Request.CreateResponse(HttpStatusCode.BadRequest, ApiResponse.Fail("New password must be at least 6 characters."));

            var tenant = TenantContext.Current;   // populated (SchoolAuth guarantees non-null)
            var user   = _auth.GetById(tenant.TenantDbName, tenant.UserId);
            if (user == null)
                return Request.CreateResponse(HttpStatusCode.Unauthorized, ApiResponse.Fail("User not found."));

            if (user.PasswordHash != JwtHelper.HashRefreshToken(request.CurrentPassword))
                return Request.CreateResponse(HttpStatusCode.BadRequest, ApiResponse.Fail("Current password is incorrect."));

            _auth.UpdatePassword(tenant.TenantDbName, tenant.UserId, JwtHelper.HashRefreshToken(request.NewPassword));
            return Request.CreateResponse(HttpStatusCode.OK, ApiResponse.Ok("Password changed. Please log in again."));
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private HttpResponseMessage BuildAuthResponse(
            string dbName, int userId, string fullName,
            int groupId, int schoolId, string[] permissions,
            string rawRefresh = null)
        {
            var accessToken = JwtHelper.GenerateAccessToken(new TokenClaims
            {
                UserId       = userId,
                FullName     = fullName,
                GroupId      = groupId,
                SchoolId     = schoolId,
                TenantDbName = dbName,
                Permissions  = permissions
            });

            if (rawRefresh == null)
            {
                rawRefresh = JwtHelper.GenerateRefreshToken();
                _auth.CreateRefreshToken(dbName, userId, JwtHelper.HashRefreshToken(rawRefresh), JwtHelper.RefreshTokenExpiry());
            }

            var response = Request.CreateResponse(HttpStatusCode.OK,
                ApiResponse<LoginResponse>.Ok(new LoginResponse
                {
                    AccessToken = accessToken,
                    UserId      = userId,
                    FullName    = fullName,
                    GroupId     = groupId,
                    SchoolId    = schoolId,
                    Permissions = permissions
                }));

            SetRefreshCookie(response, rawRefresh);
            return response;
        }

        private string GetSubdomain()
        {
            IEnumerable<string> vals;
            return Request.Headers.TryGetValues("X-Subdomain", out vals) ? vals.FirstOrDefault() : null;
        }

        private void SetRefreshCookie(HttpResponseMessage response, string rawToken)
        {
            var cookie = new CookieHeaderValue("schoolRefreshToken", rawToken)
            {
                HttpOnly = true,
                Secure   = false,   // set true in production (HTTPS)
                Path     = "/",   // must cover the /api/... prefix so the cookie reaches /school/auth/refresh
                Expires  = DateTimeOffset.UtcNow.AddDays(7)
            };
            response.Headers.AddCookies(new[] { cookie });
        }

        private void ClearRefreshCookie(HttpResponseMessage response)
        {
            var cookie = new CookieHeaderValue("schoolRefreshToken", "")
            {
                HttpOnly = true,
                Path     = "/",   // must cover the /api/... prefix so the cookie reaches /school/auth/refresh
                Expires  = DateTimeOffset.UtcNow.AddDays(-1)
            };
            response.Headers.AddCookies(new[] { cookie });
        }

        private string GetRefreshCookie()
        {
            var cookies = Request.Headers.GetCookies("schoolRefreshToken");
            if (cookies == null || cookies.Count == 0) return null;
            return cookies[0]["schoolRefreshToken"].Value;
        }
    }
}
