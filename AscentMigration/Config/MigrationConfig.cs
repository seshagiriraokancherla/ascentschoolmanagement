using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace AscentMigration.Config
{
    public class MigrationConfig
    {
        public string       SourceConnectionString { get; set; }
        public string       DestConnectionString   { get; set; }
        public int          SchoolId               { get; set; }
        public int          DefaultAcademicYearId  { get; set; }
        public List<string> Modules                { get; set; }
        public bool         DryRun                 { get; set; }
        public int          BatchSize              { get; set; }
        public bool         Cleanup                { get; set; }   // delete all target tables (reverse FK order) + clear log before migrating

        public static MigrationConfig Load()
        {
            var sourceCsEntry = ConfigurationManager.ConnectionStrings["Source"];
            var destCsEntry   = ConfigurationManager.ConnectionStrings["Dest"];

            if (sourceCsEntry == null) throw new Exception("App.config: missing connectionString 'Source'");
            if (destCsEntry   == null) throw new Exception("App.config: missing connectionString 'Dest'");

            var modulesRaw = ConfigurationManager.AppSettings["Modules"] ?? "all";
            var modules    = modulesRaw.Trim().ToLower() == "all"
                ? new List<string> { "all" }
                : modulesRaw.Split(',').Select(m => m.Trim().ToLower()).ToList();

            return new MigrationConfig
            {
                SourceConnectionString = sourceCsEntry.ConnectionString,
                DestConnectionString   = destCsEntry.ConnectionString,
                SchoolId             = int.Parse(ConfigurationManager.AppSettings["SchoolId"]             ?? "1"),
                DefaultAcademicYearId= int.Parse(ConfigurationManager.AppSettings["DefaultAcademicYearId"] ?? "0"),
                Modules   = modules,
                DryRun    = bool.Parse(ConfigurationManager.AppSettings["DryRun"]   ?? "false"),
                BatchSize = int.Parse(ConfigurationManager.AppSettings["BatchSize"] ?? "100"),
                Cleanup   = bool.Parse(ConfigurationManager.AppSettings["Cleanup"]  ?? "false"),
            };
        }

        public bool ShouldRun(string migratorName)
        {
            return Modules.Contains("all") || Modules.Contains(migratorName.ToLower());
        }

        // Returns true if App.config has <add key="tableName.force" value="true" />
        public bool IsForced(string tableName)
        {
            var val = ConfigurationManager.AppSettings[$"{tableName.ToLower()}.force"] ?? "false";
            return bool.Parse(val.Trim());
        }

        // Returns the value of an optional per-migrator custom setting, or null if not set.
        // Example: <add key="fee_receipts.academicYear" value="2024-25" />
        public string GetOptionalSetting(string key)
            => ConfigurationManager.AppSettings[key]?.Trim();

        public MigrationMode GetTableMode(string tableName)
        {
            var perTableKey = $"{tableName.ToLower()}.mode";
            var val = ConfigurationManager.AppSettings[perTableKey]
                   ?? ConfigurationManager.AppSettings["DefaultMode"]
                   ?? "truncate";

            return val.Trim().ToLower() == "skip" ? MigrationMode.Skip : MigrationMode.Truncate;
        }

        public void Print()
        {
            Console.WriteLine($"  Source DB           : {MaskCs(SourceConnectionString)}");
            Console.WriteLine($"  Dest DB             : {MaskCs(DestConnectionString)}");
            Console.WriteLine($"  School ID           : {SchoolId}");
            Console.WriteLine($"  Default Acad Year ID: {DefaultAcademicYearId}");
            Console.WriteLine($"  Modules             : {string.Join(", ", Modules)}");
            Console.WriteLine($"  Dry Run    : {DryRun}");
            Console.WriteLine($"  Batch Size : {BatchSize}");
            Console.WriteLine($"  Cleanup    : {Cleanup}");
        }

        private static string MaskCs(string cs)
        {
            // Show only Server + Database, hide credentials
            var parts = cs.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var visible = parts.Where(p =>
                p.TrimStart().StartsWith("Server",         StringComparison.OrdinalIgnoreCase) ||
                p.TrimStart().StartsWith("Data Source",    StringComparison.OrdinalIgnoreCase) ||
                p.TrimStart().StartsWith("Database",       StringComparison.OrdinalIgnoreCase) ||
                p.TrimStart().StartsWith("Initial Catalog",StringComparison.OrdinalIgnoreCase));
            return string.Join("; ", visible);
        }
    }
}
