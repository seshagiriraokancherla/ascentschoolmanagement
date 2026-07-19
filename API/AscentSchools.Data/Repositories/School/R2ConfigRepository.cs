using AscentSchools.Core.DTOs.School.Settings;
using AscentSchools.Data.ConnectionFactory;
using Dapper;

namespace AscentSchools.Data.Repositories.School
{
    /// <summary>Per-school Cloudflare R2 storage config (tenant table r2_configs).</summary>
    public class R2ConfigRepository
    {
        private readonly IConnectionFactory _db;
        public R2ConfigRepository(IConnectionFactory db) { _db = db; }

        // Safe view for the settings page — never returns the secret; HasSecretKey flags presence.
        public R2ConfigDto GetConfig(string tenantDbName, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QueryFirstOrDefault<R2ConfigDto>(
                    @"SELECT account_id AccountId, access_key_id AccessKeyId,
                             bucket_name BucketName, public_base_url PublicBaseUrl,
                             is_enabled IsEnabled,
                             CAST(CASE WHEN LEN(ISNULL(secret_access_key,'')) > 0 THEN 1 ELSE 0 END AS BIT) HasSecretKey
                      FROM r2_configs WHERE school_id = @schoolId",
                    new { schoolId });
        }

        // Full config incl. secret — server-side use only (presigning). Null when absent/disabled.
        public R2ConfigInternal GetInternal(string tenantDbName, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QueryFirstOrDefault<R2ConfigInternal>(
                    @"SELECT account_id AccountId, access_key_id AccessKeyId,
                             secret_access_key SecretAccessKey, bucket_name BucketName,
                             public_base_url PublicBaseUrl, is_enabled IsEnabled
                      FROM r2_configs WHERE school_id = @schoolId AND is_enabled = 1",
                    new { schoolId });
        }

        // Upsert — a blank SecretAccessKey keeps the stored one.
        public void Upsert(string tenantDbName, int schoolId, UpdateR2ConfigRequest req, string createdBy)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"MERGE r2_configs AS t
                      USING (SELECT @schoolId AS school_id) AS s ON t.school_id = s.school_id
                      WHEN MATCHED THEN UPDATE SET
                          account_id      = @AccountId,
                          access_key_id   = @AccessKeyId,
                          secret_access_key = CASE WHEN LEN(ISNULL(@SecretAccessKey,'')) > 0
                                                   THEN @SecretAccessKey ELSE t.secret_access_key END,
                          bucket_name     = @BucketName,
                          public_base_url = @PublicBaseUrl,
                          is_enabled      = @IsEnabled,
                          updated_at      = GETDATE()
                      WHEN NOT MATCHED THEN INSERT
                          (school_id, account_id, access_key_id, secret_access_key,
                           bucket_name, public_base_url, is_enabled, created_by)
                      VALUES
                          (@schoolId, @AccountId, @AccessKeyId, ISNULL(@SecretAccessKey,''),
                           @BucketName, @PublicBaseUrl, @IsEnabled, @createdBy);",
                    new
                    {
                        schoolId,
                        req.AccountId,
                        req.AccessKeyId,
                        req.SecretAccessKey,
                        req.BucketName,
                        PublicBaseUrl = (req.PublicBaseUrl ?? "").TrimEnd('/'),
                        req.IsEnabled,
                        createdBy
                    });
        }
    }
}
