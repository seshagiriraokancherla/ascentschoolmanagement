using AscentSchools.Core.DTOs.Mobile.AppConfig;
using AscentSchools.Data.ConnectionFactory;
using Dapper;

namespace AscentSchools.Data.Repositories.Mobile
{
    /// <summary>
    /// Reads app_config from the master DB (mobile force/soft update gate).
    /// Looks up the exact applicationId row, falling back to the '*' default.
    /// </summary>
    public class AppConfigRepository
    {
        private readonly IConnectionFactory _db;
        public AppConfigRepository(IConnectionFactory db) { _db = db; }

        public AppConfigDto GetConfig(string applicationId, string platform)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.QueryFirstOrDefault<AppConfigDto>(
                    @"SELECT TOP 1
                             application_id             ApplicationId,
                             platform                   Platform,
                             min_supported_version_code MinSupportedVersionCode,
                             latest_version_code        LatestVersionCode,
                             update_message             UpdateMessage,
                             store_url                  StoreUrl,
                             auto_update_enabled        AutoUpdateEnabled
                      FROM app_config
                      WHERE platform = @platform
                        AND application_id IN (@applicationId, '*')
                      ORDER BY CASE WHEN application_id = @applicationId THEN 0 ELSE 1 END",
                    new { applicationId, platform });
        }
    }
}
