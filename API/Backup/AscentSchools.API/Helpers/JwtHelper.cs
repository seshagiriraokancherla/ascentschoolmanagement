using AscentSchools.Core.DTOs.Auth;
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

        private static SymmetricSecurityKey SigningKey =>
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));

        // ── Access Token ──────────────────────────────────────────────────

        public static string GenerateAccessToken(TokenClaims claims)
        {
            var claimsList = new List<Claim>
            {
                new Claim("userId",   claims.UserId.ToString()),
                new Claim("groupId",  claims.GroupId.ToString()),
                new Claim("schoolId", claims.SchoolId?.ToString() ?? ""),
                new Claim("dbName",   claims.TenantDbName ?? ""),
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
    }
}
