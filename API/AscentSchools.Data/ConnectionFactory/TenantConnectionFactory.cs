using Dapper;
using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace AscentSchools.Data.ConnectionFactory
{
    /// <summary>
    /// Resolves SQL Server connections for both master DB and tenant DBs.
    ///
    /// Master DB  : one fixed connection string in Web.config (MasterDb:ConnectionString)
    /// Tenant DB  : server/options from TenantDb:ConnectionTemplate, but credentials are
    ///              looked up per-DB from school_groups (db_username / db_password).
    ///              Falls back to template credentials when db_username is not set.
    ///
    /// Credentials are cached in memory for 15 minutes to avoid a master-DB round-trip
    /// on every tenant query. The cache is invalidated per dbName on TTL expiry.
    /// </summary>
    public class TenantConnectionFactory : IConnectionFactory
    {
        private readonly string _masterConnectionString;
        private readonly string _tenantConnectionTemplate;

        // Thread-safe credential cache: dbName → (username, password, cachedAt)
        private static readonly ConcurrentDictionary<string, CredentialEntry> _credCache
            = new ConcurrentDictionary<string, CredentialEntry>(StringComparer.OrdinalIgnoreCase);

        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);

        private class CredentialEntry
        {
            public string   Username { get; set; }
            public string   Password { get; set; }
            public DateTime CachedAt { get; set; }
        }

        public TenantConnectionFactory()
        {
            _masterConnectionString   = ConfigurationManager.AppSettings["MasterDb:ConnectionString"];
            _tenantConnectionTemplate = ConfigurationManager.AppSettings["TenantDb:ConnectionTemplate"];
        }

        /// <summary>Returns an open connection to ascent_master.</summary>
        public IDbConnection GetMasterConnection()
        {
            var conn = new SqlConnection(_masterConnectionString);
            conn.Open();
            return conn;
        }

        /// <summary>
        /// Returns an open connection to the given tenant DB.
        /// Uses per-DB credentials from school_groups when available; falls back to template.
        /// </summary>
        public IDbConnection GetTenantConnection(string tenantDbName)
        {
            var connStr = BuildTenantConnectionString(tenantDbName);
            var conn    = new SqlConnection(connStr);
            conn.Open();
            return conn;
        }

        private string BuildTenantConnectionString(string tenantDbName)
        {
            var creds = GetCachedCredentials(tenantDbName);
            if (creds != null && !string.IsNullOrEmpty(creds.Username) && !string.IsNullOrEmpty(creds.Password))
            {
                // Parse server/options from template, override with per-DB credentials
                var templateBuilder = new SqlConnectionStringBuilder(string.Format(_tenantConnectionTemplate, tenantDbName));
                var builder = new SqlConnectionStringBuilder
                {
                    DataSource               = templateBuilder.DataSource,
                    InitialCatalog           = tenantDbName,
                    UserID                   = creds.Username,
                    Password                 = creds.Password,
                    MultipleActiveResultSets = templateBuilder.MultipleActiveResultSets,
                };
                return builder.ConnectionString;
            }

            // Fallback: use template as-is (original behavior)
            return string.Format(_tenantConnectionTemplate, tenantDbName);
        }

        private CredentialEntry GetCachedCredentials(string dbName)
        {
            if (_credCache.TryGetValue(dbName, out var cached))
            {
                if (DateTime.UtcNow - cached.CachedAt < CacheTtl)
                    return cached;
                _credCache.TryRemove(dbName, out _);
            }

            try
            {
                using (var conn = GetMasterConnection())
                {
                    var result = conn.QueryFirstOrDefault<CredentialEntry>(
                        @"SELECT db_username Username, db_password Password
                          FROM school_groups WHERE db_name = @dbName",
                        new { dbName });

                    if (result != null)
                    {
                        result.CachedAt = DateTime.UtcNow;
                        _credCache[dbName] = result;
                        return result;
                    }
                }
            }
            catch { /* Fail silently — fall back to template credentials */ }

            return null;
        }

        /// <summary>
        /// Clears the cached credentials for a specific tenant DB.
        /// Call this after updating db_username or db_password in school_groups.
        /// </summary>
        public static void InvalidateCredentialCache(string dbName)
        {
            _credCache.TryRemove(dbName, out _);
        }

        /// <summary>
        /// Provisions a new tenant DB: creates the database and runs tenant_tables.sql.
        /// Called by the Control App when onboarding a new school group.
        /// </summary>
        public void ProvisionTenantDatabase(string dbName, string sqlScript)
        {
            using (var master = GetMasterConnection())
            {
                master.Execute($"IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = '{dbName}') CREATE DATABASE [{dbName}]");
            }

            using (var tenant = GetTenantConnection(dbName))
            {
                var batches = sqlScript.Split(new[] { "\r\nGO\r\n", "\nGO\n", "\r\nGO", "\nGO" },
                    System.StringSplitOptions.RemoveEmptyEntries);

                foreach (var batch in batches)
                {
                    var trimmed = batch.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed))
                        tenant.Execute(trimmed);
                }
            }
        }
    }

    // Minimal Dapper extension used internally by ProvisionTenantDatabase
    internal static class ConnectionExtensions
    {
        internal static void Execute(this IDbConnection conn, string sql)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.CommandType = CommandType.Text;
                cmd.ExecuteNonQuery();
            }
        }
    }
}
