using AscentSchools.Core.DTOs.Control.Branding;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.Control;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.Control
{
    [RoutePrefix("control/school-groups")]
    public class BrandingController : BaseControlController
    {
        private readonly BrandingRepository    _branding;
        private readonly SchoolGroupRepository _groups;

        public BrandingController()
        {
            var db    = new TenantConnectionFactory();
            _branding = new BrandingRepository(db);
            _groups   = new SchoolGroupRepository(db);
        }

        // GET control/school-groups/{id}/branding
        [HttpGet, Route("{id:int}/branding")]
        public HttpResponseMessage Get(int id)
        {
            if (_groups.GetById(id) == null) return NotFound("School group not found.");
            return Ok(_branding.GetGroupBranding(id));
        }

        // PUT control/school-groups/{id}/branding
        [HttpPut, Route("{id:int}/branding")]
        public HttpResponseMessage Upsert(int id, [FromBody] UpdateGroupBrandingRequest request)
        {
            if (request == null) return BadRequest("Request body required.");
            if (_groups.GetById(id) == null) return NotFound("School group not found.");

            _branding.UpsertGroupBranding(id, request);
            return Ok(_branding.GetGroupBranding(id));
        }
    }
}
