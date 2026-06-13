using AscentMigration.Config;
using Dapper;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace AscentMigration.Migrators
{
    public abstract class BaseMigrator : IMigrator
    {
        protected readonly MigrationConfig Config;

        protected BaseMigrator(MigrationConfig config)
        {
            Config = config;
        }

        public abstract string Name { get; }
        public abstract Task<MigrationResult> RunAsync();

        protected SqlConnection OpenSource()
        {
            var conn = new SqlConnection(Config.SourceConnectionString);
            conn.Open();
            return conn;
        }

        protected SqlConnection OpenDest()
        {
            var conn = new SqlConnection(Config.DestConnectionString);
            conn.Open();
            return conn;
        }

        // Resolves the fallback academic_year_id used by migrators whose legacy
        // source has no academic-year column (fee_categories, fee_types) or whose
        // AcdYear lookup misses (terms, fee_structures).
        // If App.config DefaultAcademicYearId > 0 it is used as a manual override;
        // otherwise the latest academic_year_id in the dest academic_years table
        // (highest id = most recently inserted year) is used. Call AFTER the
        // AcademicYearsMigrator has run (it runs first in the pipeline).
        protected async Task<int> ResolveDefaultAcademicYearIdAsync(SqlConnection dest)
        {
            if (Config.DefaultAcademicYearId > 0)
                return Config.DefaultAcademicYearId;

            var id = await dest.QueryFirstOrDefaultAsync<int?>(
                @"SELECT TOP 1 academic_year_id FROM academic_years
                  WHERE school_id = @SchoolId
                  ORDER BY academic_year_id DESC",
                new { Config.SchoolId });
            return id ?? 0;
        }

        protected void Log(string message)
        {
            Console.WriteLine($"  [{Name}] {message}");
        }

        protected void LogProgress(int current, int total)
        {
            if (total == 0) return;
            var pct = (int)((double)current / total * 100);
            Console.Write($"\r  [{Name}] Progress: {current}/{total} ({pct}%)   ");
        }

        protected void LogProgressDone()
        {
            Console.WriteLine();
        }
    }
}
