using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    [RoutePrefix("school/dashboard")]
    public class DashboardController : BaseSchoolController
    {
        private readonly DashboardRepository _repo;

        public DashboardController()
        {
            _repo = new DashboardRepository(new TenantConnectionFactory());
        }

        [HttpGet, Route("")]
        public HttpResponseMessage GetDashboard()
            => Ok(_repo.GetDashboard(Tenant.TenantDbName, Tenant.SchoolId));
    }
}
