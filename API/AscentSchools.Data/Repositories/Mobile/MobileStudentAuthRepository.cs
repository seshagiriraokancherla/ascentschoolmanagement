using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System;
using System.Collections.Generic;

namespace AscentSchools.Data.Repositories.Mobile
{
    public class MobileStudentAuthRepository
    {
        private readonly IConnectionFactory _db;
        public MobileStudentAuthRepository(IConnectionFactory db) { _db = db; }

        // ── Student lookup ────────────────────────────────────────────────

        /// <summary>
        /// Finds all active students whose father_mobile matches the given number in this school.
        /// Returns only the latest academic-year row per student (handles multi-year model).
        /// </summary>
        public IEnumerable<StudentParentLookupRecord> GetStudentsByParentMobile(string tenantDbName, string mobile, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<StudentParentLookupRecord>(
                    @"SELECT s.student_id StudentId, s.admission_no AdmissionNo,
                             s.student_name StudentName, c.class_name ClassName,
                             COALESCE(NULLIF(LTRIM(RTRIM(s.father_name)), ''), 'Parent') ParentName
                      FROM students s
                      LEFT JOIN classes c ON c.class_id = s.class_id
                      WHERE s.school_id = @schoolId
                        AND s.status IN ('Active', 'Y')
                        AND s.father_mobile = @mobile
                        AND NOT EXISTS (
                            SELECT 1 FROM students s2
                            WHERE s2.admission_no = s.admission_no
                              AND s2.school_id    = s.school_id
                              AND s2.status IN ('Active', 'Y')
                              AND s2.academic_year_id > s.academic_year_id
                        )",
                    new { mobile, schoolId });
        }

        public StudentMobileRecord GetStudentByAdmissionNo(string tenantDbName, string admissionNo, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                // ORDER BY academic_year_id DESC so the most recent year's row is always used.
                // This supports the multi-year model where a student gets a new row each year.
                return conn.QueryFirstOrDefault<StudentMobileRecord>(
                    @"SELECT TOP 1
                             s.student_id StudentId, s.admission_no AdmissionNo,
                             s.student_name StudentName, c.class_name ClassName,
                             s.section_id SectionId, sec.section_name SectionName,
                             s.academic_year_id AcademicYearId
                      FROM students s
                      LEFT JOIN classes  c   ON c.class_id    = s.class_id
                      LEFT JOIN sections sec ON sec.section_id = s.section_id
                      WHERE s.admission_no = @admissionNo AND s.school_id = @schoolId
                        AND s.status = 'Active'
                      ORDER BY s.academic_year_id DESC",
                    new { admissionNo, schoolId });
        }

        // After promotion the student gets a new student_id for the new year.
        // This updates the mobile account to point to the latest student_id.
        public void UpdateAccountStudentId(string tenantDbName, int accountId, long newStudentId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    "UPDATE student_mobile_accounts SET student_id = @newStudentId WHERE account_id = @accountId",
                    new { newStudentId, accountId });
        }

        // ── Mobile account ────────────────────────────────────────────────

        public StudentAccountRecord GetAccountById(string tenantDbName, int accountId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QueryFirstOrDefault<StudentAccountRecord>(
                    @"SELECT account_id AccountId, student_id StudentId, pin_hash PinHash,
                             mobile Mobile, email Email, is_active IsActive,
                             otp_code OtpCode, otp_expires_at OtpExpiresAt
                      FROM student_mobile_accounts
                      WHERE account_id = @accountId",
                    new { accountId });
        }

        // Fallback lookup used after promotion (account may still reference an older student_id)
        public StudentAccountRecord GetAccountByAdmissionNo(string tenantDbName, string admissionNo, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QueryFirstOrDefault<StudentAccountRecord>(
                    @"SELECT sma.account_id AccountId, sma.student_id StudentId, sma.pin_hash PinHash,
                             sma.mobile Mobile, sma.email Email, sma.is_active IsActive,
                             sma.otp_code OtpCode, sma.otp_expires_at OtpExpiresAt
                      FROM student_mobile_accounts sma
                      INNER JOIN students s ON s.student_id = sma.student_id
                      WHERE s.admission_no = @admissionNo AND s.school_id = @schoolId",
                    new { admissionNo, schoolId });
        }

