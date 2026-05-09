using AscentSchools.API.Filters;
using AscentSchools.API.Helpers;
using AscentSchools.API.Middleware;
using AscentSchools.Core.DTOs.Mobile.Auth;
using AscentSchools.Core.Models;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.Control;
using AscentSchools.Data.Repositories.Mobile;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;

namespace AscentSchools.API.Controllers.Mobile
{
    [RoutePrefix("mobile/auth")]
    public class MobileAuthController : ApiController
    {
        // Google Play review bypass — fixed credentials so reviewer can test without a real SIM
        private const string ReviewMobile = "9999999999";
        private const string ReviewOtp    = "123456";

        private readonly SchoolGroupRepository    _groups;
        private readonly MobileStudentAuthRepository _studentAuth;
        private readonly MobileParentAuthRepository  _parentAuth;

        public MobileAuthController()
        {
            var db       = new TenantConnectionFactory();
            _groups      = new SchoolGroupRepository(db);
            _studentAuth = new MobileStudentAuthRepository(db);
            _parentAuth  = new MobileParentAuthRepository(db);
        }

        // ================================================================
        // STUDENT AUTH
        // ================================================================

        // POST mobile/auth/student/register
        [HttpPost, Route("student/register"), AllowAnonymous]
        public HttpResponseMessage StudentRegister([FromBody] StudentRegisterRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.AdmissionNo) || string.IsNullOrWhiteSpace(request.Pin))
                return Fail(HttpStatusCode.BadRequest, "AdmissionNo and PIN are required.");

            if (request.Pin.Length < 4 || request.Pin.Length > 6 || !request.Pin.All(char.IsDigit))
                return Fail(HttpStatusCode.BadRequest, "PIN must be 4–6 digits.");

            var tenant = ResolveTenant();
            if (tenant == null) return Fail(HttpStatusCode.Unauthorized, "School not found. Check X-Subdomain header.");

            // Resolve school_id from tenant schools (first active school in group)
            var schoolId = GetSchoolId(tenant.DbName);
            if (schoolId == 0) return Fail(HttpStatusCode.ServiceUnavailable, "No active school branch found.");

            var student = _studentAuth.GetStudentByAdmissionNo(tenant.DbName, request.AdmissionNo.Trim(), schoolId);
            if (student == null)
                return Fail(HttpStatusCode.NotFound, "Student not found. Check your admission number.");

            if (_studentAuth.GetAccountByStudentId(tenant.DbName, student.StudentId) != null)
                return Fail(HttpStatusCode.Conflict, "This student is already registered. Please login.");

            var pinHash = JwtHelper.HashRefreshToken(request.Pin);
            var accountId = _studentAuth.CreateAccount(tenant.DbName, student.StudentId, pinHash, schoolId,
                                                        request.Mobile, request.Email);

