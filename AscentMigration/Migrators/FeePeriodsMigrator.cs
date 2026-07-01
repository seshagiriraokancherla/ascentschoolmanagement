using AscentMigration.Config;
using AscentMigration.Models.Legacy;
using Dapper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace AscentMigration.Migrators
{
    // SAS_TermMonthData (Descrpt = 'Monthly') → fee_periods
    // Mapping:
    //   TrmMnthData → period_label  (e.g. "October-2026")
    //   month_no    → month number extracted from TrmMnthData ("October-2026" → 10)
    //   YearNam     → year_no       (fallback: year embedded in TrmMnthData label)
    //   OrdrNo      → sequence_no
    //   TraStatus   → status        ('A' → Active, 'D' → Deleted, else Active)
    //   AcdYear     → academic_year_id (lookup academic_years.academic_year; fallback default)
    //   school_id / created_by / created_at populated like every other migrator.
    public class FeePeriodsMigrator : BaseMigrator
    {
        public override string Name => "fee_periods";

        public FeePeriodsMigrator(MigrationConfig config) : base(config) { }

        public override async Task<MigrationResult> RunAsync()
        {
            var result = new MigrationResult { Entity = Name };

            using (var src  = OpenSource())
            using (var dest = OpenDest())
            {
                // 1. Load only the monthly rows
                var rows = (await src.QueryAsync<LegacyTerm>(
                    "SELECT * FROM SAS_TermMonthData WHERE Descrpt = 'Monthly'")).AsList();
                result.Total = rows.Count;
                Log($"Found {result.Total} monthly rows in SAS_TermMonthData (Descrpt = 'Monthly')");

                // 2. Pre-load academic_years lookup: academic_year string → academic_year_id
                var acadYears = await dest.QueryAsync<AcademicYearLookup>(
                    "SELECT academic_year_id, academic_year FROM academic_years WHERE school_id = @SchoolId",
                    new { Config.SchoolId });
                // Tolerant of duplicate academic_year rows in dest (e.g. re-runs without
                // cleanup) — ToDictionary throws on dup keys; keep the first id and warn.
                var yearMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var y in acadYears)
                {
                    var key = y.academic_year?.Trim() ?? "";
                    if (!yearMap.ContainsKey(key)) yearMap[key] = y.academic_year_id;
                    else Log($"Warning: duplicate academic_year '{key}' in dest — keeping first (id={yearMap[key]})");
                }

                // 3. Truncate vs Skip
                var mode = Config.GetTableMode(Name);
                Log($"Mode: {mode}");

                if (mode == MigrationMode.Truncate && !Config.DryRun)
                {
                    var deleted = await dest.ExecuteAsync(
                        "DELETE FROM fee_periods WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    Log($"Deleted {deleted} existing rows for school_id={Config.SchoolId}");
                }

                // Skip mode: composite key period_label|academic_year_id
                var existingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (mode == MigrationMode.Skip)
                {
                    var existing = await dest.QueryAsync<FeePeriodLookup>(
                        "SELECT period_label, academic_year_id FROM fee_periods WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    foreach (var p in existing)
                        existingKeys.Add(CompositeKey(p.period_label, p.academic_year_id));
                }

                // Fallback academic_year_id (config override or latest in dest)
                int defaultYearId = await ResolveDefaultAcademicYearIdAsync(dest);
                Log($"Fallback academic_year_id = {defaultYearId}");

                // 4. Migrate row by row
                int processed = 0;
                foreach (var row in rows)
                {
                    processed++;
                    LogProgress(processed, result.Total);

                    var label = row.TrmMnthData?.Trim();
                    if (string.IsNullOrWhiteSpace(label))
                    {
                        result.Errors.Add(new MigrationError
                        {
                            RowIdentifier = $"TrmID={row.TrmID?.Trim() ?? "null"}",
                            Reason        = "TrmMnthData is empty — skipped"
                        });
                        continue;
                    }

                    // month_no from the label ("October-2026" → 10)
                    if (!TryExtractMonthNo(label, out byte monthNo))
                    {
                        result.Errors.Add(new MigrationError
                        {
                            RowIdentifier = label,
                            Reason        = "Could not extract month number from TrmMnthData — skipped"
                        });
                        continue;
                    }

                    // year_no from YearNam; fallback to the year embedded in the label
                    if (!TryParseYearNo(row.YearNam, label, out short yearNo))
                    {
                        result.Errors.Add(new MigrationError
                        {
                            RowIdentifier = label,
                            Reason        = $"Could not determine year_no (YearNam='{row.YearNam?.Trim()}') — skipped"
                        });
                        continue;
                    }

                    // academic_year_id by AcdYear string; fallback to default
                    int academicYearId = defaultYearId;
                    var acdYear = row.AcdYear?.Trim();
                    if (!string.IsNullOrWhiteSpace(acdYear))
                    {
                        if (yearMap.TryGetValue(acdYear, out var mappedId))
                            academicYearId = mappedId;
                        else
                            Log($"Warning: academic_year '{acdYear}' not found for period '{label}' — using fallback academic_year_id={defaultYearId}");
                    }

                    if (mode == MigrationMode.Skip && existingKeys.Contains(CompositeKey(label, academicYearId)))
                    {
                        result.Skipped++;
                        continue;
                    }

                    try
                    {
                        if (!Config.DryRun)
                        {
                            await dest.ExecuteAsync(@"
                                INSERT INTO fee_periods
                                    (school_id, academic_year_id, month_no, year_no,
                                     period_label, sequence_no, status, created_by, created_at)
                                VALUES
                                    (@SchoolId, @AcademicYearId, @MonthNo, @YearNo,
                                     @PeriodLabel, @SequenceNo, @Status, @CreatedBy, @CreatedAt)",
                                new
                                {
                                    SchoolId       = Config.SchoolId,
                                    AcademicYearId = academicYearId,
                                    MonthNo        = monthNo,
                                    YearNo         = yearNo,
                                    PeriodLabel    = label,
                                    SequenceNo     = row.OrdrNo,
                                    Status         = MapStatus(row.TraStatus),
                                    CreatedBy      = "migration",
                                    CreatedAt      = DateTime.Now
                                });
                        }

                        result.Migrated++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(new MigrationError
                        {
                            RowIdentifier = $"{label} (year={acdYear})",
                            Reason        = ex.Message
                        });
                    }
                }

                LogProgressDone();
            }

            return result;
        }

        private static string CompositeKey(string label, int? academicYearId)
        {
            return $"{label?.Trim()}|{academicYearId}";
        }

        // Extracts the month number from a label like "October-2026" / "Oct-2026".
        private static bool TryExtractMonthNo(string label, out byte monthNo)
        {
            monthNo = 0;
            if (string.IsNullOrWhiteSpace(label)) return false;

            // The month name is the part before the first separator ('-', '/', space).
            var token = label.Split(new[] { '-', '/', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                             .FirstOrDefault()?.Trim();
            if (string.IsNullOrWhiteSpace(token)) return false;

            // Numeric month (e.g. "10-2026")
            if (byte.TryParse(token, out var num) && num >= 1 && num <= 12)
            {
                monthNo = num;
                return true;
            }

            // Full month name ("MMMM") then abbreviated ("MMM")
            if (DateTime.TryParseExact(token, "MMMM", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dtFull))
            {
                monthNo = (byte)dtFull.Month;
                return true;
            }
            if (DateTime.TryParseExact(token, "MMM", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dtAbbr))
            {
                monthNo = (byte)dtAbbr.Month;
                return true;
            }

            return false;
        }

        // year_no from YearNam ("2026" or "2026-27" → 2026); fallback to the year in the label.
        private static bool TryParseYearNo(string yearNam, string label, out short yearNo)
        {
            yearNo = 0;

            var y = yearNam?.Trim();
            if (!string.IsNullOrWhiteSpace(y))
            {
                // "2026-27" → take the leading part
                var first = y.Split('-')[0].Trim();
                if (short.TryParse(first, out var parsed) && parsed > 0)
                {
                    yearNo = parsed;
                    return true;
                }
            }

            // Fallback: the year embedded in "October-2026"
            if (!string.IsNullOrWhiteSpace(label))
            {
                var parts = label.Split(new[] { '-', '/', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var last = parts.LastOrDefault()?.Trim();
                if (short.TryParse(last, out var labelYear) && labelYear > 0)
                {
                    yearNo = labelYear;
                    return true;
                }
            }

            return false;
        }

        private static string MapStatus(string legacy)
        {
            switch (legacy?.Trim())
            {
                case "A": return "Active";
                case "D": return "Deleted";
                default:  return "Active";
            }
        }

        private class AcademicYearLookup
        {
            public int    academic_year_id { get; set; }
            public string academic_year    { get; set; }
        }

        private class FeePeriodLookup
        {
            public string period_label     { get; set; }
            public int?   academic_year_id { get; set; }
        }
    }
}
