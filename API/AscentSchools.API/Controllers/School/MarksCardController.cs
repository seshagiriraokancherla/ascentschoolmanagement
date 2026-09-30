using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    /// <summary>
    /// Marks card / progress report (Reports → Marks Card). One section + one exam:
    /// subject marks, subject grades, total, total grade and rank within the section.
    /// </summary>
    [RoutePrefix("school/marks-card")]
    public class MarksCardController : BaseSchoolController
    {
        private readonly MarksCardRepository _repo;

        public MarksCardController()
        {
            _repo = new MarksCardRepository(new TenantConnectionFactory());
        }

        // GET school/marks-card?academicYearId=&examTypeId=&classId=&sectionId=
        [HttpGet, Route("")]
        public HttpResponseMessage GetMarksCards([FromUri] int academicYearId, [FromUri] int examTypeId,
            [FromUri] int classId, [FromUri] int sectionId)
        {
            if (academicYearId <= 0 || examTypeId <= 0 || classId <= 0 || sectionId <= 0)
                return BadRequest("academicYearId, examTypeId, classId and sectionId are required.");

            var data = _repo.GetMarksCards(Tenant.TenantDbName, Tenant.SchoolId,
                academicYearId, examTypeId, classId, sectionId);
            if (data == null)
                return BadRequest("The selected section does not belong to this class.");
            return Ok(data);
        }
    }
}
