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
