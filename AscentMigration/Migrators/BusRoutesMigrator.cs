using AscentMigration.Config;
using AscentMigration.Models.Legacy;
using Dapper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AscentMigration.Migrators
{
    public class BusRoutesMigrator : BaseMigrator
    {
        public override string Name => "bus_routes";

        public BusRoutesMigrator(MigrationConfig config) : base(config) { }

        public override async Task<MigrationResult> RunAsync()
        {
            var result = new MigrationResult { Entity = Name };

            using (var src  = OpenSource())
            using (var dest = OpenDest())
            {
                // 1. Load all legacy rows
                var rows = (await src.QueryAsync<LegacyBusRoute>("SELECT * FROM SAS_BusRoutes")).AsList();
                result.Total = rows.Count;
                Log($"Found {result.Total} rows in SAS_BusRoutes");

                // 2. Handle Truncate vs Skip
                var mode = Config.GetTableMode(Name);
                Log($"Mode: {mode}");

                if (mode == MigrationMode.Truncate && !Config.DryRun)
                {
                    var deleted = await dest.ExecuteAsync(
                        "DELETE FROM bus_routes WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    Log($"Deleted {deleted} existing rows for school_id={Config.SchoolId}");
                }

                // For Skip mode: load existing route names to detect duplicates
                var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (mode == MigrationMode.Skip)
                {
                    var names = await dest.QueryAsync<string>(
                        "SELECT route_name FROM bus_routes WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    foreach (var n in names) existingNames.Add(n);
                }

                // 3. Migrate row by row
                int processed = 0;
                foreach (var row in rows)
                {
                    processed++;
                    LogProgress(processed, result.Total);

                    var routeName = row.RouteNam?.Trim();
                    if (string.IsNullOrWhiteSpace(routeName))
                    {
                        result.Errors.Add(new MigrationError
                        {
                            RowIdentifier = $"RouteID={row.RouteID?.Trim() ?? "null"}",
                            Reason        = "RouteNam is empty — skipped"
                        });
                        continue;
                    }

                    if (mode == MigrationMode.Skip && existingNames.Contains(routeName))
                    {
                        result.Skipped++;
                        continue;
                    }

                    try
                    {
                        if (!Config.DryRun)
                        {
                            await dest.ExecuteAsync(@"
                                INSERT INTO bus_routes
                                    (route_name, route_code, route_category, description,
                                     status, bus_no_data, school_id,
                                     created_by, created_at, machine_id, deleted_by)
                                VALUES
                                    (@RouteName, @RouteCode, @RouteCategory, @Description,
                                     @Status, @BusNoData, @SchoolId,
                                     @CreatedBy, @CreatedAt, @MachineId, @DeletedBy)",
                                new
                                {
                                    RouteName     = routeName,
                                    RouteCode     = row.RouteCod?.Trim(),
                                    RouteCategory = row.RouteCategory?.Trim(),
                                    Description   = row.Descrpt?.Trim(),
                                    Status        = MapStatus(row.RouteStatus),
                                    BusNoData     = row.BusNoData?.Trim(),
                                    SchoolId      = Config.SchoolId,
                                    CreatedBy     = "migration",
                                    CreatedAt     = DateTime.Now,
                                    MachineId     = row.MachID?.Trim(),
                                    DeletedBy     = row.DeleteData?.Trim()
                                });
                        }

                        result.Migrated++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(new MigrationError
                        {
                            RowIdentifier = routeName,
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
