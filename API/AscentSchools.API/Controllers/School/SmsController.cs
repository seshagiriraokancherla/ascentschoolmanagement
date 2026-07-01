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

            // Capture tenant values into locals — the parallel Task.Run threads below have
            // no HttpContext, so TenantContext.Current (and thus Tenant.*) is null there.
            var dbName   = Tenant.TenantDbName;
            var schoolId = Tenant.SchoolId;

            // Load this school's gateway account + the template for the requested type.
            var account = _repo.GetSmsAccount(Tenant.TenantDbName, Tenant.SchoolId);
            if (account == null || !account.IsEnabled)
                return BadRequest("SMS gateway is not configured or is disabled for this school. Configure it in Settings → SMS Gateway.");

            var templateKey = TemplateKeyFor(req.SmsType);
            var template    = _repo.GetTemplate(Tenant.TenantDbName, Tenant.SchoolId, templateKey);
            if (template == null || !template.IsActive || string.IsNullOrWhiteSpace(template.TemplateId))
                return BadRequest($"No active SMS template (with a DLT template id) is configured for '{req.SmsType}'. Set it in Settings → SMS Gateway.");

            var tplId        = template.TemplateId;
            var templateText = template.MessageText;

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
                    _repo.LogSms(dbName, schoolId, entry);
                    return new SmsDispatchResult
                    {
                        StudentId = r.StudentId, StudentName = r.StudentName,
                        Mobile = r.Mobile, Success = false, Reason = "No mobile number",
                    };
                }

                string message  = BuildMessage(req.SmsType, templateText, r, req.Date, req.CustomMessage);
                string response = SmsHelper.SendSms(
                    account.ApiUrl, account.Username, account.ApiKey, account.SenderId,
                    r.Mobile, message, tplId);
                bool   success  = !response.StartsWith("ERROR=");

                var logEntry = new SmsLogEntry
                {
                    SmsType = req.SmsType, StudentId = r.StudentId, StudentName = r.StudentName,
                    Mobile  = r.Mobile,    Message   = message,
                    Status  = success ? "Sent" : "Failed",
                    ErrorMessage = success ? null : response,
                    SentBy  = sentBy,
                };
                _repo.LogSms(dbName, schoolId, logEntry);

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

        // ── Gateway config endpoints (Settings → SMS Gateway) ──────────────────

        // GET school/sms/config
        [HttpGet, Route("config")]
        public HttpResponseMessage GetConfig()
        {
            // Returns null if never configured — the UI shows a blank form.
            return Ok(_repo.GetSmsConfig(Tenant.TenantDbName, Tenant.SchoolId));
        }

        // PUT school/sms/config
        [HttpPut, Route("config")]
        public HttpResponseMessage SaveConfig([FromBody] UpdateSmsConfigRequest req)
        {
            if (req == null) return BadRequest("No data provided.");
            if (string.IsNullOrWhiteSpace(req.ApiUrl) ||
                string.IsNullOrWhiteSpace(req.Username) ||
                string.IsNullOrWhiteSpace(req.SenderId))
                return BadRequest("API URL, Username and Sender ID are required.");

            var by = Tenant.FullName ?? Tenant.UserId.ToString();
            _repo.UpsertSmsConfig(Tenant.TenantDbName, Tenant.SchoolId, req, by);
            return Ok(_repo.GetSmsConfig(Tenant.TenantDbName, Tenant.SchoolId), "SMS gateway settings saved.");
        }

        // POST school/sms/templates  (add or edit by template_key)
        [HttpPost, Route("templates")]
        public HttpResponseMessage SaveTemplate([FromBody] SaveSmsTemplateRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.TemplateKey))
                return BadRequest("templateKey is required.");

            _repo.UpsertTemplate(Tenant.TenantDbName, Tenant.SchoolId, req);
            return Ok(_repo.GetTemplates(Tenant.TenantDbName, Tenant.SchoolId), "Template saved.");
        }

        // DELETE school/sms/templates/{key}
        [HttpDelete, Route("templates/{key}")]
        public HttpResponseMessage DeleteTemplate(string key)
        {
            _repo.DeleteTemplate(Tenant.TenantDbName, Tenant.SchoolId, key);
            return Ok(_repo.GetTemplates(Tenant.TenantDbName, Tenant.SchoolId), "Template deleted.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string TemplateKeyFor(string smsType)
        {
            switch (smsType)
            {
                case "Absent": return "ABSENT";
                case "FeeDue": return "FEE_DUE";
                default:       return "CUSTOM";
            }
        }

        // Custom uses the text typed at send time; other types use the school's stored template text.
        private static string BuildMessage(string smsType, string templateText, SmsRecipient r, string date, string customMessage)
        {
            string text = smsType == "Custom" ? (customMessage ?? "") : (templateText ?? "");
            return text
                .Replace("{name}",        r.StudentName ?? "")
                .Replace("{class}",       r.ClassName   ?? "")
                .Replace("{admissionNo}", r.AdmissionNo ?? "")
                .Replace("{date}",        date ?? "")
                .Replace("{amount}",      r.OutstandingAmount.ToString("F2"));
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
