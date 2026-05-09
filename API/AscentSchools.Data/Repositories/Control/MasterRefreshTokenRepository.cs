using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System;

namespace AscentSchools.Data.Repositories.Control
{
    public class MasterRefreshTokenRepository
    {
        private readonly IConnectionFactory _db;
        public MasterRefreshTokenRepository(IConnectionFactory db) { _db = db; }

        public void Create(int userId, string tokenHash, DateTime expiresAt)
        {
            using (var conn = _db.GetMasterConnection())
                conn.Execute(
                    "INSERT INTO refresh_tokens (user_id, token_hash, expires_at) VALUES (@userId, @tokenHash, @expiresAt)",
                    new { userId, tokenHash, expiresAt });
        }

        public RefreshTokenRecord GetByHash(string tokenHash)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.QueryFirstOrDefault<RefreshTokenRecord>(
                    @"SELECT token_id TokenId, user_id UserId, expires_at ExpiresAt, revoked_at RevokedAt
                      FROM refresh_tokens WHERE token_hash = @tokenHash",
                    new { tokenHash });
        }

        public void Revoke(int tokenId, string replacedByHash = null)
        {
            using (var conn = _db.GetMasterConnection())
                conn.Execute(
                    "UPDATE refresh_tokens SET revoked_at = GETDATE(), replaced_by = @replacedByHash WHERE token_id = @tokenId",
                    new { tokenId, replacedByHash });
        }

        public void RevokeAllForUser(int userId)
        {
            using (var conn = _db.GetMasterConnection())
                conn.Execute(
                    "UPDATE refresh_tokens SET revoked_at = GETDATE() WHERE user_id = @userId AND revoked_at IS NULL",
                    new { userId });
        }
    }

    public class RefreshTokenRecord
    {
        public int       TokenId   { get; set; }
        public int       UserId    { get; set; }
        public DateTime  ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
    }
}
