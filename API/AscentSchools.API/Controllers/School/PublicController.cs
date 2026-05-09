using AscentSchools.Core.DTOs.Branding;
using AscentSchools.Core.Models;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    /// <summary>Public endpoints — no auth required. Used before login (branding, etc.).</summary>
    [RoutePrefix("")]
    public class PublicController : ApiController
    {
        private readonly SchoolBrandingRepository _branding;

        public PublicController()
        {
            _branding = new SchoolBrandingRepository(new TenantConnectionFactory());
        }

        /// <summary>
        /// GET /branding?schoolId=1
        /// Returns branding for the subdomain (and optionally a specific branch).
        /// Public — no authentication needed.
        /// </summary>
        [HttpGet, Route("branding"), AllowAnonymous]
        public HttpResponseMessage GetBranding([FromUri] int? schoolId = null)
        {
            IEnumerable<string> vals;
            var subdomain = Request.Headers.TryGetValues("X-Subdomain", out vals) ? vals.FirstOrDefault() : null;

            if (string.IsNullOrWhiteSpace(subdomain))
                return Request.CreateResponse(HttpStatusCode.BadRequest, ApiResponse.Fail("X-Subdomain header is required."));

            var result = _branding.GetBySubdomain(subdomain, schoolId);
            if (result == null)
                return Request.CreateResponse(HttpStatusCode.NotFound, ApiResponse.Fail("School not found."));

            return Request.CreateResponse(HttpStatusCode.OK, ApiResponse<BrandingResponse>.Ok(result));
        }
    }
}
