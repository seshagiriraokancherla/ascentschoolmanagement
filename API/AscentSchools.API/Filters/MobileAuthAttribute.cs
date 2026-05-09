using AscentSchools.API.Middleware;
using AscentSchools.Core.Models;
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace AscentSchools.API.Filters
{
    /// <summary>
    /// Authorization filter for mobile app endpoints.
    /// Accepts tokenType=student or tokenType=parent.
    /// Set requireChildContext=true for data endpoints that need a selected child.
    /// </summary>
    public class MobileAuthAttribute : AuthorizationFilterAttribute
    {
        private readonly bool _requireChildContext;

        public MobileAuthAttribute(bool requireChildContext = false)
        {
            _requireChildContext = requireChildContext;
        }

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            var ctx = MobileContext.Current;

            if (ctx == null)
            {
                actionContext.Response = actionContext.Request.CreateResponse(
                    HttpStatusCode.Unauthorized,
                    ApiResponse.Fail("Mobile authentication required."));
                return;
            }

            if (_requireChildContext && !ctx.HasChildContext)
            {
                actionContext.Response = actionContext.Request.CreateResponse(
                    HttpStatusCode.Unauthorized,
                    ApiResponse.Fail("Please select a child first."));
            }
        }
    }
}
