using AscentSchools.API.Helpers;
using AscentSchools.API.Middleware;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace AscentSchools.API.Filters
{
    /// <summary>
    /// Authorization filter for all control app endpoints.
    /// Validates that the JWT is a control token (tokenType=control).
    /// Optionally enforces specific roles: [ControlAuth("super_admin")]
    /// </summary>
    public class ControlAuthAttribute : AuthorizationFilterAttribute
    {
        private readonly string[] _roles;
        public ControlAuthAttribute(params string[] roles) { _roles = roles; }

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            var request = actionContext.Request;
            var auth    = request.Headers.Authorization;

            if (auth == null || auth.Scheme != "Bearer" || string.IsNullOrWhiteSpace(auth.Parameter))
            {
                actionContext.Response = Deny(request, HttpStatusCode.Unauthorized, "Authentication required.");
                return;
            }

            var principal = JwtHelper.ValidateAccessToken(auth.Parameter);
            if (principal == null)
            {
                actionContext.Response = Deny(request, HttpStatusCode.Unauthorized, "Invalid or expired token.");
                return;
            }

            var claims    = principal.Claims.ToList();
            var tokenType = claims.FirstOrDefault(c => c.Type == "tokenType")?.Value;
            if (tokenType != "control")
            {
                actionContext.Response = Deny(request, HttpStatusCode.Unauthorized, "Invalid token type.");
                return;
            }

            var role = claims.FirstOrDefault(c => c.Type == "role")?.Value;
            if (_roles != null && _roles.Length > 0 && !_roles.Contains(role))
            {
                actionContext.Response = Deny(request, HttpStatusCode.Forbidden, "Insufficient permissions.");
                return;
            }

            int.TryParse(claims.FirstOrDefault(c => c.Type == "userId")?.Value, out var userId);
            ControlContext.Current = new ControlContext
            {
                UserId   = userId,
                FullName = claims.FirstOrDefault(c => c.Type == "fullName")?.Value,
                Role     = role
            };
        }

        private static HttpResponseMessage Deny(HttpRequestMessage req, HttpStatusCode code, string msg)
            => req.CreateResponse(code, new { success = false, message = msg });
    }
}
