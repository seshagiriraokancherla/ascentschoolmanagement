using AscentMigration.Config;
using AscentMigration.Models.Legacy;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AscentMigration.Migrators
{
    public class AcademicYearsMigrator : BaseMigrator
    {
        public override string Name => "academic_years";

        public AcademicYearsMigrator(MigrationConfig config) : base(config) { }

        public override async Task<MigrationResult> RunAsync()
        {
            var result = new MigrationResult { Entity = Name };

            using (var src  = OpenSource())
            using (var dest = OpenDest())
            {
                // 1. Load all legacy rows
                var rows = (await src.QueryAsync<LegacyAcademicYear>("SELECT * FROM SAS_AcdYear")).AsList();
                result.Total = rows.Count;
                Log($"Found {result.Total} rows in SAS_AcdYear");

                // 2. Handle Truncate vs Skip
                var mode = Config.GetTableMode(Name);
                Log($"Mode: {mode}");

                if (mode == MigrationMode.Truncate && !Config.DryRun)
                {
                    var deleted = await dest.ExecuteAsync(
                        "DELETE FROM academic_years WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    Log($"Deleted {deleted} existing rows for school_id={Config.SchoolId}");
                }

                // For Skip mode: check by academic_year + school_id
                var existingYears = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (mode == MigrationMode.Skip)
                {
                    var years = await dest.QueryAsync<string>(
                        "SELECT academic_year FROM academic_years WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    foreach (var y in years) existingYears.Add(y);
                }

                // 3. Migrate row by row
                int processed = 0;
                foreach (var row in rows)
                {
                    processed++;
                    LogProgress(processed, result.Total);

                    var academicYear = row.AcademicYear?.Trim();
                    if (string.IsNullOrWhiteSpace(academicYear))
                    {
                        result.Errors.Add(new MigrationError
                        {
                            RowIdentifier = $"row {processed}",
                            Reason        = "AcademicYear is empty — skipped"
                        });
                        continue;
                    }

                    if (mode == MigrationMode.Skip && existingYears.Contains(academicYear))
                    {
                        result.Skipped++;
                        continue;
                    }

                    try
                    {
                        if (!Config.DryRun)
                        {
                            await dest.ExecuteAsync(@"
                                INSERT INTO academic_years
                                    (academic_year, start_month, end_month, status,
                                     registration_fee_frequency, transport_fee_frequency,
                                     new_admissions_enabled, school_id, created_by, created_at)
                                VALUES
                                    (@AcademicYear, @StartMonth, @EndMonth, @Status,
                                     @RegFeeFrequency, @TransFeeFrequency,
                                     @NewAdmissionsEnabled, @SchoolId, @CreatedBy, @CreatedAt)",
                                new
                                {
                                    AcademicYear         = academicYear,
                                    StartMonth           = row.StartMonth?.Trim(),
                                    EndMonth             = row.EndMonth?.Trim(),
                                    Status               = MapStatus(row.AcdStatus),
                                    RegFeeFrequency      = row.RegFeeType?.Trim(),
                                    TransFeeFrequency    = row.TransFeeTyp?.Trim(),
                                    NewAdmissionsEnabled = row.NewAdminsStat?.Trim(),
                                    SchoolId             = Config.SchoolId,
                                    CreatedBy            = "migration",
                                    CreatedAt            = DateTime.Now
                                });
                        }

                        result.Migrated++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(new MigrationError
                        {
                            RowIdentifier = academicYear,
                            Reason        = ex.Message
                        });
                    }
                }

                LogProgressDone();
            }

            return result;
        }

        private static string MapStatus(string legacy)
        {
            switch (legacy?.Trim())
            {
                case "A": return "Active";
                case "D": return "Inactive";
                default:  return "Inactive";
            }
        }
    }
}
