using AscentSchools.Core.DTOs.Auth;
using AscentSchools.Core.DTOs.Control.Auth;
using AscentSchools.Core.DTOs.Mobile.Auth;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AscentSchools.API.Helpers
{
    public static class JwtHelper
    {
        private static string Secret      => ConfigurationManager.AppSettings["Jwt:Secret"];
        private static string Issuer      => ConfigurationManager.AppSettings["Jwt:Issuer"];
        private static string Audience    => ConfigurationManager.AppSettings["Jwt:Audience"];
        private static int    AccessMins  => int.Parse(ConfigurationManager.AppSettings["Jwt:AccessTokenMins"]);
        private static int    RefreshDays => int.Parse(ConfigurationManager.AppSettings["Jwt:RefreshTokenDays"]);

        // Mobile app sessions live much longer than web (avoids re-OTP → SMS cost).
        // Defaults to 365 days when the config key is absent.
        public  static int    MobileRefreshDays
        {
            get
            {
                var v = ConfigurationManager.AppSettings["Jwt:MobileRefreshTokenDays"];
                return int.TryParse(v, out var d) && d > 0 ? d : 365;
            }
        }

        // Mobile access token lifetime — separate from the web/control access token
        // (Jwt:AccessTokenMins) so staff/admin web sessions stay short (revocable) while
        // the mobile app carries a long-lived access token. Defaults to 15 days (21600 min)
        // when the config key is absent.
        private static int    MobileAccessMins
        {
            get
            {
                var v = ConfigurationManager.AppSettings["Jwt:MobileAccessTokenMins"];
                return int.TryParse(v, out var m) && m > 0 ? m : 21600;
            }
        }

        private static SymmetricSecurityKey SigningKey =>
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));

        // ── Access Token ──────────────────────────────────────────────────

        public static string GenerateAccessToken(TokenClaims claims)
        {
            var claimsList = new List<Claim>
            {
                new Claim("tokenType", "school"),
                new Claim("userId",    claims.UserId.ToString()),
                new Claim("fullName",  claims.FullName ?? ""),
                new Claim("groupId",   claims.GroupId.ToString()),
                new Claim("schoolId",  claims.SchoolId?.ToString() ?? ""),
                new Claim("dbName",    claims.TenantDbName ?? ""),
            };

            // Add each permission as a separate claim
            if (claims.Permissions != null)
                foreach (var p in claims.Permissions)
                    claimsList.Add(new Claim("perm", p));

            var token = new JwtSecurityToken(
                issuer:             Issuer,
                audience:           Audience,
                claims:             claimsList,
                notBefore:          DateTime.UtcNow,
                expires:            DateTime.UtcNow.AddMinutes(AccessMins),
                signingCredentials: new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static ClaimsPrincipal ValidateAccessToken(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var parameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidIssuer              = Issuer,
                    ValidateAudience         = true,
                    ValidAudience            = Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey         = SigningKey,
                    ValidateLifetime         = true,
                    ClockSkew                = TimeSpan.Zero
                };
                return handler.ValidateToken(token, parameters, out _);
            }
            catch
            {
                return null;
            }
        }

        public static TokenClaims ExtractClaims(ClaimsPrincipal principal)
        {
            if (principal == null) return null;
            var claims = principal.Claims.ToList();

            int.TryParse(claims.FirstOrDefault(c => c.Type == "userId")?.Value,  out var userId);
            int.TryParse(claims.FirstOrDefault(c => c.Type == "groupId")?.Value, out var groupId);
            int? schoolId = null;
            var schoolIdStr = claims.FirstOrDefault(c => c.Type == "schoolId")?.Value;
            if (!string.IsNullOrEmpty(schoolIdStr) && int.TryParse(schoolIdStr, out var sid))
                schoolId = sid;

            return new TokenClaims
            {
                UserId       = userId,
                FullName     = claims.FirstOrDefault(c => c.Type == "fullName")?.Value,
                GroupId      = groupId,
                SchoolId     = schoolId,
                TenantDbName = claims.FirstOrDefault(c => c.Type == "dbName")?.Value,
                Permissions  = claims.Where(c => c.Type == "perm").Select(c => c.Value).ToArray()
            };
        }

        // ── Refresh Token ─────────────────────────────────────────────────

        /// <summary>Generates a cryptographically random refresh token string.</summary>
        public static string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            using (var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>Returns SHA-256 hash of the raw refresh token for DB storage.</summary>
        public static string HashRefreshToken(string rawToken)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(rawToken));
                return Convert.ToBase64String(bytes);
            }
        }

        public static DateTime RefreshTokenExpiry() =>
            DateTime.UtcNow.AddDays(RefreshDays);

        public static DateTime MobileRefreshTokenExpiry() =>
            DateTime.UtcNow.AddDays(MobileRefreshDays);

        // ── Control App Token ─────────────────────────────────────────────

        public static string GenerateControlAccessToken(ControlTokenClaims claims)
        {
            var claimsList = new List<Claim>
            {
                new Claim("tokenType", "control"),
                new Claim("userId",    claims.UserId.ToString()),
                new Claim("fullName",  claims.FullName ?? ""),
                new Claim("role",      claims.Role     ?? ""),
            };

            var token = new JwtSecurityToken(
                issuer:             Issuer,
                audience:           Audience,
                claims:             claimsList,
                notBefore:          DateTime.UtcNow,
                expires:            DateTime.UtcNow.AddMinutes(AccessMins),
                signingCredentials: new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // ── Mobile App Tokens ─────────────────────────────────────────────

        /// <summary>Issues a student mobile token (tokenType=student).</summary>
        public static string GenerateMobileStudentToken(MobileStudentClaims claims)
        {
            var claimsList = new List<Claim>
            {
                new Claim("tokenType",   "student"),
                new Claim("accountId",   claims.AccountId.ToString()),
                new Claim("studentId",   claims.StudentId.ToString()),
                new Claim("groupId",     claims.GroupId.ToString()),
                new Claim("schoolId",    claims.SchoolId.ToString()),
                new Claim("dbName",      claims.DbName      ?? ""),
                new Claim("studentName", claims.StudentName ?? ""),
                new Claim("className",   claims.ClassName   ?? ""),
                new Claim("admissionNo", claims.AdmissionNo ?? ""),
            };

            return BuildToken(claimsList, MobileAccessMins);
        }

        /// <summary>Issues a parent-init token (tokenType=parent) with no child context yet.</summary>
        public static string GenerateMobileParentToken(MobileParentClaims claims)
        {
            var claimsList = new List<Claim>
            {
                new Claim("tokenType",   "parent"),
                new Claim("parentId",    claims.ParentId.ToString()),
                new Claim("fullName",    claims.FullName    ?? ""),
                new Claim("studentId",   claims.StudentId.ToString()),
                new Claim("groupId",     claims.GroupId.ToString()),
                new Claim("schoolId",    claims.SchoolId.ToString()),
                new Claim("dbName",      claims.DbName      ?? ""),
                new Claim("studentName", claims.StudentName ?? ""),
                new Claim("className",   claims.ClassName   ?? ""),
                new Claim("admissionNo", claims.AdmissionNo ?? ""),
            };

            return BuildToken(claimsList, MobileAccessMins);
        }

        public static MobileStudentClaims ExtractMobileStudentClaims(ClaimsPrincipal principal)
        {
            if (principal == null) return null;
            var c = principal.Claims.ToList();
            long.TryParse(c.FirstOrDefault(x => x.Type == "studentId")?.Value,  out var sid);
            int.TryParse(c.FirstOrDefault(x => x.Type == "accountId")?.Value,   out var aid);
            int.TryParse(c.FirstOrDefault(x => x.Type == "groupId")?.Value,     out var gid);
            int.TryParse(c.FirstOrDefault(x => x.Type == "schoolId")?.Value,    out var scid);
            return new MobileStudentClaims
            {
                AccountId   = aid,
                StudentId   = sid,
                GroupId     = gid,
                SchoolId    = scid,
                DbName      = c.FirstOrDefault(x => x.Type == "dbName")?.Value,
                StudentName = c.FirstOrDefault(x => x.Type == "studentName")?.Value,
                ClassName   = c.FirstOrDefault(x => x.Type == "className")?.Value,
                AdmissionNo = c.FirstOrDefault(x => x.Type == "admissionNo")?.Value,
            };
        }

        public static MobileParentClaims ExtractMobileParentClaims(ClaimsPrincipal principal)
        {
            if (principal == null) return null;
            var c = principal.Claims.ToList();
            int.TryParse(c.FirstOrDefault(x => x.Type == "parentId")?.Value,    out var pid);
            long.TryParse(c.FirstOrDefault(x => x.Type == "studentId")?.Value,  out var sid);
            int.TryParse(c.FirstOrDefault(x => x.Type == "groupId")?.Value,     out var gid);
            int.TryParse(c.FirstOrDefault(x => x.Type == "schoolId")?.Value,    out var scid);
            return new MobileParentClaims
            {
                ParentId    = pid,
                FullName    = c.FirstOrDefault(x => x.Type == "fullName")?.Value,
                StudentId   = sid,
                GroupId     = gid,
                SchoolId    = scid,
                DbName      = c.FirstOrDefault(x => x.Type == "dbName")?.Value,
                StudentName = c.FirstOrDefault(x => x.Type == "studentName")?.Value,
                ClassName   = c.FirstOrDefault(x => x.Type == "className")?.Value,
                AdmissionNo = c.FirstOrDefault(x => x.Type == "admissionNo")?.Value,
            };
        }

        // ── Teacher Mobile Token ──────────────────────────────────────────

        /// <summary>Issues a teacher mobile token (tokenType=teacher).</summary>
        public static string GenerateMobileTeacherToken(int userId, int schoolId, int groupId, string dbName, string fullName)
        {
            var claimsList = new List<Claim>
            {
                new Claim("tokenType", "teacher"),
                new Claim("userId",    userId.ToString()),
                new Claim("schoolId",  schoolId.ToString()),
                new Claim("groupId",   groupId.ToString()),
                new Claim("dbName",    dbName   ?? ""),
                new Claim("fullName",  fullName ?? ""),
            };
            return BuildToken(claimsList, MobileAccessMins);
        }

        private static string BuildToken(List<Claim> claims, int expiryMinutes)
        {
            var token = new JwtSecurityToken(
                issuer:             Issuer,
                audience:           Audience,
                claims:             claims,
                notBefore:          DateTime.UtcNow,
                expires:            DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256)
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
