using AscentSchools.Core.DTOs.School.Settings;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    [RoutePrefix("school/settings")]
    public class SchoolSettingsController : BaseSchoolController
    {
        private readonly SchoolSettingsRepository _repo;

        public SchoolSettingsController()
        {
            _repo = new SchoolSettingsRepository(new TenantConnectionFactory());
        }

        // GET school/settings
        [HttpGet, Route("")]
        public System.Net.Http.HttpResponseMessage Get()
        {
            var settings = _repo.Get(Tenant.SchoolId);
            return Ok(settings);
        }

        // PUT school/settings
        [HttpPut, Route("")]
        public System.Net.Http.HttpResponseMessage Update([FromBody] UpdateSchoolSettingsRequest request)
        {
            if (request == null) return BadRequest("Request body required.");
            _repo.Upsert(Tenant.SchoolId, request, Tenant.FullName);
            return Ok(_repo.Get(Tenant.SchoolId));
        }
    }
}
