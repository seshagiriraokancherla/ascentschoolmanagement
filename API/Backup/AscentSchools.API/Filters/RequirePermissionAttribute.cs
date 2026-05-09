using AscentSchools.API.Middleware;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace AscentSchools.API.Filters
{
    /// <summary>
    /// Decorates a controller action with a required permission code.
    /// Usage: [RequirePermission(PermissionCodes.StudentFee.Collect)]
    /// Returns 401 if not authenticated, 403 if authenticated but missing the permission.
    /// </summary>
    public class RequirePermissionAttribute : AuthorizationFilterAttribute
    {
        private readonly string _permissionCode;

        public RequirePermissionAttribute(string permissionCode)
        {
            _permissionCode = permissionCode;
        }

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            var tenant = TenantContext.Current;

            if (tenant == null)
            {
                actionContext.Response = actionContext.Request.CreateResponse(
                    HttpStatusCode.Unauthorized,
                    new { success = false, message = "Authentication required." });
                return;
            }

            if (!tenant.HasPermission(_permissionCode))
            {
                actionContext.Response = actionContext.Request.CreateResponse(
                    HttpStatusCode.Forbidden,
                    new { success = false, message = $"Permission denied: {_permissionCode}" });
            }
        }
    }
}
