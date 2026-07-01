using AscentSchools.Core.DTOs.School.Sms;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System;
using System.Collections.Generic;

namespace AscentSchools.Data.Repositories.School
{
    public class SmsRepository
    {
        private readonly IConnectionFactory _db;
        public SmsRepository(IConnectionFactory db) { _db = db; }

        // ── Recipient queries ─────────────────────────────────────────────────

        /// <summary>Returns students who were Absent on the given date.</summary>
        public IEnumerable<SmsRecipientDto> GetAbsentRecipients(
            string tenantDbName, int schoolId, DateTime date, int? classId, int? sectionId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<SmsRecipientDto>(
                    @"SELECT s.student_id                                       StudentId,
                             s.student_name                                     StudentName,
                             s.admission_no                                     AdmissionNo,
                             ISNULL(c.class_name,   '')                        ClassName,
                             ISNULL(sec.section_name,'')                       SectionName,
                             s.father_mobile                                    FatherMobile,
                             CONVERT(VARCHAR(10), sa.attendance_date, 105)     AttendanceDate
                      FROM   students s
                      JOIN   student_attendance sa
                             ON  sa.student_id = s.student_id
                             AND sa.school_id  = s.school_id
                             AND sa.status     = 'Absent'
                      LEFT JOIN classes  c   ON c.class_id    = s.class_id
                      LEFT JOIN sections sec ON sec.section_id = s.section_id
                      WHERE  s.school_id      = @schoolId
                        AND  sa.attendance_date = @date
                        AND  s.status         IN ('Active','Y')
                        AND  s.father_mobile  IS NOT NULL
                        AND  s.father_mobile  <> ''
                        AND  (@classId   IS NULL OR s.class_id   = @classId)
                        AND  (@sectionId IS NULL OR s.section_id = @sectionId)
                      ORDER BY c.class_name, sec.section_name, s.student_name",
                    new { schoolId, date, classId, sectionId });
        }

        /// <summary>Returns active students with fee outstanding > 0 for the given academic year.</summary>
        public IEnumerable<SmsRecipientDto> GetFeeDueRecipients(
            string tenantDbName, int schoolId, int academicYearId, int? classId, int? sectionId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<SmsRecipientDto>(
                    @"SELECT s.student_id           StudentId,
                             s.student_name         StudentName,
                             s.admission_no         AdmissionNo,
                             ISNULL(c.class_name,   '')  ClassName,
                             ISNULL(sec.section_name,'') SectionName,
                             s.father_mobile        FatherMobile,
                             ISNULL(fs_total.total_fee, 0)
                               - ISNULL(paid.total_paid, 0) OutstandingAmount
                      FROM   students s
                      LEFT JOIN classes  c   ON c.class_id    = s.class_id
                      LEFT JOIN sections sec ON sec.section_id = s.section_id
                      OUTER APPLY (
                          SELECT SUM(fs.amount) total_fee
                          FROM   fee_structures fs
                          WHERE  fs.class_id        = s.class_id
                            AND  fs.fee_category_id = s.fee_category_id
                            AND  fs.academic_year_id= @academicYearId
                            AND  fs.school_id       = s.school_id
                            AND  ISNULL(fs.status,'Active') <> 'Inactive'
                      ) fs_total
                      OUTER APPLY (
                          SELECT SUM(ri.net_amount) total_paid
                          FROM   fee_receipt_items ri
                          JOIN   fee_receipts fr ON fr.receipt_id = ri.receipt_id
                          WHERE  fr.student_id      = s.student_id
                            AND  fr.school_id       = s.school_id
                            AND  fr.academic_year_id= @academicYearId
                            AND  fr.status          = 'Active'
                            AND  ri.school_id       = s.school_id
                      ) paid
                      WHERE  s.school_id        = @schoolId
                        AND  s.academic_year_id = @academicYearId
                        AND  s.status           IN ('Active','Y')
                        AND  s.father_mobile    IS NOT NULL
                        AND  s.father_mobile    <> ''
                        AND  (ISNULL(fs_total.total_fee,0) - ISNULL(paid.total_paid,0)) > 0
                        AND  (@classId   IS NULL OR s.class_id   = @classId)
                        AND  (@sectionId IS NULL OR s.section_id = @sectionId)
                      ORDER BY c.class_name, sec.section_name, s.student_name",
                    new { schoolId, academicYearId, classId, sectionId });
        }

