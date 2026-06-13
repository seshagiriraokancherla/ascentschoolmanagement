using System;

namespace AscentMigration.Models.Legacy
{
    public class LegacyBus
    {
        public string    BusID        { get; set; }
        public string    BusNam       { get; set; }
        public string    BusModelDet  { get; set; }
        public string    DriverID     { get; set; }
        public string    BusCapacity  { get; set; }
        public string    Descrpt      { get; set; }
        public string    BusStat      { get; set; }
        public string    CrtBy        { get; set; }
        public DateTime? CrtDate      { get; set; }
        public string    BranchID     { get; set; }
        public string    MachID       { get; set; }
        public string    DeletBy      { get; set; }
        public DateTime? PurDat       { get; set; }
        public string    OwnderData   { get; set; }
        public string    CleanerNam   { get; set; }
        public string    TripData     { get; set; }
        public string    RouteNam     { get; set; }
        public string    BusRegNo     { get; set; }
    }
}
