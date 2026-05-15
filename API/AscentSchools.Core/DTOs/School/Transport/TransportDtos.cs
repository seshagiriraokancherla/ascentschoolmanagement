using System.Collections.Generic;

namespace AscentSchools.Core.DTOs.School.Transport
{
    // ── Buses ─────────────────────────────────────────────────────────────────

    public class BusDto
    {
        public int    BusId          { get; set; }
        public string BusName        { get; set; }
        public string Model          { get; set; }
        public int?   Capacity       { get; set; }
        public string RegistrationNo { get; set; }
        public string Description    { get; set; }
        public string Status         { get; set; }
    }

    public class SaveBusRequest
    {
        public string BusName        { get; set; }
        public string Model          { get; set; }
        public int?   Capacity       { get; set; }
        public string RegistrationNo { get; set; }
        public string Description    { get; set; }
        public string Status         { get; set; }
    }

    // ── Routes ────────────────────────────────────────────────────────────────

    public class BusRouteDto
    {
        public int    RouteId       { get; set; }
        public string RouteName     { get; set; }
        public string RouteCode     { get; set; }
        public string RouteCategory { get; set; }
        public string Description   { get; set; }
        public string Status        { get; set; }
    }

    public class SaveBusRouteRequest
    {
        public string RouteName     { get; set; }
        public string RouteCode     { get; set; }
        public string RouteCategory { get; set; }
        public string Description   { get; set; }
        public string Status        { get; set; }
    }

    // ── Bus Fee Structure ─────────────────────────────────────────────────────

    public class BusFeeStructureDto
    {
        public int     RouteId         { get; set; }
        public string  RouteName       { get; set; }
        public int     AcademicYearId  { get; set; }
        public IEnumerable<BusFeeTermDto> Terms { get; set; }
    }

    public class BusFeeTermDto
    {
        public int      TermId   { get; set; }
        public string   TermName { get; set; }
        public decimal? Amount   { get; set; }
    }

    public class SaveBusFeeStructureRequest
    {
        public int   RouteId        { get; set; }
        public int   AcademicYearId { get; set; }
        public IEnumerable<BusFeeTermEntry> Items { get; set; }
    }

    public class BusFeeTermEntry
    {
        public int     TermId { get; set; }
        public decimal Amount { get; set; }
    }

    // ── Student Transport ─────────────────────────────────────────────────────

    public class StudentTransportDto
    {
        public long   StudentId       { get; set; }
        public int?   StudentUniqueId { get; set; }
        public int?   AcademicYearId  { get; set; }
        public string StudentName     { get; set; }
        public string AdmissionNo     { get; set; }
        public string ClassName       { get; set; }
        public string TransportType   { get; set; }
        public int?   BusRouteId      { get; set; }
        public string RouteName       { get; set; }
        public int?   BusId           { get; set; }
        public string BusName         { get; set; }
        public string BusTrip         { get; set; }
    }

    public class UpdateStudentTransportRequest
    {
        public int    AcademicYearId { get; set; }
        public string TransportType  { get; set; }
        public int?   BusRouteId     { get; set; }
        public int?   BusId          { get; set; }
        public string BusTrip        { get; set; }
    }
}
