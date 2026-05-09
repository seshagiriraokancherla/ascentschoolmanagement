using AscentSchools.API.Helpers;
using AscentSchools.API.Middleware;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;

namespace AscentSchools.API.Filters
{
    /// <summary>
    /// Global filter — runs on every request.
    /// Validates the Bearer JWT, populates TenantContext.Current.
    /// Routes with [AllowAnonymous] skip the 401 response but still parse the token if present.
    /// </summary>
    public class JwtAuthenticationFilter : IAuthenticationFilter
    {
        public bool AllowMultiple => false;

        public Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            var request = context.Request;
            var auth    = request.Headers.Authorization;

            if (auth == null || auth.Scheme != "Bearer" || string.IsNullOrWhiteSpace(auth.Parameter))
                return Task.FromResult(0); // No token — let AllowAnonymous or RequirePermission handle it

            var principal = JwtHelper.ValidateAccessToken(auth.Parameter);
            if (principal == null)
            {
                context.ErrorResult = new UnauthorizedResult(request);
                return Task.FromResult(0);
            }

            var claims = JwtHelper.ExtractClaims(principal);
            if (claims == null)
            {
                context.ErrorResult = new UnauthorizedResult(request);
                return Task.FromResult(0);
            }

            // Populate TenantContext for use in controllers and downstream filters
            TenantContext.Current = new TenantContext
            {
                UserId       = claims.UserId,
                GroupId      = claims.GroupId,
                SchoolId     = claims.SchoolId,
                TenantDbName = claims.TenantDbName,
                Permissions  = claims.Permissions
            };

            context.Principal = principal;
            return Task.FromResult(0);
        }

        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
            => Task.FromResult(0);
    }

    /// <summary>Helper to return a clean 401 response.</summary>
    internal class UnauthorizedResult : System.Web.Http.IHttpActionResult
    {
        private readonly HttpRequestMessage _request;
        public UnauthorizedResult(HttpRequestMessage request) { _request = request; }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            var response = _request.CreateResponse(HttpStatusCode.Unauthorized,
                new { success = false, message = "Invalid or expired token." });
            return Task.FromResult(response);
        }
    }
}