        /// <summary>Returns all active students for a class/section (Custom SMS).</summary>
        public IEnumerable<SmsRecipientDto> GetCustomRecipients(
            string tenantDbName, int schoolId, int academicYearId, int? classId, int? sectionId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<SmsRecipientDto>(
                    @"SELECT s.student_id          StudentId,
                             s.student_name        StudentName,
                             s.admission_no        AdmissionNo,
                             ISNULL(c.class_name,   '')  ClassName,
                             ISNULL(sec.section_name,'') SectionName,
                             s.father_mobile       FatherMobile
                      FROM   students s
                      LEFT JOIN classes  c   ON c.class_id    = s.class_id
                      LEFT JOIN sections sec ON sec.section_id = s.section_id
                      WHERE  s.school_id        = @schoolId
                        AND  s.academic_year_id = @academicYearId
                        AND  s.status           IN ('Active','Y')
                        AND  s.father_mobile    IS NOT NULL
                        AND  s.father_mobile    <> ''
                        AND  (@classId   IS NULL OR s.class_id   = @classId)
                        AND  (@sectionId IS NULL OR s.section_id = @sectionId)
                      ORDER BY c.class_name, sec.section_name, s.student_name",
                    new { schoolId, academicYearId, classId, sectionId });
        }

        // ── Logging ───────────────────────────────────────────────────────────

