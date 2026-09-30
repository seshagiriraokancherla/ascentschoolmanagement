using AscentSchools.Core.DTOs.School.Settings;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    [RoutePrefix("school/settings")]
    public class SchoolSettingsController : BaseSchoolController
    {
        private static readonly string[] AllowedSignatureExts = { ".png", ".jpg", ".jpeg" };

        private readonly SchoolSettingsRepository _repo;

        public SchoolSettingsController()
        {
            _repo = new SchoolSettingsRepository(new TenantConnectionFactory());
        }

        // GET school/settings
        [HttpGet, Route("")]
        public System.Net.Http.HttpResponseMessage Get()
        {
            var settings = _repo.Get(Tenant.SchoolId);
            return Ok(settings);
        }

        // GET school/settings/profile — branch name/address/contact for report headers
        [HttpGet, Route("profile")]
        public System.Net.Http.HttpResponseMessage GetProfile()
        {
            return Ok(_repo.GetProfile(Tenant.SchoolId));
        }

        // PUT school/settings
        [HttpPut, Route("")]
        public System.Net.Http.HttpResponseMessage Update([FromBody] UpdateSchoolSettingsRequest request)
        {
            if (request == null) return BadRequest("Request body required.");
            _repo.Upsert(Tenant.SchoolId, request, Tenant.FullName);
            return Ok(_repo.Get(Tenant.SchoolId));
        }

        // POST school/settings/signature (multipart/form-data) — uploads the institution
        // head's (Principal's) signature image, stored on this server under ~/Uploads/
        // (not R2 — this must work even for schools that haven't configured R2). Returns
        // the resulting URL only; the caller still has to PUT /school/settings to save it
        // into institution_head_signature, matching the existing "Save Settings" flow.
        // One fixed file per school — re-uploading (even with a different extension)
        // replaces the previous signature rather than accumulating files.
        [HttpPost, Route("signature")]
        public async Task<HttpResponseMessage> UploadSignature()
        {
            if (!Request.Content.IsMimeMultipartContent())
                return BadRequest("Expected multipart/form-data.");

            var uploadDir = HttpContext.Current.Server.MapPath("~/Uploads/signatures/");
            if (!Directory.Exists(uploadDir))
                Directory.CreateDirectory(uploadDir);

            var provider = new MultipartFormDataStreamProvider(uploadDir);
            await Request.Content.ReadAsMultipartAsync(provider);

            var fileData = provider.FileData.Count > 0 ? provider.FileData[0] : null;
            if (fileData == null) return BadRequest("No file received.");

            var rawName = fileData.Headers.ContentDisposition.FileName?.Trim('"') ?? "signature.png";
            var ext     = Path.GetExtension(rawName).ToLowerInvariant();

            if (Array.IndexOf(AllowedSignatureExts, ext) < 0)
            {
                File.Delete(fileData.LocalFileName);
                return BadRequest("Only PNG or JPG images are allowed.");
            }
            if (new FileInfo(fileData.LocalFileName).Length > 1024 * 1024)
            {
                File.Delete(fileData.LocalFileName);
                return BadRequest("Image must be 1 MB or smaller.");
            }

            // Clear any previously-uploaded signature under a different extension so only
            // one file for this school stays on disk.
            foreach (var otherExt in AllowedSignatureExts)
            {
                if (otherExt == ext) continue;
                var otherPath = Path.Combine(uploadDir, $"{Tenant.SchoolId}{otherExt}");
                if (File.Exists(otherPath)) File.Delete(otherPath);
            }

            var fileName = $"{Tenant.SchoolId}{ext}";
            var destPath = Path.Combine(uploadDir, fileName);
            if (File.Exists(destPath)) File.Delete(destPath);
            File.Move(fileData.LocalFileName, destPath);

            return Ok(new { url = $"/Uploads/signatures/{fileName}" });
        }
    }
}
