using System;

namespace AscentMigration.Models.Legacy
{
    public class LegacyFeeCategory
    {
        public string    FeeCatgID      { get; set; }
        public string    FeeCategoryNam { get; set; }
        public string    AcdmYear       { get; set; }
        public string    Descrpt        { get; set; }
        public string    CategoryStatus { get; set; }
        public string    CrtBy          { get; set; }
        public DateTime? CrtDat         { get; set; }
        public string    MachID         { get; set; }
        public string    branchID       { get; set; }
        public string    DeleteByData   { get; set; }
    }
}
