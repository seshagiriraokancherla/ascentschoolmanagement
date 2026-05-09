namespace AscentSchools.Core.DTOs.Control.Modules
{
    public class ModuleDto
    {
        public int    ModuleId     { get; set; }
        public string ModuleName   { get; set; }
        public string ModuleCode   { get; set; }
        public string Description  { get; set; }
        public string PlanTier     { get; set; }
        public int?   DisplayOrder { get; set; }
    }
}
