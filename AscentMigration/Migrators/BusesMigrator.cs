using AscentMigration.Config;
using AscentMigration.Models.Legacy;
using Dapper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AscentMigration.Migrators
{
    public class BusesMigrator : BaseMigrator
    {
        public override string Name => "buses";

        public BusesMigrator(MigrationConfig config) : base(config) { }

        public override async Task<MigrationResult> RunAsync()
        {
            var result = new MigrationResult { Entity = Name };

            using (var src  = OpenSource())
            using (var dest = OpenDest())
            {
                // 1. Load all legacy rows
                var rows = (await src.QueryAsync<LegacyBus>("SELECT * FROM SAS_BussData")).AsList();
                result.Total = rows.Count;
                Log($"Found {result.Total} rows in SAS_BussData");

                // 2. Handle Truncate vs Skip
                var mode = Config.GetTableMode(Name);
                Log($"Mode: {mode}");

                if (mode == MigrationMode.Truncate && !Config.DryRun)
                {
                    var deleted = await dest.ExecuteAsync(
                        "DELETE FROM buses WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    Log($"Deleted {deleted} existing rows for school_id={Config.SchoolId}");
                }

                // For Skip mode: dedup by bus_name
                var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (mode == MigrationMode.Skip)
                {
                    var names = await dest.QueryAsync<string>(
                        "SELECT bus_name FROM buses WHERE school_id = @SchoolId AND bus_name IS NOT NULL",
                        new { Config.SchoolId });
                    foreach (var n in names) existingNames.Add(n);
                }

                // 3. Migrate row by row
                int processed = 0;
                foreach (var row in rows)
                {
                    processed++;
                    LogProgress(processed, result.Total);

                    // --- capacity: varchar → int ---
                    int? capacity = null;
                    var capRaw = row.BusCapacity?.Trim();
                    if (!string.IsNullOrWhiteSpace(capRaw))
                    {
                        if (int.TryParse(capRaw, out var cap))
                            capacity = cap;
                        else
                            Log($"Warning: BusID={row.BusID?.Trim()} — BusCapacity='{capRaw}' is not a number — capacity set to NULL");
                    }

                    var busName = row.BusNam?.Trim();

                    if (mode == MigrationMode.Skip && busName != null && existingNames.Contains(busName))
                    {
                        result.Skipped++;
                        continue;
                    }

                    try
                    {
                        if (!Config.DryRun)
                        {
                            await dest.ExecuteAsync(@"
                                INSERT INTO buses
                                    (bus_name, model, capacity, description, status,
                                     purchase_date, owner_data, route_name,
                                     registration_no, trip_data, cleaner_name,
                                     school_id, created_by, created_at)
                                VALUES
                                    (@BusName, @Model, @Capacity, @Description, @Status,
                                     @PurchaseDate, @OwnerData, @RouteName,
                                     @RegistrationNo, @TripData, @CleanerName,
                                     @SchoolId, @CreatedBy, @CreatedAt)",
                                new
                                {
                                    BusName        = busName,
                                    Model          = row.BusModelDet?.Trim(),
                                    Capacity       = capacity,
                                    Description    = row.Descrpt?.Trim(),
                                    Status         = MapStatus(row.BusStat),
                                    PurchaseDate   = row.PurDat,
                                    OwnerData      = row.OwnderData?.Trim(),
                                    RouteName      = row.RouteNam?.Trim(),
                                    RegistrationNo = row.BusRegNo?.Trim(),
                                    TripData       = row.TripData?.Trim(),
                                    CleanerName    = row.CleanerNam?.Trim(),
                                    SchoolId       = Config.SchoolId,
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
                            RowIdentifier = $"BusID={row.BusID?.Trim()} BusName={busName}",
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
