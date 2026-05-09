using AscentSchools.API.Helpers;
using AscentSchools.Core.DTOs.School.Sms;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    [RoutePrefix("school/sms")]
    public class SmsController : BaseSchoolController
    {
        private readonly SmsRepository _repo;

        public SmsController()
        {
            _repo = new SmsRepository(new TenantConnectionFactory());
        }

        // GET school/sms/recipients?smsType=Absent&date=2025-05-05&classId=&sectionId=&academicYearId=
        [HttpGet, Route("recipients")]
        public HttpResponseMessage GetRecipients(
            [FromUri] string smsType,
            [FromUri] string date          = null,
            [FromUri] int?   classId       = null,
            [FromUri] int?   sectionId     = null,
            [FromUri] int?   academicYearId = null)
        {
            if (string.IsNullOrWhiteSpace(smsType))
                return BadRequest("smsType is required.");

            IEnumerable<SmsRecipientDto> result;

            switch (smsType)
            {
                case "Absent":
                    if (string.IsNullOrWhiteSpace(date))
                        return BadRequest("date is required for Absent SMS.");
                    if (!DateTime.TryParse(date, out var parsedDate))
                        return BadRequest("Invalid date format.");
                    result = _repo.GetAbsentRecipients(
                        Tenant.TenantDbName, Tenant.SchoolId, parsedDate, classId, sectionId);
                    break;

                case "FeeDue":
                    if (!academicYearId.HasValue || academicYearId <= 0)
                        return BadRequest("academicYearId is required for FeeDue SMS.");
                    result = _repo.GetFeeDueRecipients(
                        Tenant.TenantDbName, Tenant.SchoolId, academicYearId.Value, classId, sectionId);
                    break;

                case "Custom":
                    if (!academicYearId.HasValue || academicYearId <= 0)
                        return BadRequest("academicYearId is required for Custom SMS.");
                    result = _repo.GetCustomRecipients(
                        Tenant.TenantDbName, Tenant.SchoolId, academicYearId.Value, classId, sectionId);
                    break;

                default:
                    return BadRequest("smsType must be Absent, FeeDue, or Custom.");
            }

            return Ok(result);
        }

        // POST school/sms/send
        [HttpPost, Route("send")]
        public async Task<HttpResponseMessage> Send([FromBody] SendSmsRequest req)
        {
            if (req == null || req.Recipients == null || !req.Recipients.Any())
                return BadRequest("No recipients provided.");

            var validTypes = new[] { "Absent", "FeeDue", "Custom" };
            if (!validTypes.Contains(req.SmsType))
                return BadRequest("smsType must be Absent, FeeDue, or Custom.");

            if (req.SmsType == "Custom" && string.IsNullOrWhiteSpace(req.CustomMessage))
                return BadRequest("customMessage is required for Custom SMS.");

            var sentBy = Tenant.FullName ?? Tenant.UserId.ToString();

            // Send all recipients in this batch in parallel
            var tasks = req.Recipients.Select(r => Task.Run(() =>
            {
                if (string.IsNullOrWhiteSpace(r.Mobile))
                {
                    var entry = new SmsLogEntry
                    {
                        SmsType = req.SmsType, StudentId = r.StudentId, StudentName = r.StudentName,
                        Mobile = r.Mobile ?? "", Message = "", Status = "Failed",
                        ErrorMessage = "No mobile number", SentBy = sentBy,
                    };
                    _repo.LogSms(Tenant.TenantDbName, Tenant.SchoolId, entry);
                    return new SmsDispatchResult
                    {
                        StudentId = r.StudentId, StudentName = r.StudentName,
                        Mobile = r.Mobile, Success = false, Reason = "No mobile number",
                    };
                }

                string message    = BuildMessage(req.SmsType, r, req.Date, req.CustomMessage);
                string templateId = GetTemplateId(req.SmsType);
                string response   = SmsHelper.SendSms(r.Mobile, message, templateId);
                bool   success    = !response.StartsWith("ERROR=");

                var logEntry = new SmsLogEntry
                {
                    SmsType = req.SmsType, StudentId = r.StudentId, StudentName = r.StudentName,
                    Mobile  = r.Mobile,    Message   = message,
                    Status  = success ? "Sent" : "Failed",
                    ErrorMessage = success ? null : response,
                    SentBy  = sentBy,
                };
                _repo.LogSms(Tenant.TenantDbName, Tenant.SchoolId, logEntry);

                return new SmsDispatchResult
                {
                    StudentId   = r.StudentId,   StudentName = r.StudentName,
                    Mobile      = r.Mobile,       Success    = success,
                    Reason      = success ? null : response,
                };
            }));

            var results = await Task.WhenAll(tasks);

            var sendResult = new SendSmsResult
            {
                Sent   = results.Count(r => r.Success),
                Failed = results.Count(r => !r.Success),
                Errors = results
                    .Where(r => !r.Success)
                    .Select(r => new SmsErrorDto
                    {
                        StudentId   = r.StudentId,
                        StudentName = r.StudentName,
                        Mobile      = r.Mobile,
                        Reason      = r.Reason,
                    })
                    .ToList(),
            };

            return Ok(sendResult, $"{sendResult.Sent} SMS sent, {sendResult.Failed} failed.");
        }

        // GET school/sms/logs?smsType=&dateFrom=&dateTo=
        [HttpGet, Route("logs")]
        public HttpResponseMessage GetLogs(
            [FromUri] string smsType  = null,
            [FromUri] string dateFrom = null,
            [FromUri] string dateTo   = null)
        {
            DateTime? from = string.IsNullOrWhiteSpace(dateFrom) ? (DateTime?)null : DateTime.Parse(dateFrom);
            DateTime? to   = string.IsNullOrWhiteSpace(dateTo)   ? (DateTime?)null : DateTime.Parse(dateTo);
            var logs = _repo.GetLogs(Tenant.TenantDbName, Tenant.SchoolId, smsType, from, to);
            return Ok(logs);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string BuildMessage(string smsType, SmsRecipient r, string date, string customMessage)
        {
            switch (smsType)
            {
                case "Absent":
                    return string.Format(
                        "Dear Parent, your ward {0} was absent on {1}. Please ensure regular attendance.",
                        r.StudentName, date ?? "today");

                case "FeeDue":
                    return string.Format(
                        "Dear Parent, your ward {0} has an outstanding fee of Rs.{1}. Please pay at the earliest.",
                        r.StudentName, r.OutstandingAmount.ToString("F2"));

                case "Custom":
                    return (customMessage ?? "").Replace("{name}", r.StudentName);

                default:
                    return customMessage ?? "";
            }
        }

        private static string GetTemplateId(string smsType)
        {
            switch (smsType)
            {
                case "Absent": return SmsHelper.AbsentTemplateId;
                case "FeeDue": return SmsHelper.FeeDueTemplateId;
                default:       return SmsHelper.CustomTemplateId;
            }
        }

        private class SmsDispatchResult
        {
            public long   StudentId   { get; set; }
            public string StudentName { get; set; }
            public string Mobile      { get; set; }
            public bool   Success     { get; set; }
            public string Reason      { get; set; }
        }
    }
}
