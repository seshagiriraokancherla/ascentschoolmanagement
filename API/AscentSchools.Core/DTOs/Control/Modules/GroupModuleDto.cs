namespace AscentSchools.Core.DTOs.Control.Modules
{
    public class GroupModuleDto
    {
        public int    ModuleId   { get; set; }
        public string ModuleName { get; set; }
        public string ModuleCode { get; set; }
        public string PlanTier   { get; set; }
        public bool   IsEnabled  { get; set; }
    }
}
