using AscentSchools.Core.DTOs.Control.Modules;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System.Collections.Generic;

namespace AscentSchools.Data.Repositories.Control
{
    public class ModuleRepository
    {
        private readonly IConnectionFactory _db;
        public ModuleRepository(IConnectionFactory db) { _db = db; }

        public IEnumerable<ModuleDto> GetAll()
        {
            using (var conn = _db.GetMasterConnection())
                return conn.Query<ModuleDto>(
                    @"SELECT module_id ModuleId, module_name ModuleName, module_code ModuleCode,
                             description Description, plan_tier PlanTier, display_order DisplayOrder
                      FROM modules WHERE status = 'Active' ORDER BY display_order");
        }

        public IEnumerable<GroupModuleDto> GetGroupModules(int groupId)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.Query<GroupModuleDto>(
                    @"SELECT m.module_id ModuleId, m.module_name ModuleName, m.module_code ModuleCode,
                             m.plan_tier PlanTier, ISNULL(gm.is_enabled, 1) IsEnabled
                      FROM modules m
                      LEFT JOIN group_modules gm ON m.module_id = gm.module_id AND gm.group_id = @groupId
                      WHERE m.status = 'Active'
                      ORDER BY m.display_order",
                    new { groupId });
        }

        /// <summary>
        /// Returns true if the module is enabled for the given group + school branch.
        /// Hierarchy: group disabled → always off (hard block).
        ///            school row exists → use school setting.
        ///            no school row → inherit group setting (default on if no row).
        /// Returns false if the module code is not found.
        /// </summary>
        public bool IsModuleEnabled(int groupId, int? schoolId, string moduleCode)
        {
            using (var conn = _db.GetMasterConnection())
            {
                var result = conn.QueryFirstOrDefault<bool?>(
                    @"SELECT
                          CASE
                              WHEN ISNULL(gm.is_enabled, 1) = 0 THEN CAST(0 AS BIT)
                              WHEN sm.school_id IS NOT NULL      THEN sm.is_enabled
                              ELSE ISNULL(gm.is_enabled, 1)
                          END
                      FROM modules m
                      LEFT JOIN group_modules  gm ON gm.module_id = m.module_id AND gm.group_id  = @groupId
                      LEFT JOIN school_modules sm ON sm.module_id = m.module_id AND sm.school_id = @schoolId
                      WHERE m.module_code = @moduleCode AND m.status = 'Active'",
                    new { groupId, schoolId, moduleCode });

                return result ?? false;   // module not found in master list → treat as disabled
            }
        }

        public void UpdateGroupModule(int groupId, int moduleId, bool isEnabled)
        {
            using (var conn = _db.GetMasterConnection())
                conn.Execute(
                    @"IF EXISTS (SELECT 1 FROM group_modules WHERE group_id = @groupId AND module_id = @moduleId)
                          UPDATE group_modules SET is_enabled = @isEnabled, updated_at = GETDATE()
                          WHERE group_id = @groupId AND module_id = @moduleId
                      ELSE
                          INSERT INTO group_modules (group_id, module_id, is_enabled)
                          VALUES (@groupId, @moduleId, @isEnabled)",
                    new { groupId, moduleId, isEnabled });
        }
    }
}
