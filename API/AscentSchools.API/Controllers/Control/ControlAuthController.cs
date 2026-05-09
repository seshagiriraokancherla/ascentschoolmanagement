using AscentSchools.API.Helpers;
using AscentSchools.Core.DTOs.Control.Auth;
using AscentSchools.Core.Models;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.Control;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;

namespace AscentSchools.API.Controllers.Control
{
    [RoutePrefix("control/auth")]
    public class ControlAuthController : ApiController
    {
        private readonly ControlUserRepository        _users;
        private readonly MasterRefreshTokenRepository _tokens;

        public ControlAuthController()
        {
            var db  = new TenantConnectionFactory();
            _users  = new ControlUserRepository(db);
            _tokens = new MasterRefreshTokenRepository(db);
        }

        // POST control/auth/login
        [HttpPost, Route("login"), AllowAnonymous]
        public HttpResponseMessage Login([FromBody] ControlLoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return Request.CreateResponse(HttpStatusCode.BadRequest, ApiResponse.Fail("Username and password are required."));

            var user = _users.GetByUsername(request.Username);
            if (user == null || user.Status != "Active")
                return Request.CreateResponse(HttpStatusCode.Unauthorized, ApiResponse.Fail("Invalid credentials."));

            if (user.PasswordHash != JwtHelper.HashRefreshToken(request.Password))
                return Request.CreateResponse(HttpStatusCode.Unauthorized, ApiResponse.Fail("Invalid credentials."));

            _users.UpdateLastLogin(user.UserId);

            var response = BuildAuthResponse(user);
            return response;
        }

        // POST control/auth/refresh
        [HttpPost, Route("refresh"), AllowAnonymous]
        public HttpResponseMessage Refresh()
        {
            var rawToken = GetRefreshCookie();
            if (string.IsNullOrWhiteSpace(rawToken))
                return Request.CreateResponse(HttpStatusCode.Unauthorized, ApiResponse.Fail("No refresh token."));

            var hash   = JwtHelper.HashRefreshToken(rawToken);
            var record = _tokens.GetByHash(hash);

            if (record == null || record.RevokedAt.HasValue || record.ExpiresAt < DateTime.UtcNow)
                return Request.CreateResponse(HttpStatusCode.Unauthorized, ApiResponse.Fail("Refresh token expired or revoked."));

            var user = _users.GetById(record.UserId);
            if (user == null || user.Status != "Active")
                return Request.CreateResponse(HttpStatusCode.Unauthorized, ApiResponse.Fail("User inactive."));

            // Rotate refresh token (sliding expiry)
            var newRaw  = JwtHelper.GenerateRefreshToken();
            var newHash = JwtHelper.HashRefreshToken(newRaw);
            _tokens.Revoke(record.TokenId, newHash);
            _tokens.Create(record.UserId, newHash, JwtHelper.RefreshTokenExpiry());

            return BuildAuthResponse(user, newRaw);
        }

        // POST control/auth/logout
        [HttpPost, Route("logout"), AllowAnonymous]
        public HttpResponseMessage Logout()
        {
            var rawToken = GetRefreshCookie();
            if (!string.IsNullOrWhiteSpace(rawToken))
            {
                var record = _tokens.GetByHash(JwtHelper.HashRefreshToken(rawToken));
                if (record != null) _tokens.Revoke(record.TokenId);
            }

            var response = Request.CreateResponse(HttpStatusCode.OK, ApiResponse.Ok("Logged out."));
            ClearRefreshCookie(response);
            return response;
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private HttpResponseMessage BuildAuthResponse(ControlUserRecord user, string rawRefresh = null)
        {
            var accessToken = JwtHelper.GenerateControlAccessToken(new ControlTokenClaims
            {
                UserId   = user.UserId,
                FullName = user.FullName,
                Role     = user.Role
            });

            if (rawRefresh == null)
            {
                rawRefresh = JwtHelper.GenerateRefreshToken();
                _tokens.Create(user.UserId, JwtHelper.HashRefreshToken(rawRefresh), JwtHelper.RefreshTokenExpiry());
            }

            var response = Request.CreateResponse(HttpStatusCode.OK,
                ApiResponse<ControlLoginResponse>.Ok(new ControlLoginResponse
                {
                    AccessToken = accessToken,
                    UserId      = user.UserId,
                    FullName    = user.FullName,
                    Role        = user.Role
                }));

            SetRefreshCookie(response, rawRefresh);
            return response;
        }

        private void SetRefreshCookie(HttpResponseMessage response, string rawToken)
        {
            var cookie = new CookieHeaderValue("controlRefreshToken", rawToken)
            {
                HttpOnly = true,
                Secure   = false,   // set true in production (HTTPS)
                Path     = "/control/auth",
                Expires  = DateTimeOffset.UtcNow.AddDays(7)
            };
            response.Headers.AddCookies(new[] { cookie });
        }

        private void ClearRefreshCookie(HttpResponseMessage response)
        {
            var cookie = new CookieHeaderValue("controlRefreshToken", "")
            {
                HttpOnly = true,
                Path     = "/control/auth",
                Expires  = DateTimeOffset.UtcNow.AddDays(-1)
            };
            response.Headers.AddCookies(new[] { cookie });
        }

        private string GetRefreshCookie()
        {
            var cookies = Request.Headers.GetCookies("controlRefreshToken");
            if (cookies == null || cookies.Count == 0) return null;
            return cookies[0]["controlRefreshToken"].Value;
        }
    }
}
