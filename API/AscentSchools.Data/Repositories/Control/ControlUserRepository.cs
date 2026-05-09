using AscentSchools.Core.DTOs.Control.Auth;
using AscentSchools.Data.ConnectionFactory;
using Dapper;

namespace AscentSchools.Data.Repositories.Control
{
    public class ControlUserRepository
    {
        private readonly IConnectionFactory _db;
        public ControlUserRepository(IConnectionFactory db) { _db = db; }

        public ControlUserRecord GetByUsername(string username)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.QueryFirstOrDefault<ControlUserRecord>(
                    @"SELECT user_id UserId, full_name FullName, username Username,
                             password_hash PasswordHash, role Role, status Status
                      FROM control_users WHERE username = @username",
                    new { username });
        }

        public ControlUserRecord GetById(int userId)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.QueryFirstOrDefault<ControlUserRecord>(
                    @"SELECT user_id UserId, full_name FullName, username Username,
                             password_hash PasswordHash, role Role, status Status
                      FROM control_users WHERE user_id = @userId",
                    new { userId });
        }

        public void UpdateLastLogin(int userId)
        {
            using (var conn = _db.GetMasterConnection())
                conn.Execute(
                    "UPDATE control_users SET last_login_at = GETDATE() WHERE user_id = @userId",
                    new { userId });
        }
    }
}
