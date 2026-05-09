using AscentSchools.Core.DTOs.School.Auth;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System;
using System.Collections.Generic;

namespace AscentSchools.Data.Repositories.School
{
    public class SchoolAuthRepository
    {
        private readonly IConnectionFactory _db;
        public SchoolAuthRepository(IConnectionFactory db) { _db = db; }

        public SchoolUserRecord GetById(string tenantDbName, int userId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QueryFirstOrDefault<SchoolUserRecord>(
                    @"SELECT user_id UserId, username Username, full_name FullName,
                             password_hash PasswordHash, status Status, school_id SchoolId
                      FROM users WHERE user_id = @userId",
                    new { userId });
        }

        public SchoolUserRecord GetByUsername(string tenantDbName, string username)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QueryFirstOrDefault<SchoolUserRecord>(
                    @"SELECT user_id UserId, username Username, full_name FullName,
                             password_hash PasswordHash, status Status, school_id SchoolId
                      FROM users WHERE username = @username",
                    new { username });
        }

        /// <summary>Returns distinct school IDs this user has role assignments for.</summary>
        public IEnumerable<int> GetUserSchoolIds(string tenantDbName, int userId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<int>(
                    "SELECT DISTINCT school_id FROM user_roles WHERE user_id = @userId",
                    new { userId });
        }

        /// <summary>Returns all permission codes for a user at a specific school branch.</summary>
        public IEnumerable<string> GetUserPermissions(string tenantDbName, int userId, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<string>(
                    @"SELECT DISTINCT p.permission_code
                      FROM user_roles ur
                      JOIN role_permissions rp ON rp.role_id = ur.role_id
                      JOIN permissions p       ON p.permission_id = rp.permission_id
                      WHERE ur.user_id = @userId AND ur.school_id = @schoolId
                      AND p.status = 'Active'",
                    new { userId, schoolId });
        }

        public void UpdateLastLogin(string tenantDbName, int userId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    "UPDATE users SET last_login_at = GETDATE() WHERE user_id = @userId",
                    new { userId });
        }

        // ── Refresh tokens ────────────────────────────────────────────────

        public void CreateRefreshToken(string tenantDbName, int userId, string tokenHash, DateTime expiresAt)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    "INSERT INTO refresh_tokens (user_id, token_hash, expires_at) VALUES (@userId, @tokenHash, @expiresAt)",
                    new { userId, tokenHash, expiresAt });
        }

        public SchoolRefreshTokenRecord GetRefreshToken(string tenantDbName, string tokenHash)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QueryFirstOrDefault<SchoolRefreshTokenRecord>(
                    @"SELECT token_id TokenId, user_id UserId, expires_at ExpiresAt, revoked_at RevokedAt
                      FROM refresh_tokens WHERE token_hash = @tokenHash",
                    new { tokenHash });
        }

        public void RevokeRefreshToken(string tenantDbName, int tokenId, string replacedByHash = null)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    "UPDATE refresh_tokens SET revoked_at = GETDATE(), replaced_by = @replacedByHash WHERE token_id = @tokenId",
                    new { tokenId, replacedByHash });
        }
    }

    public class SchoolRefreshTokenRecord
    {
        public int       TokenId   { get; set; }
        public int       UserId    { get; set; }
        public DateTime  ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
    }
}
