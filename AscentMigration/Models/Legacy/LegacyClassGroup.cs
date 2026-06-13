using System;

namespace AscentMigration.Models.Legacy
{
    public class LegacyClassGroup
    {
        public string    ClsGrpID { get; set; }
        public string    GrpNam   { get; set; }
        public string    GrpDesc  { get; set; }
        public string    GrpPrfx  { get; set; }
        public string    GrpStat  { get; set; }
        public string    CrtBy    { get; set; }
        public DateTime? CrtDat   { get; set; }
        public string    BranchID { get; set; }
        public string    MachID   { get; set; }
        public string    DeletBy  { get; set; }
    }
}
