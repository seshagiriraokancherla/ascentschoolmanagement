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

        /// <summary>
        /// GET /version — API assembly version + deploy date (bin DLL last-write time).
        /// Public — used by the app footer to show which API build is live.
        /// </summary>
        [HttpGet, Route("version"), AllowAnonymous]
        public HttpResponseMessage GetVersion()
        {
            var asm     = System.Reflection.Assembly.GetExecutingAssembly();
            var version = asm.GetName().Version.ToString();

            string buildDate = null;
            try
            {
                // The bin DLL's write time = when it was deployed (FTP upload). More accurate
                // than the shadow-copied asm.Location.
                var binPath = System.Web.Hosting.HostingEnvironment.MapPath("~/bin/AscentSchools.API.dll");
                if (binPath != null && System.IO.File.Exists(binPath))
                    // UTC so it doesn't depend on the server's local timezone.
                    buildDate = System.IO.File.GetLastWriteTimeUtc(binPath).ToString("yyyy-MM-dd HH:mm") + " UTC";
            }
            catch { /* best-effort */ }

            return Request.CreateResponse(HttpStatusCode.OK,
                ApiResponse<object>.Ok(new { version, buildDate }));
        }
    }
}
