using System;
using System.Collections.Generic;

namespace AscentSchools.Core.DTOs.School.Hostel
{
    public class HostelDto
    {
        public int      HostelId    { get; set; }
        public string   HostelName  { get; set; }
        public string   Description { get; set; }
        public int?     Capacity    { get; set; }
        public string   ContactNo   { get; set; }
        public string   Address     { get; set; }
        public int?     NoOfRooms   { get; set; }
        public int      SchoolId    { get; set; }
        public string   CreatedBy   { get; set; }
        public DateTime CreatedAt   { get; set; }
    }

    public class SaveHostelRequest
    {
        public string HostelName  { get; set; }
        public string Description { get; set; }
        public int?   Capacity    { get; set; }
        public string ContactNo   { get; set; }
        public string Address     { get; set; }
        public int?   NoOfRooms   { get; set; }
    }

    // ── Fee Structure ─────────────────────────────────────────────────────────

    public class HostelFeeStructureDto
    {
        public int     HostelFeeStructureId { get; set; }
        public int     HostelId      { get; set; }
        public string  HostelName    { get; set; }
        public int     AcademicYearId { get; set; }
        public string  AcademicYear  { get; set; }
        public int?    TermId        { get; set; }
        public string  TermName      { get; set; }
        public int?    OrderNo       { get; set; }
        public int?    FeePeriodId   { get; set; }
        public string  PeriodLabel   { get; set; }
        public int?    PeriodSequenceNo { get; set; }
        public string  PaymentType   { get; set; }
        public decimal Amount        { get; set; }
    }

    public class SaveHostelFeeStructureRequest
    {
        public int    HostelId       { get; set; }
        public int    AcademicYearId { get; set; }
        public string PaymentType    { get; set; }   // Term | Monthly
        public List<HostelFeeStructureItem> Items { get; set; }
    }

    public class HostelFeeStructureItem
    {
        public int?    TermId      { get; set; }
        public int?    FeePeriodId { get; set; }
        public decimal Amount      { get; set; }
    }

    // ── Student Assignment ────────────────────────────────────────────────────

    public class StudentHostelDto
    {
        public long   StudentId       { get; set; }
        public int    StudentUniqueId { get; set; }
        public string StudentName     { get; set; }
        public string AdmissionNo     { get; set; }
        public string ClassName       { get; set; }
        public string SectionName     { get; set; }
        public int    AcademicYearId  { get; set; }
        public string AcademicYear    { get; set; }
        public int?   HostelId        { get; set; }
        public string HostelName      { get; set; }
    }

    public class UpdateStudentHostelRequest
    {
        public int? HostelId       { get; set; }
        public int  AcademicYearId { get; set; }
    }

    // ── Bulk import ───────────────────────────────────────────────────────────

    public class BulkHostelFeeStructureRow
    {
        public int     RowNumber    { get; set; }
        public string  HostelName   { get; set; }
        public string  AcademicYear { get; set; }
        public string  PaymentType  { get; set; }
        public string  TermName     { get; set; }
        public string  PeriodLabel  { get; set; }
        public string  Amount       { get; set; }
    }

    public class BulkHostelFeeStructureRequest
    {
        public List<BulkHostelFeeStructureRow> Rows { get; set; }
    }
}
