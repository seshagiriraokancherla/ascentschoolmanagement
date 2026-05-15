using AscentSchools.Core.DTOs.School.Rbac;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System.Collections.Generic;
using System.Linq;

namespace AscentSchools.Data.Repositories.School
{
    public class SchoolRbacRepository
    {
        private readonly IConnectionFactory _db;
        public SchoolRbacRepository(IConnectionFactory db) { _db = db; }

        // ── Roles ─────────────────────────────────────────────────────────

        public IEnumerable<SchoolRoleDto> GetRoles(string tenantDbName)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<SchoolRoleDto>(
                    @"SELECT r.role_id RoleId, r.role_name RoleName, r.description Description,
                             r.school_id SchoolId,
                             COUNT(rp.permission_id) PermissionCount
                      FROM roles r
                      LEFT JOIN role_permissions rp ON rp.role_id = r.role_id
                      WHERE r.status = 'Active'
                      GROUP BY r.role_id, r.role_name, r.description, r.school_id
                      ORDER BY r.role_id");
        }

        public int CreateRole(string tenantDbName, CreateSchoolRoleRequest request)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QuerySingle<int>(
                    @"INSERT INTO roles (role_name, description, school_id)
                      VALUES (@RoleName, @Description, @SchoolId);
                      SELECT CAST(SCOPE_IDENTITY() AS INT)",
                    request);
        }

        public void UpdateRole(string tenantDbName, int roleId, UpdateSchoolRoleRequest request)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    "UPDATE roles SET role_name = @RoleName, description = @Description WHERE role_id = @roleId",
                    new { request.RoleName, request.Description, roleId });
        }

        // ── Permissions ───────────────────────────────────────────────────

        public IEnumerable<SchoolPermissionDto> GetAllPermissions(string tenantDbName)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<SchoolPermissionDto>(
                    @"SELECT permission_id PermissionId, permission_code PermissionCode, description Description
                      FROM permissions WHERE status = 'Active'
                      ORDER BY permission_code");
        }

        public IEnumerable<int> GetRolePermissionIds(string tenantDbName, int roleId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<int>(
                    "SELECT permission_id FROM role_permissions WHERE role_id = @roleId",
                    new { roleId });
        }

        /// <summary>Replaces all permissions for a role atomically.</summary>
        public void SetRolePermissions(string tenantDbName, int roleId, int[] permissionIds)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        conn.Execute(
                            "DELETE FROM role_permissions WHERE role_id = @roleId",
                            new { roleId }, tx);

                        if (permissionIds != null && permissionIds.Length > 0)
                            conn.Execute(
                                "INSERT INTO role_permissions (role_id, permission_id) VALUES (@roleId, @PermissionId)",
                                permissionIds.Select(pid => new { roleId, PermissionId = pid }),
                                tx);

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}