        public void LogSms(string tenantDbName, int schoolId, SmsLogEntry entry)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"INSERT INTO sms_logs
                        (school_id, sms_type, student_id, student_name, mobile,
                         message, status, error_message, sent_by, sent_at)
                      VALUES
                        (@schoolId, @SmsType, @StudentId, @StudentName, @Mobile,
                         @Message, @Status, @ErrorMessage, @SentBy, GETDATE())",
                    new
                    {
                        schoolId,
                        entry.SmsType, entry.StudentId, entry.StudentName, entry.Mobile,
                        entry.Message, entry.Status, entry.ErrorMessage, entry.SentBy,
                    });
        }

        // ── SMS gateway config (per school) ────────────────────────────────────

        /// <summary>Gateway account WITH api_key — for the send path only.</summary>
        public SmsAccount GetSmsAccount(string tenantDbName, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QueryFirstOrDefault<SmsAccount>(
                    @"SELECT provider Provider, api_url ApiUrl, username Username,
                             api_key ApiKey, sender_id SenderId, is_enabled IsEnabled
                      FROM   sms_configs WHERE school_id = @schoolId",
                    new { schoolId });
        }

        /// <summary>Gateway account for the settings page (api_key omitted) + templates.</summary>
        public SmsConfigDto GetSmsConfig(string tenantDbName, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var cfg = conn.QueryFirstOrDefault<SmsConfigDto>(
                    @"SELECT config_id ConfigId, provider Provider, api_url ApiUrl,
                             username Username, sender_id SenderId,
                             CASE WHEN LEN(ISNULL(api_key,'')) > 0 THEN 1 ELSE 0 END HasApiKey,
                             is_enabled IsEnabled,
                             CONVERT(VARCHAR(19), updated_at, 120) UpdatedAt
                      FROM   sms_configs WHERE school_id = @schoolId",
                    new { schoolId });

                if (cfg != null)
                    cfg.Templates = new List<SmsTemplateDto>(GetTemplates(tenantDbName, schoolId));
                return cfg;
            }
        }

        public void UpsertSmsConfig(string tenantDbName, int schoolId, UpdateSmsConfigRequest req, string updatedBy)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"IF EXISTS (SELECT 1 FROM sms_configs WHERE school_id = @schoolId)
                        UPDATE sms_configs
                        SET provider   = @Provider,
                            api_url    = @ApiUrl,
                            username   = @Username,
                            -- blank/null api_key keeps the existing one
                            api_key    = CASE WHEN ISNULL(@ApiKey,'') = '' THEN api_key ELSE @ApiKey END,
                            sender_id  = @SenderId,
                            is_enabled = @IsEnabled,
                            updated_at = GETDATE()
                        WHERE school_id = @schoolId
                      ELSE
                        INSERT INTO sms_configs (school_id, provider, api_url, username, api_key, sender_id, is_enabled, created_by)
                        VALUES (@schoolId, @Provider, @ApiUrl, @Username, ISNULL(@ApiKey,''), @SenderId, @IsEnabled, @updatedBy)",
                    new { schoolId, req.Provider, req.ApiUrl, req.Username, req.ApiKey, req.SenderId, req.IsEnabled, updatedBy });
        }

        // ── SMS templates (per school per key) ──────────────────────────────────

        public IEnumerable<SmsTemplateDto> GetTemplates(string tenantDbName, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<SmsTemplateDto>(
                    @"SELECT template_row_id TemplateRowId, template_key TemplateKey, title Title,
                             template_id TemplateId, message_text MessageText,
                             placeholders Placeholders, is_active IsActive
                      FROM   sms_templates WHERE school_id = @schoolId
                      ORDER BY template_key",
                    new { schoolId });
        }

        public SmsTemplateDto GetTemplate(string tenantDbName, int schoolId, string templateKey)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QueryFirstOrDefault<SmsTemplateDto>(
                    @"SELECT template_row_id TemplateRowId, template_key TemplateKey, title Title,
                             template_id TemplateId, message_text MessageText,
                             placeholders Placeholders, is_active IsActive
                      FROM   sms_templates WHERE school_id = @schoolId AND template_key = @templateKey",
                    new { schoolId, templateKey });
        }

        public void UpsertTemplate(string tenantDbName, int schoolId, SaveSmsTemplateRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"IF EXISTS (SELECT 1 FROM sms_templates WHERE school_id = @schoolId AND template_key = @TemplateKey)
                        UPDATE sms_templates
                        SET title = @Title, template_id = ISNULL(@TemplateId,''),
                            message_text = ISNULL(@MessageText,''), placeholders = @Placeholders,
                            is_active = @IsActive, updated_at = GETDATE()
                        WHERE school_id = @schoolId AND template_key = @TemplateKey
                      ELSE
                        INSERT INTO sms_templates (school_id, template_key, title, template_id, message_text, placeholders, is_active)
                        VALUES (@schoolId, @TemplateKey, @Title, ISNULL(@TemplateId,''), ISNULL(@MessageText,''), @Placeholders, @IsActive)",
                    new { schoolId, req.TemplateKey, req.Title, req.TemplateId, req.MessageText, req.Placeholders, req.IsActive });
        }

        public void DeleteTemplate(string tenantDbName, int schoolId, string templateKey)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    "DELETE FROM sms_templates WHERE school_id = @schoolId AND template_key = @templateKey",
                    new { schoolId, templateKey });
        }

        // ── History ───────────────────────────────────────────────────────────

        public IEnumerable<SmsLogDto> GetLogs(
            string tenantDbName, int schoolId,
            string smsType, DateTime? dateFrom, DateTime? dateTo)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<SmsLogDto>(
                    @"SELECT TOP 500
                             log_id                                          LogId,
                             sms_type                                       SmsType,
                             ISNULL(student_name,'')                        StudentName,
                             mobile                                         Mobile,
                             message                                        Message,
                             status                                         Status,
                             ISNULL(error_message,'')                       ErrorMessage,
                             sent_by                                        SentBy,
                             CONVERT(VARCHAR(19), sent_at, 120)             SentAt
                      FROM   sms_logs
                      WHERE  school_id = @schoolId
                        AND  (@smsType  IS NULL OR sms_type = @smsType)
                        AND  (@dateFrom IS NULL OR CAST(sent_at AS DATE) >= @dateFrom)
                        AND  (@dateTo   IS NULL OR CAST(sent_at AS DATE) <= @dateTo)
                      ORDER BY sent_at DESC",
                    new
                    {
                        schoolId,
                        smsType  = string.IsNullOrWhiteSpace(smsType) ? null : smsType,
                        dateFrom,
                        dateTo,
                    });
        }
    }
}
