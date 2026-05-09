using AscentSchools.Core.DTOs.School.Master;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    [RoutePrefix("school/master")]
    public class MasterDataController : BaseSchoolController
    {
        private readonly MasterDataRepository _repo;

        public MasterDataController()
        {
            _repo = new MasterDataRepository(new TenantConnectionFactory());
        }

        // ── Academic Years ────────────────────────────────────────────────

        [HttpGet, Route("academic-years")]
        public HttpResponseMessage GetAcademicYears()
            => Ok(_repo.GetAcademicYears(Tenant.TenantDbName, Tenant.SchoolId));

        [HttpPost, Route("academic-years")]
        public HttpResponseMessage CreateAcademicYear([FromBody] SaveAcademicYearRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.AcademicYear))
                return BadRequest("Academic year name is required.");
            _repo.CreateAcademicYear(Tenant.TenantDbName, Tenant.SchoolId, request);
            return Created(_repo.GetAcademicYears(Tenant.TenantDbName, Tenant.SchoolId), "Academic year created.");
        }

        [HttpPut, Route("academic-years/{id:int}")]
        public HttpResponseMessage UpdateAcademicYear(int id, [FromBody] SaveAcademicYearRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.AcademicYear))
                return BadRequest("Academic year name is required.");
            _repo.UpdateAcademicYear(Tenant.TenantDbName, Tenant.SchoolId, id, request);
            return Ok(_repo.GetAcademicYears(Tenant.TenantDbName, Tenant.SchoolId));
        }

        // ── Class Groups ──────────────────────────────────────────────────

        [HttpGet, Route("class-groups")]
        public HttpResponseMessage GetClassGroups()
            => Ok(_repo.GetClassGroups(Tenant.TenantDbName, Tenant.SchoolId));

        [HttpPost, Route("class-groups")]
        public HttpResponseMessage CreateClassGroup([FromBody] SaveClassGroupRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.GroupName))
                return BadRequest("Group name is required.");
            _repo.CreateClassGroup(Tenant.TenantDbName, Tenant.SchoolId, request);
            return Created(_repo.GetClassGroups(Tenant.TenantDbName, Tenant.SchoolId), "Class group created.");
        }

        [HttpPut, Route("class-groups/{id:int}")]
        public HttpResponseMessage UpdateClassGroup(int id, [FromBody] SaveClassGroupRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.GroupName))
                return BadRequest("Group name is required.");
            _repo.UpdateClassGroup(Tenant.TenantDbName, Tenant.SchoolId, id, request);
            return Ok(_repo.GetClassGroups(Tenant.TenantDbName, Tenant.SchoolId));
        }

        // ── Fee Categories ────────────────────────────────────────────────

        [HttpGet, Route("fee-categories")]
        public HttpResponseMessage GetFeeCategories()
            => Ok(_repo.GetFeeCategories(Tenant.TenantDbName, Tenant.SchoolId));

        [HttpPost, Route("fee-categories")]
        public HttpResponseMessage CreateFeeCategory([FromBody] SaveFeeCategoryRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.CategoryName))
                return BadRequest("Category name is required.");
            _repo.CreateFeeCategory(Tenant.TenantDbName, Tenant.SchoolId, request);
            return Created(_repo.GetFeeCategories(Tenant.TenantDbName, Tenant.SchoolId), "Fee category created.");
        }

        [HttpPut, Route("fee-categories/{id:int}")]
        public HttpResponseMessage UpdateFeeCategory(int id, [FromBody] SaveFeeCategoryRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.CategoryName))
                return BadRequest("Category name is required.");
            _repo.UpdateFeeCategory(Tenant.TenantDbName, Tenant.SchoolId, id, request);
            return Ok(_repo.GetFeeCategories(Tenant.TenantDbName, Tenant.SchoolId));
        }

        // ── Classes ───────────────────────────────────────────────────────

        [HttpGet, Route("classes")]
        public HttpResponseMessage GetClasses()
            => Ok(_repo.GetClasses(Tenant.TenantDbName, Tenant.SchoolId));

        [HttpPost, Route("classes")]
        public HttpResponseMessage CreateClass([FromBody] SaveClassRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ClassName))
                return BadRequest("Class name is required.");
            _repo.CreateClass(Tenant.TenantDbName, Tenant.SchoolId, request);
            return Created(_repo.GetClasses(Tenant.TenantDbName, Tenant.SchoolId), "Class created.");
        }

        [HttpPut, Route("classes/{id:int}")]
        public HttpResponseMessage UpdateClass(int id, [FromBody] SaveClassRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ClassName))
                return BadRequest("Class name is required.");
            _repo.UpdateClass(Tenant.TenantDbName, Tenant.SchoolId, id, request);
            return Ok(_repo.GetClasses(Tenant.TenantDbName, Tenant.SchoolId));
        }

        // ── Fee Types ─────────────────────────────────────────────────────

        [HttpGet, Route("fee-types")]
        public HttpResponseMessage GetFeeTypes()
            => Ok(_repo.GetFeeTypes(Tenant.TenantDbName, Tenant.SchoolId));

        [HttpPost, Route("fee-types")]
        public HttpResponseMessage CreateFeeType([FromBody] SaveFeeTypeRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.FeeTypeName))
                return BadRequest("Fee type name is required.");
            _repo.CreateFeeType(Tenant.TenantDbName, Tenant.SchoolId, request);
            return Created(_repo.GetFeeTypes(Tenant.TenantDbName, Tenant.SchoolId), "Fee type created.");
        }

        [HttpPut, Route("fee-types/{id:int}")]
        public HttpResponseMessage UpdateFeeType(int id, [FromBody] SaveFeeTypeRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.FeeTypeName))
                return BadRequest("Fee type name is required.");
            _repo.UpdateFeeType(Tenant.TenantDbName, Tenant.SchoolId, id, request);
            return Ok(_repo.GetFeeTypes(Tenant.TenantDbName, Tenant.SchoolId));
        }

        // ── Terms ─────────────────────────────────────────────────────────

        [HttpGet, Route("terms")]
        public HttpResponseMessage GetTerms()
            => Ok(_repo.GetTerms(Tenant.TenantDbName, Tenant.SchoolId));

        [HttpPost, Route("terms")]
        public HttpResponseMessage CreateTerm([FromBody] SaveTermRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.TermName))
                return BadRequest("Term name is required.");
            _repo.CreateTerm(Tenant.TenantDbName, Tenant.SchoolId, request);
            return Created(_repo.GetTerms(Tenant.TenantDbName, Tenant.SchoolId), "Term created.");
        }

        [HttpPut, Route("terms/{id:int}")]
        public HttpResponseMessage UpdateTerm(int id, [FromBody] SaveTermRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.TermName))
                return BadRequest("Term name is required.");
            _repo.UpdateTerm(Tenant.TenantDbName, Tenant.SchoolId, id, request);
            return Ok(_repo.GetTerms(Tenant.TenantDbName, Tenant.SchoolId));
        }

        // ── Subjects ──────────────────────────────────────────────────────

        [HttpGet, Route("subjects")]
        public HttpResponseMessage GetSubjects()
            => Ok(_repo.GetSubjects(Tenant.TenantDbName, Tenant.SchoolId));

        [HttpPost, Route("subjects")]
        public HttpResponseMessage CreateSubject([FromBody] SaveSubjectRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.SubjectName))
                return BadRequest("Subject name is required.");
            _repo.CreateSubject(Tenant.TenantDbName, Tenant.SchoolId, request);
            return Created(_repo.GetSubjects(Tenant.TenantDbName, Tenant.SchoolId), "Subject created.");
        }

        [HttpPut, Route("subjects/{id:int}")]
        public HttpResponseMessage UpdateSubject(int id, [FromBody] SaveSubjectRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.SubjectName))
                return BadRequest("Subject name is required.");
            _repo.UpdateSubject(Tenant.TenantDbName, Tenant.SchoolId, id, request);
            return Ok(_repo.GetSubjects(Tenant.TenantDbName, Tenant.SchoolId));
        }

        // ── Payment Modes ─────────────────────────────────────────────────

        [HttpGet, Route("payment-modes")]
        public HttpResponseMessage GetPaymentModes()
            => Ok(_repo.GetPaymentModes(Tenant.TenantDbName, Tenant.SchoolId));

        [HttpPost, Route("payment-modes")]
        public HttpResponseMessage CreatePaymentMode([FromBody] SavePaymentModeRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ModeName))
                return BadRequest("Mode name is required.");
            _repo.CreatePaymentMode(Tenant.TenantDbName, Tenant.SchoolId, request);
            return Created(_repo.GetPaymentModes(Tenant.TenantDbName, Tenant.SchoolId), "Payment mode created.");
        }

        [HttpPut, Route("payment-modes/{id:int}")]
        public HttpResponseMessage UpdatePaymentMode(int id, [FromBody] SavePaymentModeRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ModeName))
                return BadRequest("Mode name is required.");
            _repo.UpdatePaymentMode(Tenant.TenantDbName, Tenant.SchoolId, id, request);
            return Ok(_repo.GetPaymentModes(Tenant.TenantDbName, Tenant.SchoolId));
        }

        // ── Sections ──────────────────────────────────────────────────────

        [HttpGet, Route("sections")]
        public HttpResponseMessage GetSections([FromUri] int classId)
        {
            if (classId <= 0)
                return BadRequest("classId is required.");
            return Ok(_repo.GetSections(Tenant.TenantDbName, Tenant.SchoolId, classId));
        }

        [HttpPost, Route("sections")]
        public HttpResponseMessage CreateSection([FromBody] SaveSectionRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.SectionName))
                return BadRequest("Section name is required.");
            if (request.ClassId <= 0)
                return BadRequest("classId is required.");
            _repo.CreateSection(Tenant.TenantDbName, Tenant.SchoolId, request);
            return Created(_repo.GetSections(Tenant.TenantDbName, Tenant.SchoolId, request.ClassId), "Section created.");
        }

        [HttpPut, Route("sections/{id:int}")]
        public HttpResponseMessage UpdateSection(int id, [FromBody] SaveSectionRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.SectionName))
                return BadRequest("Section name is required.");
            _repo.UpdateSection(Tenant.TenantDbName, Tenant.SchoolId, id, request);
            return Ok(_repo.GetSections(Tenant.TenantDbName, Tenant.SchoolId, request.ClassId));
        }
    }
}
