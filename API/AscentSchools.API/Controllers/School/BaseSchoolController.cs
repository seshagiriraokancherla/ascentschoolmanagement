using AscentSchools.API.Filters;
using AscentSchools.API.Middleware;
using AscentSchools.Core.Models;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    [SchoolAuth]
    public abstract class BaseSchoolController : ApiController
    {
        protected TenantContext Tenant => TenantContext.Current;

        protected HttpResponseMessage Ok<T>(T data, string message = null)
            => Request.CreateResponse(HttpStatusCode.OK, ApiResponse<T>.Ok(data, message));

        protected HttpResponseMessage Created<T>(T data, string message = null)
            => Request.CreateResponse(HttpStatusCode.Created, ApiResponse<T>.Ok(data, message));

        protected new HttpResponseMessage BadRequest(string message, object errors = null)
            => Request.CreateResponse(HttpStatusCode.BadRequest, ApiResponse.Fail(message, errors));

        protected new HttpResponseMessage NotFound(string message)
            => Request.CreateResponse(HttpStatusCode.NotFound, ApiResponse.Fail(message));

        protected HttpResponseMessage Forbidden(string message)
            => Request.CreateResponse(HttpStatusCode.Forbidden, ApiResponse.Fail(message));

        protected HttpResponseMessage ServerError(string message)
            => Request.CreateResponse(HttpStatusCode.InternalServerError, ApiResponse.Fail(message));
    }
}