            return BuildStudentAuthResponse(tenant, accountId, student, schoolId, isLogin: false);
        }

        // POST mobile/auth/student/login
        [HttpPost, Route("student/login"), AllowAnonymous]
        public HttpResponseMessage StudentLogin([FromBody] StudentLoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.AdmissionNo) || string.IsNullOrWhiteSpace(request.Pin))
                return Fail(HttpStatusCode.BadRequest, "AdmissionNo and PIN are required.");

            var tenant = ResolveTenant();
            if (tenant == null) return Fail(HttpStatusCode.Unauthorized, "School not found. Check X-Subdomain header.");

            var schoolId = GetSchoolId(tenant.DbName);
            if (schoolId == 0) return Fail(HttpStatusCode.ServiceUnavailable, "No active school branch found.");

            var student = _studentAuth.GetStudentByAdmissionNo(tenant.DbName, request.AdmissionNo.Trim(), schoolId);
            if (student == null)
                return Fail(HttpStatusCode.Unauthorized, "Invalid admission number or PIN.");

            // Look up account — may be registered under an older student_id (pre-promotion)
            var account = _studentAuth.GetAccountByStudentId(tenant.DbName, student.StudentId)
                       ?? _studentAuth.GetAccountByAdmissionNo(tenant.DbName, request.AdmissionNo.Trim(), schoolId);
            if (account == null)
                return Fail(HttpStatusCode.Unauthorized, "Not registered. Please register first.");

            // Re-point account to current year's student_id if it drifted after promotion
            if (account.StudentId != student.StudentId)
                _studentAuth.UpdateAccountStudentId(tenant.DbName, account.AccountId, student.StudentId);

            if (!account.IsActive)
                return Fail(HttpStatusCode.Unauthorized, "Account is deactivated.");

            if (account.PinHash != JwtHelper.HashRefreshToken(request.Pin))
                return Fail(HttpStatusCode.Unauthorized, "Invalid admission number or PIN.");

            return BuildStudentAuthResponse(tenant, account.AccountId, student, schoolId, isLogin: true);
        }

        // POST mobile/auth/student/refresh
        [HttpPost, Route("student/refresh"), AllowAnonymous]
        public HttpResponseMessage StudentRefresh()
        {
            var rawToken = GetRefreshCookie("studentRefreshToken");
            if (string.IsNullOrWhiteSpace(rawToken))
                return Fail(HttpStatusCode.Unauthorized, "No refresh token.");

            var tenant = ResolveTenant();
            if (tenant == null || tenant.GroupId == 0)
                return Fail(HttpStatusCode.Unauthorized, "School not found.");

            var hash   = JwtHelper.HashRefreshToken(rawToken);
            var record = _studentAuth.GetRefreshToken(tenant.DbName, hash);

            if (record == null || record.RevokedAt.HasValue || record.ExpiresAt < DateTime.UtcNow)
                return Fail(HttpStatusCode.Unauthorized, "Refresh token expired or revoked.");

            var account = _studentAuth.GetAccountById(tenant.DbName, record.AccountId);
            if (account == null || !account.IsActive)
                return Fail(HttpStatusCode.Unauthorized, "Account inactive.");

            var schoolId = GetSchoolId(tenant.DbName);
            var student  = GetStudentRecord(tenant.DbName, account.StudentId, schoolId);

            // Rotate
            var newRaw  = JwtHelper.GenerateRefreshToken();
            var newHash = JwtHelper.HashRefreshToken(newRaw);
            _studentAuth.RevokeRefreshToken(tenant.DbName, record.TokenId, newHash);
            _studentAuth.CreateRefreshToken(tenant.DbName, record.AccountId, newHash, JwtHelper.RefreshTokenExpiry());

            return BuildStudentAuthResponse(tenant, record.AccountId, student, schoolId, isLogin: false, rawRefresh: newRaw);
        }

        // POST mobile/auth/student/logout
        [HttpPost, Route("student/logout"), AllowAnonymous]
        public HttpResponseMessage StudentLogout()
        {
            var rawToken = GetRefreshCookie("studentRefreshToken");
            if (!string.IsNullOrWhiteSpace(rawToken))
            {
                var tenant = ResolveTenant();
                if (tenant?.GroupId > 0 && !string.IsNullOrWhiteSpace(tenant.DbName))
                {
                    var record = _studentAuth.GetRefreshToken(tenant.DbName, JwtHelper.HashRefreshToken(rawToken));
                    if (record != null) _studentAuth.RevokeRefreshToken(tenant.DbName, record.TokenId);
                }
            }

            var response = Request.CreateResponse(HttpStatusCode.OK, ApiResponse.Ok("Logged out."));
            ClearCookie(response, "studentRefreshToken", "/mobile/auth/student");
            return response;
        }

        // POST mobile/auth/student/request-otp
        [HttpPost, Route("student/request-otp"), AllowAnonymous]
        public HttpResponseMessage StudentRequestOtp([FromBody] OtpRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Identifier))
                return Fail(HttpStatusCode.BadRequest, "Identifier (admission number) is required.");

            var tenant = ResolveTenant();
            if (tenant == null || tenant.GroupId == 0) return Fail(HttpStatusCode.Unauthorized, "School not found.");

            var schoolId = GetSchoolId(tenant.DbName);
            var student  = _studentAuth.GetStudentByAdmissionNo(tenant.DbName, request.Identifier.Trim(), schoolId);
            if (student == null) return Fail(HttpStatusCode.NotFound, "Student not found.");

            var account = _studentAuth.GetAccountByStudentId(tenant.DbName, student.StudentId);
            if (account == null) return Fail(HttpStatusCode.NotFound, "Account not registered.");

            // Generate 6-digit OTP (in production, send via SMS/email)
            var otp     = new Random().Next(100000, 999999).ToString();
            var expires = DateTime.UtcNow.AddMinutes(10);
            _studentAuth.SetOtp(tenant.DbName, student.StudentId, otp, expires);

            // TODO: Integrate SMS/email delivery. For now return OTP in response (dev only).
            return Request.CreateResponse(HttpStatusCode.OK, ApiResponse<object>.Ok(new { otp }, "OTP generated. Send to student's registered contact."));
        }

        // POST mobile/auth/student/reset-pin
        [HttpPost, Route("student/reset-pin"), AllowAnonymous]
        public HttpResponseMessage StudentResetPin([FromBody] ResetPinRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Identifier)
                || string.IsNullOrWhiteSpace(request.OtpCode) || string.IsNullOrWhiteSpace(request.NewPin))
                return Fail(HttpStatusCode.BadRequest, "Identifier, OTP and NewPin are required.");

            if (request.NewPin.Length < 4 || request.NewPin.Length > 6 || !request.NewPin.All(char.IsDigit))
                return Fail(HttpStatusCode.BadRequest, "PIN must be 4–6 digits.");

            var tenant = ResolveTenant();
            if (tenant == null || tenant.GroupId == 0) return Fail(HttpStatusCode.Unauthorized, "School not found.");

            var schoolId = GetSchoolId(tenant.DbName);
            var student  = _studentAuth.GetStudentByAdmissionNo(tenant.DbName, request.Identifier.Trim(), schoolId);
            if (student == null) return Fail(HttpStatusCode.NotFound, "Student not found.");

            var account = _studentAuth.GetAccountByStudentId(tenant.DbName, student.StudentId);
            if (account == null) return Fail(HttpStatusCode.NotFound, "Account not registered.");

            if (account.OtpCode != request.OtpCode || account.OtpExpiresAt < DateTime.UtcNow)
                return Fail(HttpStatusCode.BadRequest, "Invalid or expired OTP.");

            _studentAuth.UpdatePin(tenant.DbName, student.StudentId, JwtHelper.HashRefreshToken(request.NewPin));
            _studentAuth.RevokeAllRefreshTokens(tenant.DbName, account.AccountId);

            return Request.CreateResponse(HttpStatusCode.OK, ApiResponse.Ok("PIN reset successfully. Please login."));
        }

        // ================================================================
        // PARENT AUTH
        // ================================================================

        // POST mobile/auth/parent/register
        [HttpPost, Route("parent/register"), AllowAnonymous]
        public HttpResponseMessage ParentRegister([FromBody] ParentRegisterRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Pin))
                return Fail(HttpStatusCode.BadRequest, "FullName and PIN are required.");

            if (string.IsNullOrWhiteSpace(request.Mobile) && string.IsNullOrWhiteSpace(request.Email))
                return Fail(HttpStatusCode.BadRequest, "Either Mobile or Email is required.");

            if (request.Pin.Length < 4 || request.Pin.Length > 6 || !request.Pin.All(char.IsDigit))
                return Fail(HttpStatusCode.BadRequest, "PIN must be 4–6 digits.");

            if (!string.IsNullOrWhiteSpace(request.Mobile) && _parentAuth.MobileExists(request.Mobile))
                return Fail(HttpStatusCode.Conflict, "This mobile number is already registered.");

            if (!string.IsNullOrWhiteSpace(request.Email) && _parentAuth.EmailExists(request.Email))
                return Fail(HttpStatusCode.Conflict, "This email address is already registered.");

            var pinHash  = JwtHelper.HashRefreshToken(request.Pin);
            var parentId = _parentAuth.CreateParent(request.FullName, pinHash, request.Mobile, request.Email);

            return BuildParentAuthResponse(parentId, request.FullName, isLogin: false);
        }

        // POST mobile/auth/parent/login
        [HttpPost, Route("parent/login"), AllowAnonymous]
        public HttpResponseMessage ParentLogin([FromBody] ParentLoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Identifier) || string.IsNullOrWhiteSpace(request.Pin))
                return Fail(HttpStatusCode.BadRequest, "Identifier and PIN are required.");

            ParentAccountRecord parent;
            if (request.Identifier.Contains("@"))
                parent = _parentAuth.GetByEmail(request.Identifier.Trim());
            else
                parent = _parentAuth.GetByMobile(request.Identifier.Trim());

            if (parent == null || !parent.IsActive)
                return Fail(HttpStatusCode.Unauthorized, "Invalid identifier or PIN.");

            if (parent.PinHash != JwtHelper.HashRefreshToken(request.Pin))
                return Fail(HttpStatusCode.Unauthorized, "Invalid identifier or PIN.");

            _parentAuth.UpdateLastLogin(parent.ParentId);
            return BuildParentAuthResponse(parent.ParentId, parent.FullName, isLogin: true);
        }

        // POST mobile/auth/parent/refresh
        [HttpPost, Route("parent/refresh"), AllowAnonymous]
        public HttpResponseMessage ParentRefresh()
        {
            var rawToken = GetRefreshCookie("parentRefreshToken");
            if (string.IsNullOrWhiteSpace(rawToken))
                return Fail(HttpStatusCode.Unauthorized, "No refresh token.");

            var hash   = JwtHelper.HashRefreshToken(rawToken);
            var record = _parentAuth.GetRefreshToken(hash);

            if (record == null || record.RevokedAt.HasValue || record.ExpiresAt < DateTime.UtcNow)
                return Fail(HttpStatusCode.Unauthorized, "Refresh token expired or revoked.");

            var parent = _parentAuth.GetById(record.ParentId);
            if (parent == null || !parent.IsActive)
                return Fail(HttpStatusCode.Unauthorized, "Account inactive.");

            var newRaw  = JwtHelper.GenerateRefreshToken();
            var newHash = JwtHelper.HashRefreshToken(newRaw);
            _parentAuth.RevokeRefreshToken(record.TokenId, newHash);
            _parentAuth.CreateRefreshToken(record.ParentId, newHash, JwtHelper.RefreshTokenExpiry());

            return BuildParentAuthResponse(record.ParentId, parent.FullName, isLogin: false, rawRefresh: newRaw);
        }

        // POST mobile/auth/parent/logout
        [HttpPost, Route("parent/logout"), AllowAnonymous]
        public HttpResponseMessage ParentLogout()
        {
            var rawToken = GetRefreshCookie("parentRefreshToken");
            if (!string.IsNullOrWhiteSpace(rawToken))
            {
                var record = _parentAuth.GetRefreshToken(JwtHelper.HashRefreshToken(rawToken));
                if (record != null) _parentAuth.RevokeRefreshToken(record.TokenId);
            }

            var response = Request.CreateResponse(HttpStatusCode.OK, ApiResponse.Ok("Logged out."));
            ClearCookie(response, "parentRefreshToken", "/mobile/auth/parent");
            return response;
        }

        // POST mobile/auth/parent/request-otp
        [HttpPost, Route("parent/request-otp"), AllowAnonymous]
        public HttpResponseMessage ParentRequestOtp([FromBody] ParentOtpRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Mobile) || string.IsNullOrWhiteSpace(request.DeviceId))
                return Fail(HttpStatusCode.BadRequest, "Mobile and DeviceId are required.");

            // Always return the same generic message — never reveal whether a number is registered
            const string genericMsg = "If your number is registered, you will receive an OTP.";

            var mobile = request.Mobile.Trim();

            // Google Play review bypass — skip student check and SMS for the fixed review number
            if (mobile == ReviewMobile)
                return Request.CreateResponse(HttpStatusCode.OK, ApiResponse.Ok(genericMsg));

            // Resolve tenant from X-School-Code so we know which school's students to check
            var tenant = ResolveTenant();
            if (tenant == null || string.IsNullOrWhiteSpace(tenant.DbName))
                return Request.CreateResponse(HttpStatusCode.OK, ApiResponse.Ok(genericMsg));

            var schoolId = GetSchoolId(tenant.DbName);
            if (schoolId == 0)
                return Request.CreateResponse(HttpStatusCode.OK, ApiResponse.Ok(genericMsg));

            // Gate: only send OTP if at least one active student in this school has this mobile
            var students = _studentAuth.GetStudentsByParentMobile(tenant.DbName, mobile, schoolId);
            if (!students.Any())
                return Request.CreateResponse(HttpStatusCode.OK, ApiResponse.Ok(genericMsg));

            // Rate limit: max 5 requests per mobile per 10 minutes
            if (_parentAuth.GetRecentOtpCount(mobile, 10) >= 5)
                return Request.CreateResponse(HttpStatusCode.OK, ApiResponse.Ok(genericMsg));

            var otp       = new Random().Next(100000, 999999).ToString("D6");
            var otpHash   = JwtHelper.HashRefreshToken(otp);
            var expiresAt = DateTime.UtcNow.AddMinutes(5);

            // Use first student's father_name as display name for the SMS greeting
            var displayName = students.First().ParentName ?? "Parent";

            _parentAuth.CreateOtp(mobile, otpHash, request.DeviceId.Trim(), expiresAt);
            SmsHelper.SendOtp(mobile, displayName, otp);

            return Request.CreateResponse(HttpStatusCode.OK, ApiResponse.Ok(genericMsg));
        }

        // POST mobile/auth/parent/verify-otp
        [HttpPost, Route("parent/verify-otp"), AllowAnonymous]
        public HttpResponseMessage ParentVerifyOtp([FromBody] ParentVerifyOtpRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Mobile)
                || string.IsNullOrWhiteSpace(request.Otp) || string.IsNullOrWhiteSpace(request.DeviceId))
                return Fail(HttpStatusCode.BadRequest, "Mobile, OTP and DeviceId are required.");

            var mobile = request.Mobile.Trim();

            // Google Play review bypass — accept fixed OTP without a DB lookup
            bool isReviewBypass = mobile == ReviewMobile &&
                                  string.Equals(request.Otp.Trim(), ReviewOtp, StringComparison.Ordinal);

            if (!isReviewBypass)
            {
                var otpRecord = _parentAuth.GetValidOtp(mobile);
                if (otpRecord == null || otpRecord.OtpHash != JwtHelper.HashRefreshToken(request.Otp.Trim()))
                    return Fail(HttpStatusCode.Unauthorized, "Invalid or expired OTP.");

                _parentAuth.MarkOtpUsed(otpRecord.OtpId);
            }

            // Resolve tenant and find students for this mobile in this school
            var tenant = ResolveTenant();
            if (tenant == null || string.IsNullOrWhiteSpace(tenant.DbName))
                return Fail(HttpStatusCode.Unauthorized, "School not found.");

            var schoolId = GetSchoolId(tenant.DbName);
            var students = schoolId > 0
                ? _studentAuth.GetStudentsByParentMobile(tenant.DbName, mobile, schoolId).ToList()
                : new List<StudentParentLookupRecord>();

            // Auto-create parent account if this is a first-time login from any school
            var parentName = students.FirstOrDefault()?.ParentName ?? "Parent";
            var parentId   = _parentAuth.GetOrCreateParentByMobile(mobile, parentName);

            var parentAccount = _parentAuth.GetById(parentId);
            if (parentAccount != null && !parentAccount.IsActive)
                return Fail(HttpStatusCode.Unauthorized, "Account is deactivated.");

            // Upsert each child link by admission_no — handles first login, class changes, and post-promotion student_id changes
            foreach (var student in students)
            {
                var existing = _parentAuth.GetChildLinkByAdmissionNo(parentId, student.AdmissionNo, tenant.GroupId);
                if (existing != null)
                    _parentAuth.UpdateChildLink(existing.LinkId, student.StudentId, student.StudentName, student.ClassName);
                else
                    _parentAuth.CreateChildLink(parentId, student.StudentId, tenant.GroupId,
                        tenant.DbName, schoolId, student.StudentName, student.ClassName, student.AdmissionNo);
            }

            // Device binding — revoke old tokens if a different device is logging in
            if (parentAccount != null &&
                !string.Equals(parentAccount.DeviceId, request.DeviceId.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                _parentAuth.RevokeAllRefreshTokens(parentId);
                _parentAuth.BindDevice(parentId, request.DeviceId.Trim());
            }
            else if (parentAccount != null && string.IsNullOrEmpty(parentAccount.DeviceId))
            {
                _parentAuth.BindDevice(parentId, request.DeviceId.Trim());
            }

            _parentAuth.UpdateLastLogin(parentId);
            return BuildParentAuthResponse(parentId, parentName, isLogin: true);
        }

        // GET mobile/auth/parent/children
        [HttpGet, Route("parent/children")]
        [MobileAuth]
        public HttpResponseMessage ParentGetChildren()
        {
            var mobile = MobileContext.Current;
            if (!mobile.IsParent) return Fail(HttpStatusCode.Forbidden, "Parent token required.");

            // Scope children to the school the app belongs to (X-School-Code)
            // Each white-label app should only show its own school's children
            var tenant = ResolveTenant();
            if (tenant == null)
                return Fail(HttpStatusCode.BadRequest, "X-School-Code header is required.");

            var children = _parentAuth.GetChildren(mobile.ParentId, tenant.GroupId)
                .Select(c => new ChildDto
                {
                    LinkId      = c.LinkId,
                    StudentId   = c.StudentId,
                    StudentName = c.StudentName,
                    ClassName   = c.ClassName,
                    AdmissionNo = c.AdmissionNo,
                    SchoolCode  = c.SchoolCode,
                    IsActive    = c.IsActive
                });

            return Request.CreateResponse(HttpStatusCode.OK, ApiResponse<IEnumerable<ChildDto>>.Ok(children));
        }

        // POST mobile/auth/parent/select-child
        [HttpPost, Route("parent/select-child")]
        [MobileAuth]
        public HttpResponseMessage ParentSelectChild([FromBody] SelectChildRequest request)
        {
            if (request == null || request.LinkId <= 0)
                return Fail(HttpStatusCode.BadRequest, "LinkId is required.");

            var mobile = MobileContext.Current;
            if (!mobile.IsParent) return Fail(HttpStatusCode.Forbidden, "Parent token required.");

            var link = _parentAuth.GetChildLink(request.LinkId, mobile.ParentId);
            if (link == null || !link.IsActive)
                return Fail(HttpStatusCode.NotFound, "Child link not found.");

            // Defense-in-depth: ensure the link belongs to this school's group
            var tenant = ResolveTenant();
            if (tenant != null && link.GroupId != tenant.GroupId)
                return Fail(HttpStatusCode.Forbidden, "Child does not belong to this school.");

            var accessToken = JwtHelper.GenerateMobileParentToken(new MobileParentClaims
            {
                ParentId    = mobile.ParentId,
                FullName    = mobile.DisplayName,
                StudentId   = link.StudentId,
                GroupId     = link.GroupId,
                SchoolId    = link.SchoolId,
                DbName      = link.DbName,
                StudentName = link.StudentName,
                ClassName   = link.ClassName,
                AdmissionNo = link.AdmissionNo,
            });

            return Request.CreateResponse(HttpStatusCode.OK, ApiResponse<MobileAuthResponse>.Ok(new MobileAuthResponse
            {
                AccessToken = accessToken,
                TokenType   = "parent",
                FullName    = link.StudentName,
                ClassName   = link.ClassName,
                AdmissionNo = link.AdmissionNo,
                StudentId   = link.StudentId,
                ParentId    = mobile.ParentId,
            }));
        }

        // POST mobile/auth/parent/link-child
        [HttpPost, Route("parent/link-child")]
        [MobileAuth]
        public HttpResponseMessage ParentLinkChild([FromBody] LinkChildRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.AdmissionNo) || string.IsNullOrWhiteSpace(request.SchoolCode))
                return Fail(HttpStatusCode.BadRequest, "AdmissionNo and SchoolCode are required.");

            var mobile = MobileContext.Current;
            if (!mobile.IsParent) return Fail(HttpStatusCode.Forbidden, "Parent token required.");

            var tenant = _groups.GetTenantBySubdomain(request.SchoolCode.Trim());
            if (tenant == null || tenant.GroupId == 0)
                return Fail(HttpStatusCode.NotFound, "School not found.");

            var schoolId = GetSchoolId(tenant.DbName);
            if (schoolId == 0) return Fail(HttpStatusCode.ServiceUnavailable, "No active school branch found.");

            var studentRepo = new MobileStudentAuthRepository(new TenantConnectionFactory());
            var student     = studentRepo.GetStudentByAdmissionNo(tenant.DbName, request.AdmissionNo.Trim(), schoolId);
            if (student == null)
                return Fail(HttpStatusCode.NotFound, "Student not found. Check the admission number and school code.");

            if (_parentAuth.ChildLinkExists(mobile.ParentId, student.StudentId, tenant.GroupId))
                return Fail(HttpStatusCode.Conflict, "This child is already linked to your account.");

            _parentAuth.CreateChildLink(mobile.ParentId, student.StudentId, tenant.GroupId,
                tenant.DbName, schoolId, student.StudentName, student.ClassName, student.AdmissionNo);

            return Request.CreateResponse(HttpStatusCode.Created, ApiResponse.Ok("Child linked successfully."));
        }

        // ================================================================
        // Helpers
        // ================================================================

        private TenantInfo ResolveTenant()
        {
            IEnumerable<string> vals;
            var subdomain = Request.Headers.TryGetValues("X-School-Code", out vals) ? vals.FirstOrDefault() : null;
            if (string.IsNullOrWhiteSpace(subdomain))
                subdomain = Request.Headers.TryGetValues("X-Subdomain", out vals) ? vals.FirstOrDefault() : null;
            if (string.IsNullOrWhiteSpace(subdomain)) return null;
            return _groups.GetTenantBySubdomain(subdomain);
        }

        private int GetSchoolId(string dbName)
        {
            // Resolve group_id from db_name, then get first active school
            int? groupId;
            using (var conn = new TenantConnectionFactory().GetMasterConnection())
                groupId = Dapper.SqlMapper.QueryFirstOrDefault<int?>(conn,
                    "SELECT group_id FROM school_groups WHERE db_name = @dbName AND status = 'Active'",
                    new { dbName });

            if (groupId.HasValue)
            {
                using (var conn = new TenantConnectionFactory().GetMasterConnection())
                {
                    var id = Dapper.SqlMapper.QueryFirstOrDefault<int?>(conn,
                        "SELECT TOP 1 school_id FROM schools WHERE group_id = @groupId AND status = 'Active'",
                        new { groupId = groupId.Value });
                    if (id.HasValue) return id.Value;
                }
            }

            // Fallback: first school_id from the tenant DB itself
            using (var conn = new TenantConnectionFactory().GetTenantConnection(dbName))
                return Dapper.SqlMapper.QueryFirstOrDefault<int>(conn,
                    "SELECT TOP 1 school_id FROM students WHERE school_id IS NOT NULL", null);
        }

        private StudentMobileRecord GetStudentRecord(string dbName, long studentId, int schoolId)
        {
            using (var conn = new TenantConnectionFactory().GetTenantConnection(dbName))
                return Dapper.SqlMapper.QueryFirstOrDefault<StudentMobileRecord>(conn,
                    @"SELECT s.student_id StudentId, s.admission_no AdmissionNo,
                             s.student_name StudentName, c.class_name ClassName
                      FROM students s
                      LEFT JOIN classes c ON c.class_id = s.class_id
                      WHERE s.student_id = @studentId",
                    new { studentId });
        }

        private HttpResponseMessage BuildStudentAuthResponse(
            TenantInfo tenant, int accountId, StudentMobileRecord student,
            int schoolId, bool isLogin, string rawRefresh = null)
        {
            var accessToken = JwtHelper.GenerateMobileStudentToken(new MobileStudentClaims
            {
                AccountId   = accountId,
                StudentId   = student.StudentId,
                GroupId     = tenant.GroupId,
                SchoolId    = schoolId,
                DbName      = tenant.DbName,
                StudentName = student.StudentName?.Trim(),
                ClassName   = student.ClassName,
                AdmissionNo = student.AdmissionNo,
            });

            if (rawRefresh == null)
            {
                rawRefresh = JwtHelper.GenerateRefreshToken();
                _studentAuth.CreateRefreshToken(tenant.DbName, accountId,
                    JwtHelper.HashRefreshToken(rawRefresh), JwtHelper.RefreshTokenExpiry());

                if (isLogin) _studentAuth.UpdateLastLogin(tenant.DbName, accountId);
            }

            var response = Request.CreateResponse(HttpStatusCode.OK, ApiResponse<MobileAuthResponse>.Ok(new MobileAuthResponse
            {
                AccessToken = accessToken,
                TokenType   = "student",
                FullName    = student.StudentName?.Trim(),
                ClassName   = student.ClassName,
                AdmissionNo = student.AdmissionNo,
                StudentId   = student.StudentId,
            }));

            SetCookie(response, "studentRefreshToken", rawRefresh, "/mobile/auth/student");
            return response;
        }

        private HttpResponseMessage BuildParentAuthResponse(
            int parentId, string fullName, bool isLogin, string rawRefresh = null)
        {
            var accessToken = JwtHelper.GenerateMobileParentToken(new MobileParentClaims
            {
                ParentId = parentId,
                FullName = fullName,
                // No child context yet — student/school fields left as defaults (0/null)
            });

            if (rawRefresh == null)
            {
                rawRefresh = JwtHelper.GenerateRefreshToken();
                _parentAuth.CreateRefreshToken(parentId,
                    JwtHelper.HashRefreshToken(rawRefresh), JwtHelper.RefreshTokenExpiry());
            }

            var response = Request.CreateResponse(HttpStatusCode.OK, ApiResponse<MobileAuthResponse>.Ok(new MobileAuthResponse
            {
                AccessToken = accessToken,
                TokenType   = "parent",
                FullName    = fullName,
                ParentId    = parentId,
            }));

            SetCookie(response, "parentRefreshToken", rawRefresh, "/mobile/auth/parent");
            return response;
        }

        private void SetCookie(HttpResponseMessage response, string name, string value, string path)
        {
            var cookie = new CookieHeaderValue(name, value)
            {
                HttpOnly = true,
                Secure   = false,
                Path     = path,
                Expires  = DateTimeOffset.UtcNow.AddDays(7)
            };
            response.Headers.AddCookies(new[] { cookie });
        }

        private void ClearCookie(HttpResponseMessage response, string name, string path)
        {
            var cookie = new CookieHeaderValue(name, "")
            {
                HttpOnly = true,
                Path     = path,
                Expires  = DateTimeOffset.UtcNow.AddDays(-1)
            };
            response.Headers.AddCookies(new[] { cookie });
        }

        private string GetRefreshCookie(string name)
        {
            var cookies = Request.Headers.GetCookies(name);
            if (cookies == null || cookies.Count == 0) return null;
            return cookies[0][name].Value;
        }

        private HttpResponseMessage Fail(HttpStatusCode status, string message)
            => Request.CreateResponse(status, ApiResponse.Fail(message));
    }

    public class ParentOtpRequest
    {
        public string Mobile   { get; set; }
        public string DeviceId { get; set; }
    }

    public class ParentVerifyOtpRequest
    {
        public string Mobile   { get; set; }
        public string Otp      { get; set; }
        public string DeviceId { get; set; }
    }
}
