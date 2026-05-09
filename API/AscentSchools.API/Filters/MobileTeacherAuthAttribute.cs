using AscentSchools.API.Middleware;
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace AscentSchools.API.Filters
{
    /// <summary>
    /// Action filter that requires a valid teacher mobile token (tokenType=teacher).
    /// Apply at controller level for all teacher data endpoints.
    /// </summary>
    public class MobileTeacherAuthAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext context)
        {
            if (TeacherContext.Current == null)
            {
                context.Response = context.Request.CreateResponse(
                    HttpStatusCode.Unauthorized,
                    new { success = false, message = "Teacher authentication required." });
            }
        }
    }
}
