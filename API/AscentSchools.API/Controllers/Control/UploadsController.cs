using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;

namespace AscentSchools.API.Controllers.Control
{
    /// <summary>
    /// Handles file uploads for the control app (branding images).
    /// Files are saved to ~/Uploads/branding/ and served as static files by IIS.
    /// </summary>
    [RoutePrefix("control/uploads")]
    public class ControlUploadsController : BaseControlController
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".ico", ".webp" };
        private const int MaxFileSizeBytes = 2 * 1024 * 1024; // 2 MB

        // POST control/uploads/image
        [HttpPost, Route("image")]
        public HttpResponseMessage UploadImage()
        {
            var files = HttpContext.Current.Request.Files;
            if (files == null || files.Count == 0)
                return BadRequest("No file uploaded.");

            var file = files[0];
            if (file.ContentLength == 0)
                return BadRequest("File is empty.");

            if (file.ContentLength > MaxFileSizeBytes)
                return BadRequest("File exceeds 2 MB limit.");

            var ext = Path.GetExtension(file.FileName ?? "").ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                return BadRequest($"Allowed types: {string.Join(", ", AllowedExtensions)}");

            var uploadDir = HostingEnvironment.MapPath("~/Uploads/branding/");
            if (!Directory.Exists(uploadDir))
                Directory.CreateDirectory(uploadDir);

            // Unique filename — timestamp + sanitised original name
            var safeName  = Path.GetFileNameWithoutExtension(file.FileName)
                                .Replace(" ", "_")
                                .Replace("..", "");
            var uniqueName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{safeName}{ext}";
            file.SaveAs(Path.Combine(uploadDir, uniqueName));

            return Ok(new { path = $"/Uploads/branding/{uniqueName}" }, "Uploaded.");
        }
    }
}
