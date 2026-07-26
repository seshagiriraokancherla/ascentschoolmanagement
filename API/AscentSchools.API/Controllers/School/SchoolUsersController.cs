using AscentSchools.API.Helpers;
using AscentSchools.Core.DTOs.Control.Users;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.Control;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    [RoutePrefix("school/users")]
    public class SchoolUsersController : BaseSchoolController
    {
        private readonly TenantUserRepository _users;
        private readonly SchoolRepository     _schools;

        public SchoolUsersController()
        {
            var db   = new TenantConnectionFactory();
            _users   = new TenantUserRepository(db);
            _schools = new SchoolRepository(db);
        }

        // GET school/users
        [HttpGet, Route("")]
        public HttpResponseMessage GetUsers()
        {
            var users = _users.GetAll(Tenant.TenantDbName);
            return Ok(users);
        }

        // GET school/users/roles — for create-user dropdown
        [HttpGet, Route("roles")]
        public HttpResponseMessage GetRoles()
        {
            var roles = _users.GetRoles(Tenant.TenantDbName);
            return Ok(roles);
        }

        // GET school/users/schools — for branch assignment dropdown
        [HttpGet, Route("schools")]
        public HttpResponseMessage GetSchools()
        {
            var schools = _schools.GetByGroupId(Tenant.GroupId);
            return Ok(schools);
        }

        // GET school/users/staff — Active staff without a login (create-user dropdown)
        [HttpGet, Route("staff")]
        public HttpResponseMessage GetAssignableStaff()
        {
            var staff = _users.GetAssignableStaff(Tenant.TenantDbName, Tenant.SchoolId);
            return Ok(staff);
        }

        // POST school/users — school-app users are always created from a staff record.
        [HttpPost, Route("")]
        public HttpResponseMessage CreateUser([FromBody] CreateTenantUserRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Username and password are required.");

            if (request.EmployeeId == null || request.EmployeeId <= 0)
                return BadRequest("A staff member is required.");

            if (!_users.IsStaffAvailableForLogin(Tenant.TenantDbName, Tenant.SchoolId, request.EmployeeId.Value))
                return BadRequest("The selected staff member is invalid or already has a login.");

            if (_users.UsernameExists(Tenant.TenantDbName, request.Username))
                return BadRequest("Username already taken.");

            var hash = JwtHelper.HashRefreshToken(request.Password);
            _users.Create(Tenant.TenantDbName, request, hash, Tenant.UserId.ToString());
            return Created(_users.GetAll(Tenant.TenantDbName), "User created.");
        }

        // PUT school/users/{userId}/reset-password
        [HttpPut, Route("{userId:int}/reset-password")]
        public HttpResponseMessage ResetPassword(int userId, [FromBody] ResetPasswordRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest("New password is required.");

            _users.ResetPassword(Tenant.TenantDbName, userId, JwtHelper.HashRefreshToken(request.NewPassword));
            return Ok<object>(null, "Password reset. User will need to log in again.");
        }

        // PUT school/users/{userId}/status
        [HttpPut, Route("{userId:int}/status")]
        public HttpResponseMessage SetStatus(int userId, [FromBody] SetUserStatusRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Status))
                return BadRequest("Status is required.");

            _users.SetStatus(Tenant.TenantDbName, userId, request.Status);
            return Ok(_users.GetAll(Tenant.TenantDbName));
        }
    }
}
