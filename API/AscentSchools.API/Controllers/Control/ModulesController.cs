using AscentSchools.API.Filters;
using AscentSchools.Core.DTOs.Control.Modules;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.Control;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.Control
{
    [RoutePrefix("control")]
    public class ModulesController : BaseControlController
    {
        private readonly ModuleRepository      _modules;
        private readonly SchoolGroupRepository _groups;

        public ModulesController()
        {
            var db  = new TenantConnectionFactory();
            _modules = new ModuleRepository(db);
            _groups  = new SchoolGroupRepository(db);
        }

        // GET control/modules — master list
        [HttpGet, Route("modules")]
        public HttpResponseMessage GetAll() => Ok(_modules.GetAll());

        // GET control/school-groups/{id}/modules
        [HttpGet, Route("school-groups/{id:int}/modules")]
        public HttpResponseMessage GetGroupModules(int id)
        {
            if (_groups.GetById(id) == null) return NotFound("School group not found.");
            return Ok(_modules.GetGroupModules(id));
        }

        // PUT control/school-groups/{id}/modules/{moduleId}  — super_admin only
        [HttpPut, Route("school-groups/{id:int}/modules/{moduleId:int}")]
        [ControlAuth("super_admin")]
        public HttpResponseMessage UpdateGroupModule(int id, int moduleId, [FromBody] UpdateGroupModuleRequest request)
        {
            if (request == null) return BadRequest("Request body required.");
            if (_groups.GetById(id) == null) return NotFound("School group not found.");

            _modules.UpdateGroupModule(id, moduleId, request.IsEnabled);
            return Ok(_modules.GetGroupModules(id));
        }
    }
}
