using AscentSchools.API.Helpers;
using AscentSchools.API.Middleware;
using AscentSchools.Core.DTOs.Mobile.Auth;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;

namespace AscentSchools.API.Filters
{
    /// <summary>
    /// Global authentication filter — runs on every request before any controller action.
    ///
    /// Supports two authentication mechanisms:
    ///   1. Bearer JWT  — school/control/mobile staff tokens
    ///   2. X-Api-Key   — static key for VB6 legacy integration (no token management needed)
    ///
    /// API key lookup: master DB → school_settings.integration_api_key
    ///   → grants full permissions so existing school endpoints work transparently.
    /// </summary>
    public class JwtAuthenticationFilter : IAuthenticationFilter
    {
        public bool AllowMultiple => false;

        public async Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            var request = context.Request;

            // ── API Key auth (VB integration) ─────────────────────────────────
            // Checked before Bearer so VB requests never need a JWT token.
            var apiKey = request.Headers.TryGetValues("X-Api-Key", out var keyValues)
                ? keyValues?.FirstOrDefault()?.Trim()
                : null;

            if (!string.IsNullOrEmpty(apiKey))
            {
                var tenantCtx = await LookupApiKeyAsync(apiKey);
                if (tenantCtx == null)
                {
                    context.ErrorResult = new UnauthorizedResult(request, "Invalid API key.");
                    return;
                }
                TenantContext.Current = tenantCtx;
                context.Principal     = new ClaimsPrincipal(
                    new ClaimsIdentity(new[] { new Claim("tokenType", "apikey") }, "ApiKey"));
                return;
            }

            // ── Bearer JWT auth ───────────────────────────────────────────────
            var auth = request.Headers.Authorization;
            if (auth == null || auth.Scheme != "Bearer" || string.IsNullOrWhiteSpace(auth.Parameter))
                return; // No token — SchoolAuthAttribute will return 401 if the endpoint requires auth

            var principal = JwtHelper.ValidateAccessToken(auth.Parameter);
            if (principal == null)
            {
                context.ErrorResult = new UnauthorizedResult(request, "Invalid or expired token.");
                return;
            }

            var tokenType = principal.Claims.FirstOrDefault(c => c.Type == "tokenType")?.Value;

            // Control tokens — validated by ControlAuthAttribute; skip TenantContext
            if (tokenType == "control")
            {
                context.Principal = principal;
                return;
            }

            // Mobile tokens — populate MobileContext; skip TenantContext
            if (tokenType == "student")
            {
                var sc = JwtHelper.ExtractMobileStudentClaims(principal);
                if (sc != null)
                {
                    MobileContext.Current = new MobileContext
                    {
                        TokenType   = "student",
                        AccountId   = sc.AccountId,
                        StudentId   = sc.StudentId,
                        GroupId     = sc.GroupId,
                        SchoolId    = sc.SchoolId,
                        DbName      = sc.DbName,
                        DisplayName = sc.StudentName,
                        ClassName   = sc.ClassName,
                        AdmissionNo = sc.AdmissionNo,
                    };
                }
                context.Principal = principal;
                return;
            }

            if (tokenType == "teacher")
            {
                var c = principal.Claims.ToList();
                int.TryParse(c.FirstOrDefault(x => x.Type == "userId")?.Value,   out var tUid);
                int.TryParse(c.FirstOrDefault(x => x.Type == "schoolId")?.Value, out var tScid);
                int.TryParse(c.FirstOrDefault(x => x.Type == "groupId")?.Value,  out var tGid);
                TeacherContext.Current = new TeacherContext
                {
                    UserId   = tUid,
                    SchoolId = tScid,
                    GroupId  = tGid,
                    DbName   = c.FirstOrDefault(x => x.Type == "dbName")?.Value,
                    FullName = c.FirstOrDefault(x => x.Type == "fullName")?.Value,
                };
                context.Principal = principal;
                return;
            }

