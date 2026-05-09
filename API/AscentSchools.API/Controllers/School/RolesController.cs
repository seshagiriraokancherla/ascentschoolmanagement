using AscentSchools.Core.DTOs.School.Rbac;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    [RoutePrefix("school/roles")]
    public class RolesController : BaseSchoolController
    {
        private readonly SchoolRbacRepository _rbac;

        public RolesController()
        {
            _rbac = new SchoolRbacRepository(new TenantConnectionFactory());
        }

        // GET school/roles
        [HttpGet, Route("")]
        public HttpResponseMessage GetRoles()
        {
            var roles = _rbac.GetRoles(Tenant.TenantDbName);
            return Ok(roles);
        }

        // POST school/roles
        [HttpPost, Route("")]
        public HttpResponseMessage CreateRole([FromBody] CreateSchoolRoleRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RoleName))
                return BadRequest("Role name is required.");

            var roleId = _rbac.CreateRole(Tenant.TenantDbName, request);
            var roles  = _rbac.GetRoles(Tenant.TenantDbName);
            return Created(roles, "Role created.");
        }

        // PUT school/roles/{roleId}
        [HttpPut, Route("{roleId:int}")]
        public HttpResponseMessage UpdateRole(int roleId, [FromBody] UpdateSchoolRoleRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RoleName))
                return BadRequest("Role name is required.");

            _rbac.UpdateRole(Tenant.TenantDbName, roleId, request);
            return Ok(_rbac.GetRoles(Tenant.TenantDbName));
        }

        // GET school/roles/permissions — all available permissions
        [HttpGet, Route("permissions")]
        public HttpResponseMessage GetAllPermissions()
        {
            var permissions = _rbac.GetAllPermissions(Tenant.TenantDbName);
            return Ok(permissions);
        }

        // GET school/roles/{roleId}/permissions — IDs assigned to this role
        [HttpGet, Route("{roleId:int}/permissions")]
        public HttpResponseMessage GetRolePermissions(int roleId)
        {
            var ids = _rbac.GetRolePermissionIds(Tenant.TenantDbName, roleId);
            return Ok(ids);
        }

        // PUT school/roles/{roleId}/permissions — replace full permission set
        [HttpPut, Route("{roleId:int}/permissions")]
        public HttpResponseMessage SetRolePermissions(int roleId, [FromBody] RolePermissionsRequest request)
        {
            _rbac.SetRolePermissions(Tenant.TenantDbName, roleId, request?.PermissionIds ?? new int[0]);
            return Ok(_rbac.GetRolePermissionIds(Tenant.TenantDbName, roleId));
        }
    }
}
