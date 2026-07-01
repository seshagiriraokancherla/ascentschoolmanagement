using System.Collections.Generic;

namespace AscentSchools.Core.DTOs.School.Sms
{
    public class SmsRecipientDto
    {
        public long    StudentId         { get; set; }
        public string  StudentName       { get; set; }
        public string  AdmissionNo       { get; set; }
        public string  ClassName         { get; set; }
        public string  SectionName       { get; set; }
        public string  FatherMobile      { get; set; }
        public decimal OutstandingAmount { get; set; }  // populated for FeeDue type only
        public string  AttendanceDate    { get; set; }  // populated for Absent type only
    }

    public class SendSmsRequest
    {
        public string              SmsType       { get; set; }  // Absent / FeeDue / Custom
        public string              Date          { get; set; }  // for Absent — YYYY-MM-DD
        public string              CustomMessage { get; set; }  // for Custom type; use {name} placeholder
        public List<SmsRecipient>  Recipients    { get; set; }
    }

    public class SmsRecipient
    {
        public long    StudentId         { get; set; }
        public string  StudentName       { get; set; }
        public string  AdmissionNo       { get; set; }  // for {admissionNo} placeholder
        public string  ClassName         { get; set; }  // for {class} placeholder
        public string  Mobile            { get; set; }
        public decimal OutstandingAmount { get; set; }  // for FeeDue
    }

    public class SendSmsResult
    {
        public int              Sent   { get; set; }
        public int              Failed { get; set; }
        public List<SmsErrorDto> Errors { get; set; }
    }

    public class SmsErrorDto
    {
        public long   StudentId   { get; set; }
        public string StudentName { get; set; }
        public string Mobile      { get; set; }
        public string Reason      { get; set; }
    }

    public class SmsLogDto
    {
        public int    LogId        { get; set; }
        public string SmsType     { get; set; }
        public string StudentName { get; set; }
        public string Mobile      { get; set; }
        public string Message     { get; set; }
        public string Status      { get; set; }
        public string ErrorMessage { get; set; }
        public string SentBy      { get; set; }
        public string SentAt      { get; set; }
    }

    public class SmsLogEntry
    {
        public string  SmsType      { get; set; }
        public long    StudentId    { get; set; }
        public string  StudentName  { get; set; }
        public string  Mobile       { get; set; }
        public string  Message      { get; set; }
        public string  Status       { get; set; }
        public string  ErrorMessage { get; set; }
        public string  SentBy       { get; set; }
    }

    // ── Per-school SMS gateway configuration (SMS Center) ──────────────────

    /// <summary>Gateway account returned to the settings page.
    /// ApiKey is intentionally omitted — only HasApiKey is exposed.</summary>
    public class SmsConfigDto
    {
        public int    ConfigId  { get; set; }
        public string Provider  { get; set; }
        public string ApiUrl    { get; set; }
        public string Username  { get; set; }
        public string SenderId  { get; set; }
        public bool   HasApiKey { get; set; }   // true if api_key stored; value not returned
        public bool   IsEnabled { get; set; }
        public string UpdatedAt { get; set; }
        public List<SmsTemplateDto> Templates { get; set; }
    }

    public class UpdateSmsConfigRequest
    {
        public string Provider  { get; set; }
        public string ApiUrl    { get; set; }
        public string Username  { get; set; }
        public string ApiKey    { get; set; }   // blank/null = keep existing
        public string SenderId  { get; set; }
        public bool   IsEnabled { get; set; }
    }

    public class SmsTemplateDto
    {
        public int    TemplateRowId { get; set; }
        public string TemplateKey   { get; set; }
        public string Title         { get; set; }
        public string TemplateId    { get; set; }   // DLT template id
        public string MessageText   { get; set; }
        public string Placeholders  { get; set; }
        public bool   IsActive      { get; set; }
    }

    public class SaveSmsTemplateRequest
    {
        public string TemplateKey  { get; set; }
        public string Title        { get; set; }
        public string TemplateId   { get; set; }
        public string MessageText  { get; set; }
        public string Placeholders { get; set; }
        public bool   IsActive     { get; set; }
    }

    /// <summary>Internal — gateway account WITH the api_key, for the send path only.
    /// Never serialized to a GET response.</summary>
    public class SmsAccount
    {
        public string Provider  { get; set; }
        public string ApiUrl    { get; set; }
        public string Username  { get; set; }
        public string ApiKey    { get; set; }
        public string SenderId  { get; set; }
        public bool   IsEnabled { get; set; }
    }
}
