using AscentSchools.API.Helpers;
using AscentSchools.Core.DTOs.Control.SchoolGroups;
using AscentSchools.Core.DTOs.Control.Users;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.Control;
using System.Linq;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.Control
{
    [RoutePrefix("control/school-groups")]
    public class SchoolGroupsController : BaseControlController
    {
        private readonly SchoolGroupRepository _groups;
        private readonly SchoolRepository      _schools;
        private readonly TenantUserRepository  _tenantUsers;

        public SchoolGroupsController()
        {
            var db       = new TenantConnectionFactory();
            _groups      = new SchoolGroupRepository(db);
            _schools     = new SchoolRepository(db);
            _tenantUsers = new TenantUserRepository(db);
        }

        /// <summary>
        /// Resolves the tenant DB name from the stored db_name column.
        /// Returns null (and sets an error response) if the group doesn't exist or db_name is not set.
        /// </summary>
        private string ResolveTenantDb(int groupId, out HttpResponseMessage error)
        {
            var group = _groups.GetById(groupId);
            if (group == null)            { error = NotFound("School group not found."); return null; }
            if (string.IsNullOrWhiteSpace(group.DbName))
                                          { error = ServerError("Tenant database name is not configured for this group."); return null; }
            error = null;
            return group.DbName;
        }

        // GET control/school-groups
        [HttpGet, Route("")]
        public HttpResponseMessage GetAll()
        {
            var groups = _groups.GetAll();
            return Ok(groups);
        }

        // GET control/school-groups/{id}
        [HttpGet, Route("{id:int}")]
        public HttpResponseMessage GetById(int id)
        {
            var group = _groups.GetById(id);
            if (group == null) return NotFound("School group not found.");
            return Ok(group);
        }

        // POST control/school-groups
        [HttpPost, Route("")]
        public HttpResponseMessage Create([FromBody] CreateSchoolGroupRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.GroupName) || string.IsNullOrWhiteSpace(request.Subdomain))
                return BadRequest("GroupName and Subdomain are required.");

            if (_groups.SubdomainExists(request.Subdomain))
                return BadRequest("Subdomain already in use.");

            var groupId = _groups.Create(request);
            var created = _groups.GetById(groupId);
            return Created(created, $"School group created. Provision the tenant database manually: run tenant_tables.sql then seed_tenant_data.sql on 'ascent_group_{groupId}'.");
        }

        // PUT control/school-groups/{id}
        [HttpPut, Route("{id:int}")]
        public HttpResponseMessage Update(int id, [FromBody] UpdateSchoolGroupRequest request)
        {
            if (request == null) return BadRequest("Request body required.");
            if (_groups.GetById(id) == null) return NotFound("School group not found.");

            _groups.Update(id, request);
            var updated = _groups.GetById(id);
            // Invalidate credential cache so next tenant connection picks up new credentials
            if (!string.IsNullOrEmpty(updated?.DbName))
                TenantConnectionFactory.InvalidateCredentialCache(updated.DbName);
            return Ok(updated);
        }

        // GET control/school-groups/{id}/schools
        [HttpGet, Route("{id:int}/schools")]
        public HttpResponseMessage GetSchools(int id)
        {
            if (_groups.GetById(id) == null) return NotFound("School group not found.");
            return Ok(_schools.GetByGroupId(id));
        }

        // POST control/school-groups/{id}/schools
        [HttpPost, Route("{id:int}/schools")]
        public HttpResponseMessage CreateSchool(int id, [FromBody] AscentSchools.Core.DTOs.Control.Schools.CreateSchoolRequest request)
        {
            if (_groups.GetById(id) == null) return NotFound("School group not found.");
            if (request == null || string.IsNullOrWhiteSpace(request.SchoolName))
                return BadRequest("SchoolName is required.");

            var schoolId = _schools.Create(id, request);
            return Created(_schools.GetById(schoolId));
        }

        // PUT control/school-groups/{id}/schools/{schoolId}
        [HttpPut, Route("{id:int}/schools/{schoolId:int}")]
        public HttpResponseMessage UpdateSchool(int id, int schoolId, [FromBody] AscentSchools.Core.DTOs.Control.Schools.UpdateSchoolRequest request)
        {
            if (request == null) return BadRequest("Request body required.");
            var school = _schools.GetById(schoolId);
            if (school == null || school.GroupId != id) return NotFound("School not found.");

            _schools.Update(schoolId, request);
            return Ok(_schools.GetById(schoolId));
        }

        // GET control/school-groups/{id}/schools/{schoolId}/api-key
        [HttpGet, Route("{id:int}/schools/{schoolId:int}/api-key")]
        public HttpResponseMessage GetApiKey(int id, int schoolId)
        {
            var school = _schools.GetById(schoolId);
            if (school == null || school.GroupId != id) return NotFound("School not found.");

            var key = _schools.GetApiKey(schoolId);
            return Ok(new { apiKey = key ?? "" });
        }

        // POST control/school-groups/{id}/schools/{schoolId}/api-key/generate
        [HttpPost, Route("{id:int}/schools/{schoolId:int}/api-key/generate")]
        public HttpResponseMessage GenerateApiKey(int id, int schoolId)
        {
            var school = _schools.GetById(schoolId);
            if (school == null || school.GroupId != id) return NotFound("School not found.");

            var key = _schools.GenerateApiKey(schoolId);
            return Ok(new { apiKey = key });
        }

        // ── Tenant Users ─────────────────────────────────────────────────

        // GET control/school-groups/{id}/roles  — tenant DB roles for dropdown
        [HttpGet, Route("{id:int}/roles")]
        public HttpResponseMessage GetRoles(int id)
        {
            HttpResponseMessage error;
            var tenantDb = ResolveTenantDb(id, out error);
            if (tenantDb == null) return error;
            return Ok(_tenantUsers.GetRoles(tenantDb));
        }

        // GET control/school-groups/{id}/users
        [HttpGet, Route("{id:int}/users")]
        public HttpResponseMessage GetUsers(int id)
        {
            HttpResponseMessage error;
            var tenantDb = ResolveTenantDb(id, out error);
            if (tenantDb == null) return error;
            return Ok(_tenantUsers.GetAll(tenantDb));
        }

        // POST control/school-groups/{id}/users
        [HttpPost, Route("{id:int}/users")]
        public HttpResponseMessage CreateUser(int id, [FromBody] CreateTenantUserRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.FullName)
                                || string.IsNullOrWhiteSpace(request.Username)
                                || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("FullName, Username and Password are required.");

            if (request.RoleId <= 0)
                return BadRequest("A role must be selected.");

            if (request.SchoolIds == null || request.SchoolIds.Length == 0)
                return BadRequest("At least one school (branch) must be selected.");

            HttpResponseMessage error;
            var tenantDb = ResolveTenantDb(id, out error);
            if (tenantDb == null) return error;

            if (_tenantUsers.UsernameExists(tenantDb, request.Username))
                return BadRequest($"Username '{request.Username}' is already taken.");

            // Validate all provided schoolIds belong to this group
            var groupSchools = _schools.GetByGroupId(id).Select(s => s.SchoolId).ToHashSet();
            var invalid = request.SchoolIds.Where(sid => !groupSchools.Contains(sid)).ToArray();
            if (invalid.Length > 0)
                return BadRequest($"School IDs {string.Join(", ", invalid)} do not belong to this group.");

            var passwordHash = JwtHelper.HashRefreshToken(request.Password);
            var userId       = _tenantUsers.Create(tenantDb, request, passwordHash, Control.FullName);

            return Created(new { userId }, "User created successfully.");
        }

        // PUT control/school-groups/{id}/users/{userId}/reset-password
        [HttpPut, Route("{id:int}/users/{userId:int}/reset-password")]
        public HttpResponseMessage ResetPassword(int id, int userId, [FromBody] ResetPasswordRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest("NewPassword is required.");

            HttpResponseMessage error;
            var tenantDb = ResolveTenantDb(id, out error);
            if (tenantDb == null) return error;

            var hash = JwtHelper.HashRefreshToken(request.NewPassword);
            _tenantUsers.ResetPassword(tenantDb, userId, hash);
            return Ok<object>(null, "Password reset. User will need to log in again.");
        }

        // PUT control/school-groups/{id}/users/{userId}/status
        [HttpPut, Route("{id:int}/users/{userId:int}/status")]
        public HttpResponseMessage SetUserStatus(int id, int userId, [FromBody] SetUserStatusRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Status))
                return BadRequest("Status is required.");

            HttpResponseMessage error;
            var tenantDb = ResolveTenantDb(id, out error);
            if (tenantDb == null) return error;

            _tenantUsers.SetStatus(tenantDb, userId, request.Status);
            return Ok<object>(null, $"User status set to {request.Status}.");
        }
    }
}