            if (tokenType == "parent")
            {
                var pc = JwtHelper.ExtractMobileParentClaims(principal);
                if (pc != null)
                {
                    MobileContext.Current = new MobileContext
                    {
                        TokenType   = "parent",
                        ParentId    = pc.ParentId,
                        StudentId   = pc.StudentId,
                        GroupId     = pc.GroupId,
                        SchoolId    = pc.SchoolId,
                        DbName      = pc.DbName,
                        DisplayName = pc.FullName,
                        ClassName   = pc.ClassName,
                        AdmissionNo = pc.AdmissionNo,
                    };
                }
                context.Principal = principal;
                return;
            }

            var claims = JwtHelper.ExtractClaims(principal);
            if (claims == null)
            {
                context.ErrorResult = new UnauthorizedResult(request, "Invalid or expired token.");
                return;
            }

            TenantContext.Current = new TenantContext
            {
                UserId       = claims.UserId,
                FullName     = claims.FullName,
                GroupId      = claims.GroupId,
                SchoolId     = claims.SchoolId.GetValueOrDefault(),
                TenantDbName = claims.TenantDbName,
                Permissions  = claims.Permissions
            };

            context.Principal = principal;
        }

        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
            => Task.FromResult(0);

        // ── API Key lookup ────────────────────────────────────────────────────

        private static async Task<TenantContext> LookupApiKeyAsync(string key)
        {
            try
            {
                var factory = new TenantConnectionFactory();
                using (var conn = factory.GetMasterConnection())
                {
                    var row = await conn.QueryFirstOrDefaultAsync<ApiKeyRow>(
                        @"SELECT ss.school_id SchoolId,
                                 s.group_id   GroupId,
                                 sg.db_name   DbName
                          FROM   school_settings ss
                          JOIN   schools s        ON s.school_id  = ss.school_id
                          JOIN   school_groups sg ON sg.group_id  = s.group_id
                          WHERE  ss.integration_api_key = @key
                            AND  sg.status  = 'Active'
                            AND  sg.db_name IS NOT NULL",
                        new { key });

                    if (row == null || string.IsNullOrEmpty(row.DbName))
                        return null;

                    return new TenantContext
                    {
                        UserId       = 0,
                        FullName     = "VB Integration",
                        GroupId      = row.GroupId,
                        SchoolId     = row.SchoolId,
                        TenantDbName = row.DbName,
                        Permissions  = _allPermissions
                    };
                }
            }
            catch
            {
                return null;
            }
        }

        private class ApiKeyRow
        {
            public int    SchoolId { get; set; }
            public int    GroupId  { get; set; }
            public string DbName   { get; set; }
        }

        // Full permission set granted to VB integration key — no per-user RBAC needed
        private static readonly string[] _allPermissions =
        {
            "STUDENT_PROFILE.VIEW",  "STUDENT_PROFILE.CREATE", "STUDENT_PROFILE.EDIT", "STUDENT_PROFILE.DELETE",
            "STUDENT_FEE.VIEW",      "STUDENT_FEE.COLLECT",    "STUDENT_FEE.EDIT",
            "STUDENT_FEE.CANCEL_RECEIPT",                       "STUDENT_FEE.CONCESSION",
            "ATTENDANCE.VIEW",       "ATTENDANCE.MARK",         "ATTENDANCE.EDIT",
            "MARKS.VIEW",            "MARKS.ENTER",             "MARKS.EDIT",           "MARKS.PUBLISH",
            "LIBRARY.VIEW",          "LIBRARY.ISSUE",           "LIBRARY.RETURN",       "LIBRARY.MANAGE",
            "TRANSPORT.VIEW",        "TRANSPORT.MANAGE",
            "HOSTEL.VIEW",           "HOSTEL.MANAGE"
        };
    }

    /// <summary>Returns a clean 401 response with a descriptive message.</summary>
    internal class UnauthorizedResult : System.Web.Http.IHttpActionResult
    {
        private readonly HttpRequestMessage _request;
        private readonly string             _message;

        public UnauthorizedResult(HttpRequestMessage request, string message = "Unauthorized.")
        {
            _request = request;
            _message = message;
        }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            var response = _request.CreateResponse(HttpStatusCode.Unauthorized,
                new { success = false, message = _message });
            return Task.FromResult(response);
        }
    }
}
