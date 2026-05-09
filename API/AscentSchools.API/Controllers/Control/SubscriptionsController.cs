using AscentSchools.Core.DTOs.Control.Subscriptions;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.Control;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.Control
{
    [RoutePrefix("control/school-groups")]
    public class SubscriptionsController : BaseControlController
    {
        private readonly SubscriptionRepository _subs;
        private readonly SchoolGroupRepository  _groups;

        public SubscriptionsController()
        {
            var db  = new TenantConnectionFactory();
            _subs   = new SubscriptionRepository(db);
            _groups = new SchoolGroupRepository(db);
        }

        // GET control/school-groups/{id}/subscription
        [HttpGet, Route("{id:int}/subscription")]
        public HttpResponseMessage Get(int id)
        {
            if (_groups.GetById(id) == null) return NotFound("School group not found.");
            var sub = _subs.GetByGroupId(id);
            return Ok(sub); // may be null if not yet set
        }

        // PUT control/school-groups/{id}/subscription
        [HttpPut, Route("{id:int}/subscription")]
        public HttpResponseMessage Upsert(int id, [FromBody] UpdateSubscriptionRequest request)
        {
            if (request == null) return BadRequest("Request body required.");
            if (_groups.GetById(id) == null) return NotFound("School group not found.");

            _subs.Upsert(id, request);
            return Ok(_subs.GetByGroupId(id));
        }
    }
}
