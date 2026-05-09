using AscentSchools.Core.DTOs.Control.SchoolGroups;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System.Collections.Generic;

namespace AscentSchools.Data.Repositories.Control
{
    public class SchoolGroupRepository
    {
        private readonly IConnectionFactory _db;
        public SchoolGroupRepository(IConnectionFactory db) { _db = db; }

        public IEnumerable<SchoolGroupDto> GetAll()
        {
            using (var conn = _db.GetMasterConnection())
                return conn.Query<SchoolGroupDto>(
                    @"SELECT group_id GroupId, group_name GroupName, subdomain Subdomain,
                             db_name DbName, db_username DbUsername, db_password DbPassword,
                             description Description, status Status, created_at CreatedAt
                      FROM school_groups ORDER BY group_id");
        }

        public SchoolGroupDto GetById(int groupId)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.QueryFirstOrDefault<SchoolGroupDto>(
                    @"SELECT group_id GroupId, group_name GroupName, subdomain Subdomain,
                             db_name DbName, db_username DbUsername, db_password DbPassword,
                             description Description, status Status, created_at CreatedAt
                      FROM school_groups WHERE group_id = @groupId",
                    new { groupId });
        }

        public bool SubdomainExists(string subdomain)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.ExecuteScalar<int>(
                    "SELECT COUNT(1) FROM school_groups WHERE subdomain = @subdomain",
                    new { subdomain }) > 0;
        }

        /// <summary>
        /// Creates the school group record in master DB and seeds default group_modules.
        /// Tenant DB must be provisioned manually using tenant_tables.sql + seed_tenant_data.sql.
        /// After creation, set db_name manually: UPDATE school_groups SET db_name = 'ascent_group_{id}' WHERE group_id = {id}
        /// </summary>
        public int Create(CreateSchoolGroupRequest request)
        {
            int groupId;
            using (var conn = _db.GetMasterConnection())
                groupId = conn.QuerySingle<int>(
                    @"INSERT INTO school_groups (group_name, subdomain, description)
                      VALUES (@GroupName, @Subdomain, @Description);
                      SELECT CAST(SCOPE_IDENTITY() AS INT)",
                    request);

            // Seed all modules as enabled for this new group
            using (var conn = _db.GetMasterConnection())
                conn.Execute(
                    @"INSERT INTO group_modules (group_id, module_id, is_enabled)
                      SELECT @groupId, module_id, 1 FROM modules WHERE status = 'Active'",
                    new { groupId });

            return groupId;
        }

        /// <summary>
        /// Returns the tenant info (group_id + db_name) for an active subdomain.
        /// Returns null if subdomain not found or group is inactive.
        /// Login fails with a clear error if db_name is not yet set.
        /// </summary>
        public TenantInfo GetTenantBySubdomain(string subdomain)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.QueryFirstOrDefault<TenantInfo>(
                    @"SELECT group_id GroupId, db_name DbName
                      FROM school_groups
                      WHERE subdomain = @subdomain AND status = 'Active'",
                    new { subdomain });
        }

        public void Update(int groupId, UpdateSchoolGroupRequest request)
        {
            using (var conn = _db.GetMasterConnection())
                conn.Execute(
                    @"UPDATE school_groups
                      SET group_name = @GroupName, description = @Description, status = @Status,
                          db_name = @DbName, db_username = @DbUsername, db_password = @DbPassword
                      WHERE group_id = @groupId",
                    new { request.GroupName, request.Description, request.Status,
                          request.DbName, request.DbUsername, request.DbPassword, groupId });
        }
    }

    /// <summary>Tenant routing info resolved from subdomain at login.</summary>
    public class TenantInfo
    {
        public int    GroupId { get; set; }
        public string DbName  { get; set; }
    }
}
