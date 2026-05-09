namespace AscentSchools.Core.DTOs.School.Staff
{
    public class StaffDto
    {
        public int    StaffId      { get; set; }
        public string StaffName    { get; set; }
        public string EmployeeCode { get; set; }
        public string Designation  { get; set; }
        public string Department   { get; set; }
        public string Mobile       { get; set; }
        public string Email        { get; set; }
        public string JoinDate     { get; set; }
        public string Status       { get; set; }
    }

    public class SaveStaffRequest
    {
        public string StaffName    { get; set; }
        public string EmployeeCode { get; set; }
        public string Designation  { get; set; }
        public string Department   { get; set; }
        public string Mobile       { get; set; }
        public string Email        { get; set; }
        public string JoinDate     { get; set; }
    }

    public class SetStaffStatusRequest
    {
        public string Status { get; set; }
    }

    public class StaffAttendanceGridDto
    {
        public string Date     { get; set; }
        public bool   IsMarked { get; set; }
        public System.Collections.Generic.IEnumerable<StaffAttendanceRowDto> Staff { get; set; }
    }

    public class StaffAttendanceRowDto
    {
        public int    StaffId      { get; set; }
        public string StaffName    { get; set; }
        public string EmployeeCode { get; set; }
        public string Designation  { get; set; }
        public string Status       { get; set; }   // null if not yet marked
        public string Remarks      { get; set; }
    }

    public class SaveStaffAttendanceRequest
    {
        public string Date { get; set; }
        public System.Collections.Generic.IEnumerable<StaffAttendanceEntryDto> Entries { get; set; }
    }

    public class StaffAttendanceEntryDto
    {
        public int    StaffId { get; set; }
        public string Status  { get; set; }
        public string Remarks { get; set; }
    }

    public class StaffAdvanceDto
    {
        public int     AdvanceId    { get; set; }
        public int     StaffId      { get; set; }
        public string  StaffName    { get; set; }
        public string  EmployeeCode { get; set; }
        public string  Designation  { get; set; }
        public string  AdvanceDate  { get; set; }
        public decimal Amount       { get; set; }
        public string  Purpose      { get; set; }
        public string  Remarks      { get; set; }
        public decimal TotalRepaid  { get; set; }
        public decimal Outstanding  { get; set; }
        public string  Status       { get; set; }
        public string  CreatedBy    { get; set; }
    }

    public class CreateAdvanceRequest
    {
        public int     StaffId     { get; set; }
        public string  AdvanceDate { get; set; }
        public decimal Amount      { get; set; }
        public string  Purpose     { get; set; }
        public string  Remarks     { get; set; }
    }

    public class StaffAdvanceRepaymentDto
    {
        public int     RepaymentId   { get; set; }
        public int     AdvanceId     { get; set; }
        public string  RepaymentDate { get; set; }
        public decimal Amount        { get; set; }
        public string  Remarks       { get; set; }
        public string  CreatedBy     { get; set; }
    }

    public class AddRepaymentRequest
    {
        public string  RepaymentDate { get; set; }
        public decimal Amount        { get; set; }
        public string  Remarks       { get; set; }
    }

    public class StaffAdvanceSummaryDto
    {
        public int     StaffId        { get; set; }
        public string  EmployeeCode   { get; set; }
        public string  StaffName      { get; set; }
        public string  Designation    { get; set; }
        public decimal TotalAdvanced  { get; set; }
        public decimal TotalRepaid    { get; set; }
        public decimal Outstanding    { get; set; }
    }

    public class StaffAttendanceSummaryDto
    {
        public int    StaffId      { get; set; }
        public string EmployeeCode { get; set; }
        public string StaffName    { get; set; }
        public string Designation  { get; set; }
        public int    Present      { get; set; }
        public int    Absent       { get; set; }
        public int    Late         { get; set; }
        public int    HalfDay      { get; set; }
        public int    OnLeave      { get; set; }
        public int    TotalMarked  { get; set; }
    }

    // ── Salary components ────────────────────────────────────────────────────

    public class SalaryComponentDto
    {
        public int     ComponentId   { get; set; }
        public int     StaffId       { get; set; }
        public string  ComponentName { get; set; }
        public string  ComponentType { get; set; }
        public decimal Amount        { get; set; }
        public int     DisplayOrder  { get; set; }
    }

    public class SaveSalaryComponentsRequest
    {
        public int StaffId { get; set; }
        public System.Collections.Generic.List<SalaryComponentItem> Components { get; set; }
    }

    public class SalaryComponentItem
    {
        public string  ComponentName { get; set; }
        public string  ComponentType { get; set; }
        public decimal Amount        { get; set; }
        public int     DisplayOrder  { get; set; }
    }

    // ── Monthly salaries ─────────────────────────────────────────────────────

    public class StaffSalaryDto
    {
        public int     SalaryId         { get; set; }
        public int     StaffId          { get; set; }
        public string  StaffName        { get; set; }
        public string  EmployeeCode     { get; set; }
        public string  Designation      { get; set; }
        public int     Month            { get; set; }
        public int     Year             { get; set; }
        public decimal GrossEarnings    { get; set; }
        public decimal TotalDeductions  { get; set; }
        public decimal AdvanceDeducted  { get; set; }
        public decimal NetSalary        { get; set; }
        public string  Status           { get; set; }
        public string  Remarks          { get; set; }
        public string  ProcessedBy      { get; set; }
        public string  ProcessedAt      { get; set; }
        public string  PaidDate         { get; set; }
    }

    public class ProcessSalariesRequest
    {
        public int Month { get; set; }
        public int Year  { get; set; }
    }

    public class UpdateSalaryRequest
    {
        public decimal AdvanceDeducted { get; set; }
        public string  Remarks        { get; set; }
    }

    public class StaffSalarySlipDto
    {
        public int     SalaryId         { get; set; }
        public int     StaffId          { get; set; }
        public string  StaffName        { get; set; }
        public string  EmployeeCode     { get; set; }
        public string  Designation      { get; set; }
        public int     Month            { get; set; }
        public int     Year             { get; set; }
        public decimal GrossEarnings    { get; set; }
        public decimal TotalDeductions  { get; set; }
        public decimal AdvanceDeducted  { get; set; }
        public decimal NetSalary        { get; set; }
        public string  Status           { get; set; }
        public string  Remarks          { get; set; }
        public string  ProcessedBy      { get; set; }
        public string  ProcessedAt      { get; set; }
        public string  PaidDate         { get; set; }
        public System.Collections.Generic.List<SalaryItemDto> Items { get; set; }
    }

    public class SalaryItemDto
    {
        public string  ComponentName { get; set; }
        public string  ComponentType { get; set; }
        public decimal Amount        { get; set; }
    }
}
