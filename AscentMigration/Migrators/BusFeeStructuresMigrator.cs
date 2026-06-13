using AscentMigration.Config;
using AscentMigration.Models.Legacy;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace AscentMigration.Migrators
{
    public class BusFeeStructuresMigrator : BaseMigrator
    {
        public override string Name => "bus_fee_structures";

        public BusFeeStructuresMigrator(MigrationConfig config) : base(config) { }

        public override async Task<MigrationResult> RunAsync()
        {
            var result = new MigrationResult { Entity = Name };

            using (var src  = OpenSource())
            using (var dest = OpenDest())
            {
                // 1. Load all legacy rows
                var rows = (await src.QueryAsync<LegacyBusMaster>("SELECT * FROM SAS_BusMaster")).AsList();
                result.Total = rows.Count;
                Log($"Found {result.Total} rows in SAS_BusMaster");

                // 2. Pre-load all lookup dictionaries
                var legacyRouteNameMap = await LoadLegacyRouteNameMap(src);       // RouteID  → RouteNam (source)
                var destRouteIdMap     = await LoadDestRouteIdMap(dest);          // route_name → route_id (dest)
                var acadYearMap        = await LoadAcadYearMap(dest);             // academic_year → academic_year_id
                var termMap            = await LoadTermMap(dest);                 // "term_name|academic_year_id" → term_id

                Log($"Lookups loaded — legacyRoutes:{legacyRouteNameMap.Count} destRoutes:{destRouteIdMap.Count} years:{acadYearMap.Count} terms:{termMap.Count}");

                // 3. Handle Truncate vs Skip
                var mode = Config.GetTableMode(Name);
                Log($"Mode: {mode}");

                if (mode == MigrationMode.Truncate && !Config.DryRun)
                {
                    var deleted = await dest.ExecuteAsync(
                        "DELETE FROM bus_fee_structures WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    Log($"Deleted {deleted} existing rows for school_id={Config.SchoolId}");
                }

                // For Skip mode: composite key route_id|term_id|academic_year_id
                var existingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (mode == MigrationMode.Skip)
                {
                    var existing = await dest.QueryAsync<BusFeeKey>(
                        @"SELECT ISNULL(route_id,0)        AS route_id,
                                 ISNULL(term_id,0)         AS term_id,
                                 ISNULL(academic_year_id,0)AS academic_year_id
                          FROM bus_fee_structures WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    foreach (var e in existing)
                        existingKeys.Add(MakeKey(e.route_id, e.term_id, e.academic_year_id));
                }

                // 4. Migrate row by row
                int processed = 0;
                foreach (var row in rows)
                {
                    processed++;
                    LogProgress(processed, result.Total);

                    // --- academic_year_id ---
                    int? academicYearId = null;
                    var acdYear = row.AcdmYear?.Trim();
                    if (!string.IsNullOrWhiteSpace(acdYear))
                    {
                        if (acadYearMap.TryGetValue(acdYear, out var ayId))
                            academicYearId = ayId;
                        else
                            Log($"Warning: academic_year '{acdYear}' not found — academic_year_id set to NULL");
                    }

                    // --- route_id: two-step lookup ---
                    // Step 1: legacy RouteID → RouteNam via SAS_BusRoutes
                    // Step 2: RouteNam → route_id via dest bus_routes
                    int? routeId = null;
                    var legacyRouteId = row.RouteID?.Trim();
                    if (!string.IsNullOrWhiteSpace(legacyRouteId))
                    {
                        if (legacyRouteNameMap.TryGetValue(legacyRouteId, out var routeName))
                        {
                            if (destRouteIdMap.TryGetValue(routeName, out var rId))
                                routeId = rId;
                            else
                                AddError(result, row.BusMasterID, $"route_name '{routeName}' (from RouteID='{legacyRouteId}') not found in dest bus_routes — route_id set to NULL");
                        }
                        else
                            AddError(result, row.BusMasterID, $"RouteID='{legacyRouteId}' not found in SAS_BusRoutes — route_id set to NULL");
                    }

                    // --- term_id: lookup by "term_name|academic_year_id" ---
                    int? termId = null;
                    var termDet = row.TermDet?.Trim();
                    if (!string.IsNullOrWhiteSpace(termDet) && academicYearId.HasValue)
                    {
                        var termKey = $"{termDet}|{academicYearId}";
                        if (termMap.TryGetValue(termKey, out var tId))
                            termId = tId;
                        else
                            AddError(result, row.BusMasterID, $"term '{termDet}' (academic_year_id={academicYearId}) not found in dest terms — term_id set to NULL");
                    }
                    else if (!string.IsNullOrWhiteSpace(termDet) && !academicYearId.HasValue)
                    {
                        AddError(result, row.BusMasterID, $"term '{termDet}' cannot be resolved — academic_year_id is NULL — term_id set to NULL");
                    }

                    // --- Skip check ---
                    if (mode == MigrationMode.Skip)
                    {
                        var key = MakeKey(routeId, termId, academicYearId);
                        if (existingKeys.Contains(key))
                        {
                            result.Skipped++;
                            continue;
                        }
                    }

                    try
                    {
                        if (!Config.DryRun)
                        {
                            await dest.ExecuteAsync(@"
                                INSERT INTO bus_fee_structures
                                    (route_id, term_id, amount, academic_year_id,
                                     status, bus_name, category_name,
                                     school_id, created_by, created_at,
                                     machine_id, deleted_by)
                                VALUES
                                    (@RouteId, @TermId, @Amount, @AcademicYearId,
                                     @Status, @BusName, @CategoryName,
                                     @SchoolId, @CreatedBy, @CreatedAt,
                                     @MachineId, @DeletedBy)",
                                new
                                {
                                    RouteId        = routeId,
                                    TermId         = termId,
                                    Amount         = row.Amt,
                                    AcademicYearId = academicYearId,
                                    Status         = MapStatus(row.TraStatus),
                                    BusName        = row.BusNam?.Trim(),
                                    CategoryName   = row.CategoryNam?.Trim(),
                                    SchoolId       = Config.SchoolId,
                                    CreatedBy      = "migration",
                                    CreatedAt      = DateTime.Now,
                                    MachineId      = row.MachID?.Trim(),
                                    DeletedBy      = row.DeleteBy?.Trim()
                                });
                        }

                        result.Migrated++;
                    }
                    catch (Exception ex)
                    {
                        AddError(result, row.BusMasterID, $"INSERT failed: {ex.Message}");
                    }
                }

                LogProgressDone();
            }

            return result;
        }

        // ---------------------------------------------------------------
        // Lookup loaders
        // ---------------------------------------------------------------

        // Loads RouteID → RouteNam from legacy source SAS_BusRoutes
        private async Task<Dictionary<string, string>> LoadLegacyRouteNameMap(SqlConnection src)
        {
            var rows = await src.QueryAsync<LegacyRouteRow>("SELECT RouteID, RouteNam FROM SAS_BusRoutes");
            var map  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
            {
                var key = r.RouteID?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(key) && !map.ContainsKey(key))
                    map[key] = r.RouteNam?.Trim() ?? "";
            }
            return map;
        }

        // Loads route_name → route_id from dest bus_routes.
        // On duplicate route_name, the Active row wins over any other status.
        private async Task<Dictionary<string, int>> LoadDestRouteIdMap(SqlConnection dest)
        {
            var rows = await dest.QueryAsync<DestRouteRow>(
                "SELECT route_id, route_name, status FROM bus_routes WHERE school_id = @SchoolId",
                new { Config.SchoolId });
            var map        = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var mapStatus  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
            {
                var key    = r.route_name?.Trim() ?? "";
                var status = r.status?.Trim() ?? "";
                if (!map.ContainsKey(key))
                {
                    map[key]       = r.route_id;
                    mapStatus[key] = status;
                }
                else if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase)
                      && !string.Equals(mapStatus[key], "Active", StringComparison.OrdinalIgnoreCase))
                {
                    Log($"Warning: duplicate route_name '{key}' — replacing id={map[key]} ({mapStatus[key]}) with id={r.route_id} (Active)");
                    map[key]       = r.route_id;
                    mapStatus[key] = status;
                }
                else
                {
                    Log($"Warning: duplicate route_name '{key}' — keeping existing id={map[key]} ({mapStatus[key]}), ignoring id={r.route_id} ({status})");
                }
            }
            return map;
        }

        private async Task<Dictionary<string, int>> LoadAcadYearMap(SqlConnection dest)
        {
            var rows = await dest.QueryAsync<AcadYearRow>(
                "SELECT academic_year_id, academic_year FROM academic_years WHERE school_id = @SchoolId",
                new { Config.SchoolId });
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
            {
                var key = r.academic_year?.Trim() ?? "";
                if (!map.ContainsKey(key)) map[key] = r.academic_year_id;
                else Log($"Warning: duplicate academic_year '{key}' in dest — keeping first (id={map[key]})");
            }
            return map;
        }

        // Composite key: "term_name|academic_year_id"
        private async Task<Dictionary<string, int>> LoadTermMap(SqlConnection dest)
        {
            var rows = await dest.QueryAsync<TermRow>(
                "SELECT term_id, term_name, academic_year_id FROM terms WHERE school_id = @SchoolId",
                new { Config.SchoolId });
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
            {
                var key = $"{r.term_name?.Trim()}|{r.academic_year_id}";
                if (!map.ContainsKey(key)) map[key] = r.term_id;
            }
            return map;
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private static string MakeKey(int? routeId, int? termId, int? academicYearId)
            => $"{routeId ?? 0}|{termId ?? 0}|{academicYearId ?? 0}";

        private static void AddError(MigrationResult result, string rowId, string reason)
        {
            result.Errors.Add(new MigrationError
            {
                RowIdentifier = $"BusMasterID={rowId?.Trim() ?? "null"}",
                Reason        = reason
            });
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

        // ---------------------------------------------------------------
        // Private POCOs for typed Dapper queries
        // ---------------------------------------------------------------

        private class LegacyRouteRow { public string RouteID   { get; set; } public string RouteNam      { get; set; } }
        private class DestRouteRow   { public int    route_id  { get; set; } public string route_name    { get; set; } public string status { get; set; } }
        private class AcadYearRow    { public int    academic_year_id { get; set; } public string academic_year { get; set; } }
        private class TermRow        { public int    term_id   { get; set; } public string term_name     { get; set; } public int? academic_year_id { get; set; } }
        private class BusFeeKey      { public int    route_id  { get; set; } public int    term_id       { get; set; } public int  academic_year_id { get; set; } }
    }
}