        public StudentAccountRecord GetAccountByStudentId(string tenantDbName, long studentId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QueryFirstOrDefault<StudentAccountRecord>(
                    @"SELECT account_id AccountId, student_id StudentId, pin_hash PinHash,
                             mobile Mobile, email Email, is_active IsActive,
                             otp_code OtpCode, otp_expires_at OtpExpiresAt
                      FROM student_mobile_accounts
                      WHERE student_id = @studentId",
                    new { studentId });
        }

        public int CreateAccount(string tenantDbName, long studentId, string pinHash, int schoolId,
                                  string mobile = null, string email = null)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QuerySingle<int>(
                    @"INSERT INTO student_mobile_accounts
                        (student_id, pin_hash, mobile, email, school_id, is_active)
                      VALUES (@studentId, @pinHash, @mobile, @email, @schoolId, 1);
                      SELECT SCOPE_IDENTITY();",
                    new { studentId, pinHash, schoolId, mobile, email });
        }

        public void UpdateLastLogin(string tenantDbName, int accountId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    "UPDATE student_mobile_accounts SET last_login_at = GETDATE() WHERE account_id = @accountId",
                    new { accountId });
        }

        public void SetOtp(string tenantDbName, long studentId, string otpCode, DateTime expiresAt)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    "UPDATE student_mobile_accounts SET otp_code = @otpCode, otp_expires_at = @expiresAt WHERE student_id = @studentId",
                    new { studentId, otpCode, expiresAt });
        }

        public void UpdatePin(string tenantDbName, long studentId, string pinHash)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    "UPDATE student_mobile_accounts SET pin_hash = @pinHash, otp_code = NULL, otp_expires_at = NULL WHERE student_id = @studentId",
                    new { studentId, pinHash });
        }

        // ── Refresh tokens ────────────────────────────────────────────────

        public void CreateRefreshToken(string tenantDbName, int accountId, string tokenHash, DateTime expiresAt)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    "INSERT INTO student_refresh_tokens (account_id, token_hash, expires_at) VALUES (@accountId, @tokenHash, @expiresAt)",
                    new { accountId, tokenHash, expiresAt });
        }

        public StudentRefreshTokenRecord GetRefreshToken(string tenantDbName, string tokenHash)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QueryFirstOrDefault<StudentRefreshTokenRecord>(
                    @"SELECT token_id TokenId, account_id AccountId, expires_at ExpiresAt, revoked_at RevokedAt
                      FROM student_refresh_tokens WHERE token_hash = @tokenHash",
                    new { tokenHash });
        }

        public void RevokeRefreshToken(string tenantDbName, int tokenId, string replacedByHash = null)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    "UPDATE student_refresh_tokens SET revoked_at = GETDATE(), replaced_by = @replacedByHash WHERE token_id = @tokenId",
                    new { tokenId, replacedByHash });
        }

        public void RevokeAllRefreshTokens(string tenantDbName, int accountId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    "UPDATE student_refresh_tokens SET revoked_at = GETDATE() WHERE account_id = @accountId AND revoked_at IS NULL",
                    new { accountId });
        }
    }

    public class StudentMobileRecord
    {
        public long   StudentId      { get; set; }
        public string AdmissionNo    { get; set; }
        public string StudentName    { get; set; }
        public string ClassName      { get; set; }
        public int?   SectionId      { get; set; }
        public string SectionName    { get; set; }
        public int?   AcademicYearId { get; set; }
    }

    public class StudentAccountRecord
    {
        public int       AccountId    { get; set; }
        public long      StudentId    { get; set; }
        public string    PinHash      { get; set; }
        public string    Mobile       { get; set; }
        public string    Email        { get; set; }
        public bool      IsActive     { get; set; }
        public string    OtpCode      { get; set; }
        public DateTime? OtpExpiresAt { get; set; }
    }

    public class StudentRefreshTokenRecord
    {
        public int       TokenId   { get; set; }
        public int       AccountId { get; set; }
        public DateTime  ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
    }

    /// <summary>Student row returned when looking up by parent mobile number.</summary>
    public class StudentParentLookupRecord
    {
        public long   StudentId   { get; set; }
        public string AdmissionNo { get; set; }
        public string StudentName { get; set; }
        public string ClassName   { get; set; }
        public string ParentName  { get; set; }   // father_name fallback to "Parent"
    }
}
