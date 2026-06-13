using System;
using System.Collections.Generic;

namespace AscentSyncTool.Models
{
    // ── Bulk receipt import (POST /school/fees/receipts/bulk) ──────────────────
    public class BulkReceiptRow
    {
        public string LegacyReceiptNo { get; set; }   // = FeeReciptID → receipt_no
        public string AdmissionNo     { get; set; }
        public string AcademicYear    { get; set; }
        public string FeeCategory     { get; set; }   // "School" for standard fees
        public string FeeTypeName     { get; set; }
        public string TermName        { get; set; }
        public string PaidAmount      { get; set; }   // string → decimal server-side
        public string PaymentDate     { get; set; }   // yyyy-MM-dd
        public string PaymentMode     { get; set; }
        public string ChequeNo        { get; set; }
        public string Remarks         { get; set; }
        public string Status          { get; set; }   // A / D
        public string CancelReason    { get; set; }
    }

    public class BulkReceiptImportRequest
    {
        public List<BulkReceiptRow> Rows { get; set; } = new List<BulkReceiptRow>();
    }

    // ── Bulk student import (POST /school/students/bulk) ───────────────────────
    public class StudentBulkRow
    {
        public string AdmissionNo  { get; set; }
        public string StudentName  { get; set; }
        public string Gender       { get; set; }
        public string DateOfBirth  { get; set; }   // dd/MM/yyyy
        public string AcademicYear { get; set; }
        public string ClassName    { get; set; }
        public string SectionName  { get; set; }
        public string RollNo       { get; set; }
        public string FeeCategory  { get; set; }
        public string FatherName   { get; set; }
        public string MotherName   { get; set; }
        public string FatherMobile { get; set; }
        public string MotherMobile { get; set; }
        public string AadharNo     { get; set; }
        public string Caste        { get; set; }
        public string CasteCode    { get; set; }
        public string Religion     { get; set; }
        public string JoiningClass { get; set; }
        public string MotherTongue { get; set; }
        public string Status       { get; set; }   // Active / Inactive
    }

    public class BulkStudentImportRequest
    {
        public List<StudentBulkRow> Rows { get; set; } = new List<StudentBulkRow>();
        public bool Upsert { get; set; } = true;   // tool always upserts (update existing)
    }

    // ── Bulk import result ─────────────────────────────────────────────────────
    public class BulkRowError
    {
        public int    Row         { get; set; }
        public string AdmissionNo { get; set; }
        public string Identifier  { get; set; }
        public string Reason      { get; set; }
    }

    public class BulkImportResult
    {
        public int                Total    { get; set; }
        public int                Imported { get; set; }
        public int                Updated  { get; set; }
        public int                Skipped  { get; set; }
        public int                Failed   { get; set; }
        public List<BulkRowError> Errors   { get; set; } = new List<BulkRowError>();
    }

    // ── Receipts list (GET /school/fees/receipts) ──────────────────────────────
    public class ReceiptListItem
    {
        public int      ReceiptId       { get; set; }
        public string   ReceiptNo       { get; set; }
        public string   StudentName     { get; set; }
        public string   AdmissionNo     { get; set; }
        public string   ClassName       { get; set; }
        public DateTime PaymentDate     { get; set; }
        public decimal  TotalAmount     { get; set; }
        public string   PaymentModeName { get; set; }
        public string   Status          { get; set; }
    }

    // ── Receipt detail (GET /school/fees/receipts/{id}) ────────────────────────
    public class ReceiptDetail
    {
        public int      ReceiptId       { get; set; }
        public string   ReceiptNo       { get; set; }
        public string   StudentName     { get; set; }
        public string   AdmissionNo     { get; set; }
        public string   ClassName       { get; set; }
        public string   AcademicYear    { get; set; }
        public DateTime PaymentDate     { get; set; }
        public decimal  TotalAmount     { get; set; }
        public string   PaymentModeName { get; set; }
        public string   ChequeNo        { get; set; }
        public string   BankName        { get; set; }
        public string   Status          { get; set; }
        public string   Remarks         { get; set; }
        public List<ReceiptItem> Items  { get; set; } = new List<ReceiptItem>();
    }

    public class ReceiptItem
    {
        public string  FeeTypeName      { get; set; }
        public string  RouteName        { get; set; }
        public string  HostelName       { get; set; }
        public string  TermName         { get; set; }
        public decimal Amount           { get; set; }
        public decimal ConcessionAmount { get; set; }
        public decimal NetAmount        { get; set; }
    }

    // ── Standard API envelope ──────────────────────────────────────────────────
    public class ApiResponse<T>
    {
        public bool   success { get; set; }
        public T      data    { get; set; }
        public string message { get; set; }
    }
}
