using AscentSchools.API.Middleware;
using AscentSchools.Core.Models;
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace AscentSchools.API.Filters
{
    /// <summary>
    /// Authorization filter for all school app endpoints.
    /// Ensures the request carries a valid school JWT (tokenType=school)
    /// and that TenantContext has been populated by JwtAuthenticationFilter.
    /// </summary>
    public class SchoolAuthAttribute : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            if (TenantContext.Current == null)
            {
                actionContext.Response = actionContext.Request.CreateResponse(
                    HttpStatusCode.Unauthorized,
                    ApiResponse.Fail("Authentication required."));
            }
        }
    }
}
