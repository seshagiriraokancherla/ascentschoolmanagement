using AscentSchools.API.Helpers;
using AscentSchools.Core.DTOs.School.Media;
using AscentSchools.Core.DTOs.School.Settings;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    /// <summary>R2 storage config + presigned-upload URLs (direct browser→R2 uploads).</summary>
    [RoutePrefix("school")]
    public class SchoolUploadsController : BaseSchoolController
    {
        private readonly R2ConfigRepository    _r2;
        private readonly StudentRepository     _students;
        private readonly MediaUploadsRepository _media;

        public SchoolUploadsController()
        {
            var db    = new TenantConnectionFactory();
            _r2       = new R2ConfigRepository(db);
            _students = new StudentRepository(db);
            _media    = new MediaUploadsRepository(db);
        }

        // GET school/settings/r2 — safe config (no secret; hasSecretKey flag)
        [HttpGet, Route("settings/r2")]
        public HttpResponseMessage GetR2Config()
            => Ok(_r2.GetConfig(Tenant.TenantDbName, Tenant.SchoolId));

        // PUT school/settings/r2 — upsert (blank secret keeps existing)
        [HttpPut, Route("settings/r2")]
        public HttpResponseMessage SaveR2Config([FromBody] UpdateR2ConfigRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.AccountId) || string.IsNullOrWhiteSpace(req.AccessKeyId)
                || string.IsNullOrWhiteSpace(req.BucketName) || string.IsNullOrWhiteSpace(req.PublicBaseUrl))
                return BadRequest("Account ID, Access Key ID, Bucket name and Public Base URL are required.");

            _r2.Upsert(Tenant.TenantDbName, Tenant.SchoolId, req, Tenant.FullName);
            return Ok(_r2.GetConfig(Tenant.TenantDbName, Tenant.SchoolId));
        }

        // POST school/uploads/presign — returns a presigned PUT URL + the permanent public URL.
        // Browser uploads the file directly to UploadUrl, then saves PublicUrl to the DB.
        [HttpPost, Route("uploads/presign")]
        public HttpResponseMessage Presign([FromBody] PresignRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Purpose) || string.IsNullOrWhiteSpace(req.FileName))
                return BadRequest("purpose and fileName are required.");

            var cfg = _r2.GetInternal(Tenant.TenantDbName, Tenant.SchoolId);
            if (cfg == null)
                return BadRequest("R2 storage is not configured (or disabled) for this school.");

            var ext = SafeExt(req.FileName);
            string key;

            switch (req.Purpose.ToLowerInvariant())
            {
                case "student-photo":
                    var student = _students.GetById(Tenant.TenantDbName, Tenant.SchoolId, req.EntityId);
                    if (student == null) return NotFound("Student not found.");
                    // student-images/{AdmissionNo}_{academicYear}.{ext}  e.g. 1234_2026-2027.jpg
                    key = "student-images/" + Sanitize(student.AdmissionNo) + "_" + Sanitize(student.AcademicYear) + ext;
                    break;

                case "homework":
                    // homeworks/{academicYear}/{homeworkId}/{unique}{ext}  (year folders → delete old years)
                    var yr = Sanitize(_media.GetCurrentAcademicYear(Tenant.TenantDbName, Tenant.SchoolId) ?? "unknown");
                    key = "homeworks/" + yr + "/" + req.EntityId + "/" + Unique() + "_" + Sanitize(Path.GetFileNameWithoutExtension(req.FileName)) + ext;
                    break;

                case "announcement":
                    key = "announcements/" + req.EntityId + "/" + Unique() + "_" + Sanitize(Path.GetFileNameWithoutExtension(req.FileName)) + ext;
                    break;

                case "event":
                    key = "events/" + req.EntityId + "/" + Unique() + "_" + Sanitize(Path.GetFileNameWithoutExtension(req.FileName)) + ext;
                    break;

                default:
                    return BadRequest("Unsupported upload purpose.");
            }

            // Videos need a longer window (1 GB uploads); 10 min for the rest.
            var expiry = (req.ContentType ?? "").StartsWith("video/") ? 3600 : 600;
            return Ok(new PresignResponse
            {
                UploadUrl = R2Service.PresignPut(cfg, key, expiry),
                PublicUrl = R2Service.PublicUrl(cfg, key),
                Key       = key
            });
        }

        // POST school/media/attach — record an uploaded file (after the presigned PUT succeeds).
        [HttpPost, Route("media/attach")]
        public HttpResponseMessage AttachMedia([FromBody] AttachMediaRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.EntityType) || req.EntityId <= 0
                || string.IsNullOrWhiteSpace(req.FileUrl))
                return BadRequest("entityType, entityId and fileUrl are required.");

            var row = _media.Insert(Tenant.TenantDbName, Tenant.SchoolId, req, Tenant.FullName);
            return Ok(row);
        }

        // GET school/media?entityType=homework&entityId=5
        [HttpGet, Route("media")]
        public HttpResponseMessage ListMedia([FromUri] string entityType, [FromUri] long entityId)
        {
            if (string.IsNullOrWhiteSpace(entityType) || entityId <= 0)
                return BadRequest("entityType and entityId are required.");
            return Ok(_media.GetByEntity(Tenant.TenantDbName, Tenant.SchoolId, entityType, entityId));
        }

        // DELETE school/media/{id}
        [HttpDelete, Route("media/{id:long}")]
        public HttpResponseMessage DeleteMedia(long id)
        {
            _media.Delete(Tenant.TenantDbName, Tenant.SchoolId, id);
            return Ok<object>(null, "Attachment removed.");
        }

        // ── helpers ──────────────────────────────────────────────────────────
        private static string SafeExt(string fileName)
        {
            var ext = Path.GetExtension(fileName ?? "").ToLowerInvariant();
            return Regex.IsMatch(ext, "^\\.[a-z0-9]{1,5}$") ? ext : "";
        }

        private static string Sanitize(string s)
            => string.IsNullOrWhiteSpace(s) ? "unknown" : Regex.Replace(s.Trim(), "[^A-Za-z0-9._-]", "-");

        private static string Unique() => Guid.NewGuid().ToString("N").Substring(0, 10);
    }
}
