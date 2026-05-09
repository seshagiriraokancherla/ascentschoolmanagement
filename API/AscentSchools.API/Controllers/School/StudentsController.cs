using AscentSchools.Core.DTOs.School.Students;
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
    [RoutePrefix("school/students")]
    public class StudentsController : BaseSchoolController
    {
        private readonly StudentRepository _repo;

        public StudentsController()
        {
            _repo = new StudentRepository(new TenantConnectionFactory());
        }

        // GET school/students?search=&classId=&sectionId=&academicYearId=&status=&bloodGroup=
        [HttpGet, Route("")]
        public HttpResponseMessage GetStudents(
            string search         = null,
            int?   classId        = null,
            int?   sectionId      = null,
            int?   academicYearId = null,
            string status         = null,
            string bloodGroup     = null)
        {
            var students = _repo.GetAll(
                Tenant.TenantDbName, Tenant.SchoolId,
                search, classId, sectionId, academicYearId, status, bloodGroup);
            return Ok(students);
        }

        // GET school/students/{id}
        [HttpGet, Route("{id:long}")]
        public HttpResponseMessage GetStudent(long id)
        {
            var student = _repo.GetById(Tenant.TenantDbName, Tenant.SchoolId, id);
            if (student == null) return NotFound("Student not found.");
            return Ok(student);
        }

        // POST school/students
        [HttpPost, Route("")]
        public HttpResponseMessage CreateStudent([FromBody] SaveStudentRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.StudentName))
                return BadRequest("Student name is required.");

            var studentId = _repo.Create(Tenant.TenantDbName, Tenant.SchoolId, request);
            var student   = _repo.GetById(Tenant.TenantDbName, Tenant.SchoolId, studentId);
            return Created(student, "Student registered.");
        }

        // PUT school/students/{id}
        [HttpPut, Route("{id:long}")]
        public HttpResponseMessage UpdateStudent(long id, [FromBody] SaveStudentRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.StudentName))
                return BadRequest("Student name is required.");

            _repo.Update(Tenant.TenantDbName, Tenant.SchoolId, id, request);
            var student = _repo.GetById(Tenant.TenantDbName, Tenant.SchoolId, id);
            return Ok(student);
        }

        // PUT school/students/{id}/section
        [HttpPut, Route("{id:long}/section")]
        public HttpResponseMessage ChangeSection(long id, [FromBody] ChangeSectionRequest request)
        {
            if (request == null || request.SectionId <= 0)
                return BadRequest("sectionId is required.");
            _repo.ChangeSection(Tenant.TenantDbName, Tenant.SchoolId, id, request.SectionId);
            return Ok(true, "Section updated.");
        }

        // POST school/students/{id}/photo  (multipart/form-data)
        [HttpPost, Route("{id:long}/photo")]
        public async Task<HttpResponseMessage> UploadPhoto(long id)
        {
            if (!Request.Content.IsMimeMultipartContent())
                return BadRequest("Expected multipart/form-data.");

            // Confirm student exists and belongs to this school
            var student = _repo.GetById(Tenant.TenantDbName, Tenant.SchoolId, id);
            if (student == null) return NotFound("Student not found.");

            var uploadDir = HttpContext.Current.Server.MapPath(
                $"~/uploads/student-photos/{Tenant.SchoolId}/");

            if (!Directory.Exists(uploadDir))
                Directory.CreateDirectory(uploadDir);

            var provider = new System.Net.Http.MultipartFormDataStreamProvider(uploadDir);
            await Request.Content.ReadAsMultipartAsync(provider);

            var fileData = provider.FileData.Count > 0 ? provider.FileData[0] : null;
            if (fileData == null) return BadRequest("No file received.");

            // Rename temp file to {studentId}.ext
            var rawName  = fileData.Headers.ContentDisposition.FileName?.Trim('"') ?? "photo.jpg";
            var ext      = Path.GetExtension(rawName).ToLowerInvariant();
            var fileName = $"{id}{ext}";
            var destPath = Path.Combine(uploadDir, fileName);

            if (File.Exists(destPath)) File.Delete(destPath);
            File.Move(fileData.LocalFileName, destPath);

            var photoPath = $"/uploads/student-photos/{Tenant.SchoolId}/{fileName}";
            _repo.UpdatePhotoPath(Tenant.TenantDbName, Tenant.SchoolId, id, photoPath);

            return Ok(new { photoPath });
        }

        // POST school/students/bulk
        [HttpPost, Route("bulk")]
        public HttpResponseMessage BulkImport([FromBody] BulkStudentImportRequest request)
        {
            if (request == null || request.Rows == null || request.Rows.Count == 0)
                return BadRequest("No rows provided.");
            if (request.Rows.Count > 500)
                return BadRequest("Maximum 500 rows per upload.");

            // Stamp row numbers (1-based, matching CSV row after header)
            for (int i = 0; i < request.Rows.Count; i++)
                request.Rows[i].RowNumber = i + 2;

            var result = _repo.BulkCreate(Tenant.TenantDbName, Tenant.SchoolId, request);
            return Ok(result);
        }

        // GET school/students/promote/preview?fromYearId=&fromClassId=&fromSectionId=
        [HttpGet, Route("promote/preview")]
        public HttpResponseMessage GetPromotePreview(
            int fromYearId, int fromClassId, int? fromSectionId = null)
        {
            var students      = _repo.GetForPromotion(
                Tenant.TenantDbName, Tenant.SchoolId, fromYearId, fromClassId, fromSectionId);
            var detainedCount = _repo.CountDetained(
                Tenant.TenantDbName, Tenant.SchoolId, fromYearId, fromClassId, fromSectionId);
            return Ok(new PromotePreviewResponse { Students = students, DetainedCount = detainedCount });
        }

        // POST school/students/promote
        [HttpPost, Route("promote")]
        public HttpResponseMessage Promote([FromBody] PromoteStudentsRequest request)
        {
            if (request == null)
                return BadRequest("Request body is required.");
            if (request.FromAcademicYearId == 0 || request.FromClassId == 0 ||
                request.ToAcademicYearId == 0   || request.ToClassId == 0)
                return BadRequest("FromAcademicYearId, FromClassId, ToAcademicYearId and ToClassId are required.");
            if (request.FromAcademicYearId == request.ToAcademicYearId &&
                request.FromClassId        == request.ToClassId)
                return BadRequest("Source and target are the same — nothing to promote.");

            var result = _repo.Promote(Tenant.TenantDbName, Tenant.SchoolId, request);
            return Ok(result);
        }

        // PUT school/students/{id}/detain
        [HttpPut, Route("{id:long}/detain")]
        public HttpResponseMessage Detain(long id, [FromBody] DetainStudentRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Reason))
                return BadRequest("A detention reason is required.");
            _repo.DetainStudent(Tenant.TenantDbName, Tenant.SchoolId, id, req.Reason.Trim());
            return Ok(true, "Student marked as detained.");
        }

        // PUT school/students/{id}/release
        [HttpPut, Route("{id:long}/release")]
        public HttpResponseMessage Release(long id)
        {
            _repo.ReleaseStudent(Tenant.TenantDbName, Tenant.SchoolId, id);
            return Ok(true, "Student detention cleared.");
        }

        // PUT school/students/{id}/block
        [HttpPut, Route("{id:long}/block")]
        public HttpResponseMessage Block(long id, [FromBody] BlockStudentRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Reason))
                return BadRequest("A block reason is required.");
            _repo.BlockStudent(Tenant.TenantDbName, Tenant.SchoolId, id, req.Reason.Trim());
            return Ok(true, "Student blocked.");
        }

        // PUT school/students/{id}/unblock
        [HttpPut, Route("{id:long}/unblock")]
        public HttpResponseMessage Unblock(long id)
        {
            _repo.UnblockStudent(Tenant.TenantDbName, Tenant.SchoolId, id);
            return Ok(true, "Student unblocked.");
        }
    }
}
