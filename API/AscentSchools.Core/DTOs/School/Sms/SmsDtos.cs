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
}
