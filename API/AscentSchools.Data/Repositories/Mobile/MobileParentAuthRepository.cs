using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System;
using System.Collections.Generic;

namespace AscentSchools.Data.Repositories.Mobile
{
    public class MobileParentAuthRepository
    {
        private readonly IConnectionFactory _db;
        public MobileParentAuthRepository(IConnectionFactory db) { _db = db; }

        // ── Parent accounts (master DB) ───────────────────────────────────

        public ParentAccountRecord GetByMobile(string mobile)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.QueryFirstOrDefault<ParentAccountRecord>(
                    @"SELECT parent_id ParentId, full_name FullName, mobile Mobile, email Email,
                             pin_hash PinHash, is_active IsActive,
                             otp_code OtpCode, otp_expires_at OtpExpiresAt,
                             device_id DeviceId
                      FROM parent_accounts WHERE mobile = @mobile",
                    new { mobile });
        }

        public ParentAccountRecord GetByEmail(string email)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.QueryFirstOrDefault<ParentAccountRecord>(
                    @"SELECT parent_id ParentId, full_name FullName, mobile Mobile, email Email,
                             pin_hash PinHash, is_active IsActive,
                             otp_code OtpCode, otp_expires_at OtpExpiresAt
                      FROM parent_accounts WHERE email = @email",
                    new { email });
        }

        public ParentAccountRecord GetById(int parentId)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.QueryFirstOrDefault<ParentAccountRecord>(
                    @"SELECT parent_id ParentId, full_name FullName, mobile Mobile, email Email,
                             pin_hash PinHash, is_active IsActive
                      FROM parent_accounts WHERE parent_id = @parentId",
                    new { parentId });
        }

        public bool MobileExists(string mobile)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.QuerySingle<int>(
                    "SELECT COUNT(1) FROM parent_accounts WHERE mobile = @mobile",
                    new { mobile }) > 0;
        }

        public bool EmailExists(string email)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.QuerySingle<int>(
                    "SELECT COUNT(1) FROM parent_accounts WHERE email = @email",
                    new { email }) > 0;
        }

        /// <summary>
        /// Returns the parent_id for an existing account with this mobile, or creates one if it doesn't exist.
        /// Used during OTP verify for first-time logins — account is OTP-only (no PIN).
        /// </summary>
        public int GetOrCreateParentByMobile(string mobile, string fullName)
        {
            using (var conn = _db.GetMasterConnection())
            {
                var existing = conn.QueryFirstOrDefault<int?>(
                    "SELECT parent_id FROM parent_accounts WHERE mobile = @mobile",
                    new { mobile });

                if (existing.HasValue) return existing.Value;

                return conn.QuerySingle<int>(
                    @"INSERT INTO parent_accounts (full_name, mobile, pin_hash, is_active)
                      VALUES (@fullName, @mobile, '', 1);
                      SELECT CAST(SCOPE_IDENTITY() AS INT)",
                    new { fullName, mobile });
            }
        }

        public int CreateParent(string fullName, string pinHash, string mobile = null, string email = null)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.QuerySingle<int>(
                    @"INSERT INTO parent_accounts (full_name, pin_hash, mobile, email, is_active)
                      VALUES (@fullName, @pinHash, @mobile, @email, 1);
                      SELECT SCOPE_IDENTITY();",
                    new { fullName, pinHash, mobile, email });
        }

        public void UpdateLastLogin(int parentId)
        {
            using (var conn = _db.GetMasterConnection())
                conn.Execute(
                    "UPDATE parent_accounts SET last_login_at = GETDATE() WHERE parent_id = @parentId",
                    new { parentId });
        }

        public void SetOtp(int parentId, string otpCode, DateTime expiresAt)
        {
            using (var conn = _db.GetMasterConnection())
                conn.Execute(
                    "UPDATE parent_accounts SET otp_code = @otpCode, otp_expires_at = @expiresAt WHERE parent_id = @parentId",
                    new { parentId, otpCode, expiresAt });
        }

        public void UpdatePin(int parentId, string pinHash)
        {
            using (var conn = _db.GetMasterConnection())
                conn.Execute(
                    "UPDATE parent_accounts SET pin_hash = @pinHash, otp_code = NULL, otp_expires_at = NULL WHERE parent_id = @parentId",
                    new { parentId, pinHash });
        }

        // ── SMS OTP (parent_otp table) ────────────────────────────────────────

        public void CreateOtp(string mobile, string otpHash, string deviceId, DateTime expiresAt)
        {
            using (var conn = _db.GetMasterConnection())
                conn.Execute(
                    @"INSERT INTO parent_otp (mobile, otp_hash, device_id, expires_at)
                      VALUES (@mobile, @otpHash, @deviceId, @expiresAt)",
                    new { mobile, otpHash, deviceId, expiresAt });
        }

        public ParentOtpRecord GetValidOtp(string mobile)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.QueryFirstOrDefault<ParentOtpRecord>(
                    @"SELECT TOP 1 otp_id OtpId, otp_hash OtpHash, device_id DeviceId
                      FROM parent_otp
                      WHERE mobile = @mobile AND is_used = 0 AND expires_at > GETDATE()
                      ORDER BY otp_id DESC",
                    new { mobile });
        }

        public void MarkOtpUsed(int otpId)
        {
            using (var conn = _db.GetMasterConnection())
                conn.Execute(
                    "UPDATE parent_otp SET is_used = 1 WHERE otp_id = @otpId",
                    new { otpId });
        }

        /// <summary>Returns how many OTP requests were made for this mobile in the last N minutes.</summary>
        public int GetRecentOtpCount(string mobile, int withinMinutes)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.QuerySingle<int>(
                    @"SELECT COUNT(1) FROM parent_otp
                      WHERE mobile = @mobile
                        AND created_at > DATEADD(MINUTE, @neg, GETDATE())",
                    new { mobile, neg = -withinMinutes });
        }

        // ── Device binding ────────────────────────────────────────────────────

        public void BindDevice(int parentId, string deviceId)
        {
            using (var conn = _db.GetMasterConnection())
                conn.Execute(
                    "UPDATE parent_accounts SET device_id = @deviceId, device_bound_at = GETDATE() WHERE parent_id = @parentId",
                    new { parentId, deviceId });
        }

        // ── Parent children links (master DB) ─────────────────────────────

        /// <summary>Returns all children for a parent scoped to a specific school group (one app = one school).</summary>
        public IEnumerable<ParentChildRecord> GetChildren(int parentId, int groupId)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.Query<ParentChildRecord>(
                    @"SELECT pc.link_id LinkId, pc.parent_id ParentId, pc.student_id StudentId,
                             pc.group_id GroupId, pc.db_name DbName, pc.school_id SchoolId,
                             pc.student_name StudentName, pc.class_name ClassName,
                             pc.admission_no AdmissionNo,
                             sg.subdomain SchoolCode,
                             pc.is_active IsActive
                      FROM parent_children pc
                      JOIN school_groups sg ON sg.group_id = pc.group_id
                      WHERE pc.parent_id = @parentId
                        AND pc.group_id  = @groupId
                      ORDER BY pc.linked_at",
                    new { parentId, groupId });
        }

        public ParentChildRecord GetChildLink(int linkId, int parentId)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.QueryFirstOrDefault<ParentChildRecord>(
                    @"SELECT pc.link_id LinkId, pc.parent_id ParentId, pc.student_id StudentId,
                             pc.group_id GroupId, pc.db_name DbName, pc.school_id SchoolId,
                             pc.student_name StudentName, pc.class_name ClassName,
                             pc.admission_no AdmissionNo,
                             sg.subdomain SchoolCode,
                             pc.is_active IsActive
                      FROM parent_children pc
                      JOIN school_groups sg ON sg.group_id = pc.group_id
                      WHERE pc.link_id = @linkId AND pc.parent_id = @parentId",
                    new { linkId, parentId });
        }

        public bool ChildLinkExists(int parentId, long studentId, int groupId)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.QuerySingle<int>(
                    "SELECT COUNT(1) FROM parent_children WHERE parent_id = @parentId AND student_id = @studentId AND group_id = @groupId",
                    new { parentId, studentId, groupId }) > 0;
        }

        /// <summary>Finds an existing link by admission_no — works across promotions where student_id changes.</summary>
        public ParentChildRecord GetChildLinkByAdmissionNo(int parentId, string admissionNo, int groupId)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.QueryFirstOrDefault<ParentChildRecord>(
                    @"SELECT pc.link_id LinkId, pc.parent_id ParentId, pc.student_id StudentId,
                             pc.group_id GroupId, pc.db_name DbName, pc.school_id SchoolId,
                             pc.student_name StudentName, pc.class_name ClassName,
                             pc.admission_no AdmissionNo, sg.subdomain SchoolCode, pc.is_active IsActive
                      FROM parent_children pc
                      JOIN school_groups sg ON sg.group_id = pc.group_id
                      WHERE pc.parent_id = @parentId
                        AND pc.admission_no = @admissionNo
                        AND pc.group_id = @groupId",
                    new { parentId, admissionNo, groupId });
        }

        /// <summary>Refreshes student_id and display fields on an existing child link (called after promotion or first class assignment).</summary>
        public void UpdateChildLink(int linkId, long studentId, string studentName, string className)
        {
            using (var conn = _db.GetMasterConnection())
                conn.Execute(
                    "UPDATE parent_children SET student_id = @studentId, student_name = @studentName, class_name = @className WHERE link_id = @linkId",
                    new { linkId, studentId, studentName, className });
        }

        public void CreateChildLink(int parentId, long studentId, int groupId, string dbName,
                                     int schoolId, string studentName, string className, string admissionNo)
        {
            using (var conn = _db.GetMasterConnection())
                conn.Execute(
                    @"INSERT INTO parent_children
                        (parent_id, student_id, group_id, db_name, school_id,
                         student_name, class_name, admission_no, is_active)
                      VALUES (@parentId, @studentId, @groupId, @dbName, @schoolId,
                              @studentName, @className, @admissionNo, 1)",
                    new { parentId, studentId, groupId, dbName, schoolId, studentName, className, admissionNo });
        }

        // ── Refresh tokens (master DB) ─────────────────────────────────────

        public void CreateRefreshToken(int parentId, string tokenHash, DateTime expiresAt)
        {
            using (var conn = _db.GetMasterConnection())
                conn.Execute(
                    "INSERT INTO parent_refresh_tokens (parent_id, token_hash, expires_at) VALUES (@parentId, @tokenHash, @expiresAt)",
                    new { parentId, tokenHash, expiresAt });
        }

        public ParentRefreshTokenRecord GetRefreshToken(string tokenHash)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.QueryFirstOrDefault<ParentRefreshTokenRecord>(
                    @"SELECT token_id TokenId, parent_id ParentId, expires_at ExpiresAt, revoked_at RevokedAt
                      FROM parent_refresh_tokens WHERE token_hash = @tokenHash",
                    new { tokenHash });
        }

        public void RevokeRefreshToken(int tokenId, string replacedByHash = null)
        {
            using (var conn = _db.GetMasterConnection())
                conn.Execute(
                    "UPDATE parent_refresh_tokens SET revoked_at = GETDATE(), replaced_by = @replacedByHash WHERE token_id = @tokenId",
                    new { tokenId, replacedByHash });
        }

        /// <summary>Slides the expiry forward without rotating (stable mobile refresh token).</summary>
        public void ExtendRefreshToken(int tokenId, DateTime expiresAt)
        {
            using (var conn = _db.GetMasterConnection())
                conn.Execute(
                    "UPDATE parent_refresh_tokens SET expires_at = @expiresAt WHERE token_id = @tokenId AND revoked_at IS NULL",
                    new { tokenId, expiresAt });
        }

        public void RevokeAllRefreshTokens(int parentId)
        {
            using (var conn = _db.GetMasterConnection())
                conn.Execute(
                    "UPDATE parent_refresh_tokens SET revoked_at = GETDATE() WHERE parent_id = @parentId AND revoked_at IS NULL",
                    new { parentId });
        }
    }

    public class ParentAccountRecord
    {
        public int       ParentId     { get; set; }
        public string    FullName     { get; set; }
        public string    Mobile       { get; set; }
        public string    Email        { get; set; }
        public string    PinHash      { get; set; }
        public bool      IsActive     { get; set; }
        public string    OtpCode      { get; set; }
        public DateTime? OtpExpiresAt { get; set; }
        public string    DeviceId     { get; set; }
    }

    public class ParentOtpRecord
    {
        public int    OtpId    { get; set; }
        public string OtpHash  { get; set; }
        public string DeviceId { get; set; }
    }

    public class ParentChildRecord
    {
        public int    LinkId      { get; set; }
        public int    ParentId    { get; set; }
        public long   StudentId   { get; set; }
        public int    GroupId     { get; set; }
        public string DbName      { get; set; }
        public int    SchoolId    { get; set; }
        public string StudentName { get; set; }
        public string ClassName   { get; set; }
        public string AdmissionNo { get; set; }
        public string SchoolCode  { get; set; }
        public bool   IsActive    { get; set; }
    }

    public class ParentRefreshTokenRecord
    {
        public int       TokenId   { get; set; }
        public int       ParentId  { get; set; }
        public DateTime  ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
    }
}
