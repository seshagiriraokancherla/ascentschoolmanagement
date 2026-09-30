using AscentSchools.Core.DTOs.School.Homework;
using AscentSchools.Core.DTOs.School.Sms;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AscentSchools.API.Helpers
{
    /// <summary>
    /// Sends an SMS to the parents of every student in the affected sections when Daily
    /// Homework is saved — gated by the per-branch "Homework SMS" toggle in School Settings.
    /// Fire-and-forget: runs on a background task and swallows all errors so an SMS failure
    /// never affects the homework save, which has already succeeded by the time this runs.
    ///
    /// Two message shapes, chosen automatically per school by which template is configured
    /// (Settings → SMS Gateway) — no hardcoded school check needed:
    ///   - RICH_TEMPLATE_KEY ("HOMEWORKNEW"): a fixed 9-subject-slot layout (currently
    ///     sfsgwk's own DLT-registered format — Eng/Tel/Hin/Mat/Sci/Soc/EVS/Bio/IIT, each with
    ///     up to 3×30-char var chunks). Takes priority when active + has a DLT template id.
    ///   - SIMPLE_TEMPLATE_KEY ("HOMEWORK"): the original single-message template using the
    ///     ordinary {name}/{class}/{admissionNo}/{date} placeholders (Phase 141). Used when the
    ///     rich one isn't configured.
    ///
    /// IMPORTANT: callers MUST pass primitive values captured on the request thread. Never
    /// read TenantContext/HttpContext inside the Task — worker threads have no HttpContext
    /// (see decision: Phase 61 SMS Task.Run NRE).
    /// </summary>
    public class HomeworkSmsNotifier
    {
        private const string SimpleTemplateKey = "HOMEWORK";
        private const string RichTemplateKey   = "HOMEWORKNEW";

        // Fixed slot order + the subjects.short_name each slot matches (case-insensitive).
        // Currently sfsgwk-specific — a subject whose short_name isn't in this list has no
        // slot and is silently left out of the rich SMS (the homework itself still saves).
        private static readonly string[] SlotShortNames = { "Eng", "Tel", "Hin", "Mat", "Sci", "Soc", "EVS", "Bio", "IIT" };

        private readonly SmsRepository            _sms;
        private readonly SchoolSettingsRepository  _settings;

        public HomeworkSmsNotifier()
        {
            var db = new TenantConnectionFactory();
            _sms      = new SmsRepository(db);
            _settings = new SchoolSettingsRepository(db);
        }

        /// <summary>
        /// sectionIds: the resolved target sections from CreateBatchHomework's
        /// BatchHomeworkResult.Sections (a single null entry means the whole class).
        /// items: the same subject+description pairs just saved — identical across every
        /// target section in this one request, so the subject-text map is built once.
        /// </summary>
        public void NotifyClass(string dbName, int schoolId, int classId, IReadOnlyList<int?> sectionIds,
                                 DateTime assignedDate, List<BatchHomeworkItem> items, string sentBy)
        {
            Task.Run(() =>
            {
                try
                {
                    var settings = _settings.Get(schoolId);
                    if (!settings.HomeworkSmsEnabled) return;

                    var account = _sms.GetSmsAccount(dbName, schoolId);
                    if (account == null || !account.IsEnabled) return;

                    var richTemplate = _sms.GetTemplate(dbName, schoolId, RichTemplateKey);
                    var useRich = richTemplate != null && richTemplate.IsActive
                                  && !string.IsNullOrWhiteSpace(richTemplate.TemplateId);

                    SmsTemplateDto simpleTemplate = null;
                    if (!useRich)
                    {
                        simpleTemplate = _sms.GetTemplate(dbName, schoolId, SimpleTemplateKey);
                        if (simpleTemplate == null || !simpleTemplate.IsActive || string.IsNullOrWhiteSpace(simpleTemplate.TemplateId))
                            return;   // neither template configured yet — silent no-op
                    }

                    // Subject text, keyed by short_name — same for every section in this save.
                    Dictionary<string, string> subjectTextByShortName = null;
                    if (useRich)
                    {
                        var subjectIds = (items ?? new List<BatchHomeworkItem>())
                            .Where(i => i.SubjectId.HasValue).Select(i => i.SubjectId.Value).ToList();
                        var shortNames = _sms.GetSubjectShortNames(dbName, subjectIds);

                        subjectTextByShortName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var item in items ?? new List<BatchHomeworkItem>())
                        {
                            if (!item.SubjectId.HasValue) continue;
                            if (!shortNames.TryGetValue(item.SubjectId.Value, out var sn) || string.IsNullOrWhiteSpace(sn)) continue;
                            subjectTextByShortName[sn.Trim()] = item.Description ?? "";
                        }
                    }

                    foreach (var sectionId in sectionIds)
                    {
                        var recipients = _sms.GetHomeworkRecipients(dbName, schoolId, classId, sectionId).ToList();
                        if (recipients.Count == 0) continue;

                        string richMessage = null;
                        string templateId;
                        if (useRich)
                        {
                            richMessage = BuildRichMessage(
                                recipients[0].ClassName, recipients[0].SectionName, assignedDate, subjectTextByShortName);
                            templateId = richTemplate.TemplateId;
                        }
                        else
                        {
                            templateId = simpleTemplate.TemplateId;
                        }

                        foreach (var r in recipients)
                        {
                            var text = useRich
                                ? richMessage
                                : (simpleTemplate.MessageText ?? "")
                                    .Replace("{name}",        r.StudentName ?? "")
                                    .Replace("{class}",       r.ClassName   ?? "")
                                    .Replace("{admissionNo}", r.AdmissionNo ?? "")
                                    .Replace("{date}",        assignedDate.ToString("yyyy-MM-dd"));

                            var response = SmsHelper.SendSms(
                                account.ApiUrl, account.Username, account.ApiKey, account.SenderId,
                                r.FatherMobile, text, templateId);
                            var success = !response.StartsWith("ERROR=", StringComparison.Ordinal);

                            _sms.LogSms(dbName, schoolId, new SmsLogEntry
                            {
                                SmsType      = "Homework",
                                StudentId    = r.StudentId,
                                StudentName  = r.StudentName,
                                Mobile       = r.FatherMobile,
                                Message      = text,
                                Status       = success ? "Sent" : "Failed",
                                ErrorMessage = success ? null : response,
                                SentBy       = sentBy,
                            });
                        }
                    }
                }
                catch { /* best-effort */ }
            });
        }

        // ── Rich (sfsgwk) message builder ───────────────────────────────────────

        /// <summary>
        /// "Dear Parent, {classOrdinal}-{section} H.W({d-MMM})" (blind-truncated to 30 chars),
        /// then one blank-line-separated block per fixed subject slot, each with up to
        /// 3×30-char blind-sliced chunks of that subject's homework text (rest ignored).
        /// Must reproduce the DLT-registered template's static wording/line breaks exactly —
        /// any deviation and the operator will reject the send.
        /// </summary>
        private static string BuildRichMessage(string className, string sectionName, DateTime assignedDate,
                                                Dictionary<string, string> subjectTextByShortName)
        {
            var header = $"{MapClassOrdinal(className)}-{sectionName} H.W({assignedDate.ToString("d-MMM", CultureInfo.InvariantCulture)})";
            if (header.Length > 30) header = header.Substring(0, 30);

            var sb = new StringBuilder();
            sb.Append("Dear Parent, ").Append(header).Append("\n\n");

            foreach (var slot in SlotShortNames)
            {
                subjectTextByShortName.TryGetValue(slot, out var text);
                var chunks = Chunk30(text);
                sb.Append(slot).Append(": ").Append(chunks[0]).Append(chunks[1]).Append(chunks[2]).Append("\n\n");
            }

            sb.Append("Principal-SFS");
            return sb.ToString();
        }

        // Blind character slice — no word-boundary awareness (confirmed with the school).
        private static string[] Chunk30(string text)
        {
            var t = text ?? "";
            var result = new string[3];
            for (var i = 0; i < 3; i++)
            {
                var start = i * 30;
                result[i] = start < t.Length ? t.Substring(start, Math.Min(30, t.Length - start)) : "";
            }
            return result;
        }

        // "8"/"8 Class"/"Class 8" → "8th"; Nursery/LKG/UKG → their fixed short forms.
        // Falls back to the raw class name if nothing matches (never blank the SMS).
        private static string MapClassOrdinal(string className)
        {
            var c = (className ?? "").Trim();
            var lower = c.ToLowerInvariant();
            if (lower.Contains("nursery")) return "Nur";
            if (lower.Contains("lkg")) return "LKG";
            if (lower.Contains("ukg")) return "UKG";

            var m = Regex.Match(c, @"\d+");
            if (m.Success && int.TryParse(m.Value, out var n))
            {
                string suffix;
                switch (n % 100)
                {
                    case 11: case 12: case 13:
                        suffix = "th";
                        break;
                    default:
                        switch (n % 10)
                        {
                            case 1: suffix = "st"; break;
                            case 2: suffix = "nd"; break;
                            case 3: suffix = "rd"; break;
                            default: suffix = "th"; break;
                        }
                        break;
                }
                return n + suffix;
            }

            return c;
        }
    }
}
