using AscentSchools.Core.DTOs.Control.Users;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System.Collections.Generic;
using System.Linq;

namespace AscentSchools.Data.Repositories.Control
{
    /// <summary>
    /// Manages school staff users inside a tenant DB (ascent_group_{id}).
    /// Used by the control app for initial user provisioning and support operations.
    /// All methods require the tenant DB name — derived by caller as ascent_group_{groupId}.
    /// </summary>
    public class TenantUserRepository
    {
        private readonly IConnectionFactory _db;
        public TenantUserRepository(IConnectionFactory db) { _db = db; }

        public IEnumerable<TenantUserDto> GetAll(string tenantDbName)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<TenantUserDto>(
                    @"SELECT u.user_id UserId, u.username Username, u.full_name FullName,
                             u.email Email, u.mobile Mobile, u.status Status, u.created_at CreatedAt,
                             ISNULL(
                                 STUFF((
                                     SELECT ', ' + r.role_name
                                     FROM user_roles ur
                                     JOIN roles r ON r.role_id = ur.role_id
                                     WHERE ur.user_id = u.user_id
                                     FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, ''),
                             '') Roles
                      FROM users u
                      ORDER BY u.user_id");
        }

        public bool UsernameExists(string tenantDbName, string username)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.ExecuteScalar<int>(
                    "SELECT COUNT(1) FROM users WHERE username = @username",
                    new { username }) > 0;
        }

        public IEnumerable<TenantRoleDto> GetRoles(string tenantDbName)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<TenantRoleDto>(
                    @"SELECT role_id RoleId, role_name RoleName, description Description, school_id SchoolId
                      FROM roles WHERE status = 'Active' ORDER BY role_id");
        }

        /// <summary>
        /// Creates a user and assigns them a role across the specified school branches.
        /// password must already be hashed (SHA-256 via JwtHelper.HashRefreshToken).
        /// </summary>
        public int Create(string tenantDbName, CreateTenantUserRequest request, string passwordHash, string createdBy)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var userId = conn.QuerySingle<int>(
                    @"INSERT INTO users (username, password_hash, full_name, email, mobile, created_by)
                      VALUES (@Username, @PasswordHash, @FullName, @Email, @Mobile, @CreatedBy);
                      SELECT CAST(SCOPE_IDENTITY() AS INT)",
                    new { request.Username, PasswordHash = passwordHash,
                          request.FullName, request.Email, request.Mobile, CreatedBy = createdBy });

                if (request.SchoolIds != null && request.SchoolIds.Length > 0)
                {
                    foreach (var schoolId in request.SchoolIds)
                        conn.Execute(
                            "INSERT INTO user_roles (user_id, role_id, school_id) VALUES (@userId, @roleId, @schoolId)",
                            new { userId, request.RoleId, schoolId });
                }

                return userId;
            }
        }

        public void ResetPassword(string tenantDbName, int userId, string newPasswordHash)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                conn.Execute(
                    "UPDATE users SET password_hash = @newPasswordHash WHERE user_id = @userId",
                    new { newPasswordHash, userId });

                // Revoke all active refresh tokens so the user must log in again
                conn.Execute(
                    "UPDATE refresh_tokens SET revoked_at = GETDATE() WHERE user_id = @userId AND revoked_at IS NULL",
                    new { userId });
            }
        }

        public void SetStatus(string tenantDbName, int userId, string status)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    "UPDATE users SET status = @status WHERE user_id = @userId",
                    new { status, userId });
        }
    }
}
