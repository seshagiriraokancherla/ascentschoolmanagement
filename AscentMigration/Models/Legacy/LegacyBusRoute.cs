using System;

namespace AscentMigration.Models.Legacy
{
    public class LegacyBusRoute
    {
        public string    RouteID       { get; set; }
        public string    RouteNam      { get; set; }
        public string    RouteCod      { get; set; }
        public string    BusNoData     { get; set; }
        public string    RouteCategory { get; set; }
        public string    Descrpt       { get; set; }
        public string    RouteStatus   { get; set; }
        public string    CrtBy         { get; set; }
        public DateTime? CrtDate       { get; set; }
        public string    BranchID      { get; set; }
        public string    MachID        { get; set; }
        public string    DeleteData    { get; set; }
    }
}
