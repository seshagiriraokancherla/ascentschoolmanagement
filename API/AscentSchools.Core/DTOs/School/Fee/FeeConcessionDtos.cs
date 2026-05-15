using System;
using System.Collections.Generic;

namespace AscentSchools.Core.DTOs.School.Fee
{
    public class FeeConcessionDto
    {
        public int      ConcessionId   { get; set; }
        public long     StudentId      { get; set; }
        public string   StudentName    { get; set; }
        public string   AdmissionNo    { get; set; }
        public string   ClassName      { get; set; }
        public int?     FeeTypeId      { get; set; }
        public string   FeeTypeName    { get; set; }
        public int?     BusRouteId     { get; set; }  // populated for transport concessions
        public string   RouteName      { get; set; }  // populated for transport concessions
        public int?     HostelId       { get; set; }  // populated for hostel concessions
        public string   HostelName     { get; set; }  // populated for hostel concessions
        public int?     TermId         { get; set; }
        public string   TermName       { get; set; }
        public int?     FeePeriodId    { get; set; }
        public string   PeriodLabel    { get; set; }
        public string   ConcessionType { get; set; }
        public decimal  Amount         { get; set; }
        public string   Remarks        { get; set; }
        public string   ReceiptNo      { get; set; }
        public int      AcademicYearId { get; set; }
        public string   AcademicYear   { get; set; }
        public string   CreatedBy      { get; set; }
        public DateTime CreatedAt      { get; set; }
    }

    public class SaveFeeConcessionRequest
    {
        public long    StudentId      { get; set; }
        public int?    FeeTypeId      { get; set; }   // null for transport/hostel concessions
        public int?    BusRouteId     { get; set; }   // set for transport concessions
        public int?    HostelId       { get; set; }   // set for hostel concessions
        public int?    TermId         { get; set; }
        public int?    FeePeriodId    { get; set; }
        public int     AcademicYearId { get; set; }
        public string  ConcessionType { get; set; }
        public decimal Amount         { get; set; }
        public string  Remarks        { get; set; }
    }

    public class BulkSaveFeeConcessionRequest
    {
        public int     AcademicYearId { get; set; }
        public int?    FeeTypeId      { get; set; }   // null for transport/hostel concessions
        public int?    BusRouteId     { get; set; }   // set for transport concessions
        public int?    HostelId       { get; set; }   // set for hostel concessions
        public int?    TermId         { get; set; }
        public int?    FeePeriodId    { get; set; }
        public string  ConcessionType { get; set; }
        public List<BulkFeeConcessionRow> Rows { get; set; }
    }

    public class BulkFeeConcessionRow
    {
        public long    StudentId { get; set; }
        public decimal Amount    { get; set; }
        public string  Remarks   { get; set; }
    }
}
