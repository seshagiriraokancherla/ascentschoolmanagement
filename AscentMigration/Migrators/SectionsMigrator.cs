using AscentMigration.Config;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace AscentMigration.Migrators
{
    public class SectionsMigrator : BaseMigrator
    {
        public override string Name => "sections";

        public SectionsMigrator(MigrationConfig config) : base(config) { }

        public override async Task<MigrationResult> RunAsync()
        {
            var result = new MigrationResult { Entity = Name };

            using (var src  = OpenSource())
            using (var dest = OpenDest())
            {
                // 1. Load distinct section names from legacy StudentMaster
                var sectionNames = (await src.QueryAsync<string>(
                    @"SELECT DISTINCT StuSect
                      FROM SAS_StudentMaster
                      WHERE StuSect IS NOT NULL AND StuSect != ''
                      ORDER BY StuSect")).AsList();

                Log($"Found {sectionNames.Count} distinct section(s) in SAS_StudentMaster: {string.Join(", ", sectionNames)}");

                // 2. Load all classes for this school
                var classes = (await dest.QueryAsync<ClassRow>(
                    "SELECT class_id, class_name FROM classes WHERE school_id = @SchoolId ORDER BY class_id",
                    new { Config.SchoolId })).AsList();

                Log($"Found {classes.Count} class(es) for school_id={Config.SchoolId}");

                if (sectionNames.Count == 0 || classes.Count == 0)
                {
                    Log("Nothing to migrate — no sections or no classes found.");
                    return result;
                }

                result.Total = sectionNames.Count * classes.Count;
                Log($"Total rows to insert: {result.Total} ({sectionNames.Count} sections × {classes.Count} classes)");

                // 3. Handle Truncate vs Skip
                var mode = Config.GetTableMode(Name);
                Log($"Mode: {mode}");

                if (mode == MigrationMode.Truncate && !Config.DryRun)
                {
                    var deleted = await dest.ExecuteAsync(
                        "DELETE FROM sections WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    Log($"Deleted {deleted} existing rows for school_id={Config.SchoolId}");
                }

                // For Skip mode: load existing class_id+section_name combos
                var existingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (mode == MigrationMode.Skip)
                {
                    var existing = await dest.QueryAsync<SectionKey>(
                        "SELECT class_id, section_name FROM sections WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    foreach (var e in existing)
                        existingKeys.Add(MakeKey(e.class_id, e.section_name));
                }

                // 4. For each class insert all sections; sequence_no resets to 1 per class
                int processed = 0;
                foreach (var cls in classes)
                {
                    int seqNo = 1;
                    foreach (var sectionName in sectionNames)
                    {
                        processed++;
                        LogProgress(processed, result.Total);

                        if (mode == MigrationMode.Skip && existingKeys.Contains(MakeKey(cls.class_id, sectionName)))
                        {
                            result.Skipped++;
                            seqNo++;
                            continue;
                        }

                        try
                        {
                            if (!Config.DryRun)
                            {
                                await dest.ExecuteAsync(@"
                                    INSERT INTO sections
                                        (class_id, section_name, description, sequence_no,
                                         status, school_id, created_by, created_at)
                                    VALUES
                                        (@ClassId, @SectionName, NULL, @SequenceNo,
                                         'Active', @SchoolId, @CreatedBy, @CreatedAt)",
                                    new
                                    {
                                        ClassId     = cls.class_id,
                                        SectionName = sectionName,
                                        SequenceNo  = seqNo,
                                        SchoolId    = Config.SchoolId,
                                        CreatedBy   = "migration",
                                        CreatedAt   = DateTime.Now
                                    });
                            }

                            result.Migrated++;
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add(new MigrationError
                            {
                                RowIdentifier = $"class_id={cls.class_id} ({cls.class_name}), section='{sectionName}'",
                                Reason        = ex.Message
                            });
                        }

                        seqNo++;
                    }
                }

                LogProgressDone();
            }

            return result;
        }

        private static string MakeKey(int classId, string sectionName)
            => $"{classId}|{sectionName?.Trim() ?? ""}";

        private class ClassRow   { public int class_id    { get; set; } public string class_name   { get; set; } }
        private class SectionKey { public int class_id    { get; set; } public string section_name { get; set; } }
    }
}
