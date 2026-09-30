using AscentSchools.Core.DTOs.School.MarksGrades;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    /// <summary>
    /// Marks grade master (Master Data → Marks Grade Master) — reusable named grading
    /// scales for a student's TOTAL marks. School-wide; each exam row picks one on
    /// Exam Master. Per-subject bands are Grade Types, not this.
    /// </summary>
    [RoutePrefix("school/marks-grades")]
    public class MarksGradeMasterController : BaseSchoolController
    {
        private const int MaxBands = 50;

        private readonly MarksGradeMasterRepository _repo;

        public MarksGradeMasterController()
        {
            _repo = new MarksGradeMasterRepository(new TenantConnectionFactory());
        }

        // GET school/marks-grades — all scales (with band + exam counts)
        [HttpGet, Route("")]
        public HttpResponseMessage GetScales()
            => Ok(_repo.GetScales(Tenant.TenantDbName, Tenant.SchoolId));

        // GET school/marks-grades/{id}/bands
        [HttpGet, Route("{id:int}/bands")]
        public HttpResponseMessage GetBands(int id)
            => Ok(_repo.GetBands(Tenant.TenantDbName, Tenant.SchoolId, id));

        // POST school/marks-grades — create a scale
        [HttpPost, Route("")]
        public HttpResponseMessage CreateScale([FromBody] SaveMarksGradeScaleRequest request)
        {
            var error = ValidateScale(request);
            if (error != null) return BadRequest(error);
            if (_repo.NameExists(Tenant.TenantDbName, Tenant.SchoolId, request.ScaleName, null))
                return BadRequest($"A scale named '{request.ScaleName}' already exists.");

            var id = _repo.CreateScale(Tenant.TenantDbName, Tenant.SchoolId, Tenant.FullName, request);
            return Ok(new { id }, "Scale created.");
        }

        // PUT school/marks-grades/{id} — rename / describe / activate
        [HttpPut, Route("{id:int}")]
        public HttpResponseMessage UpdateScale(int id, [FromBody] SaveMarksGradeScaleRequest request)
        {
            var error = ValidateScale(request);
            if (error != null) return BadRequest(error);
            if (_repo.NameExists(Tenant.TenantDbName, Tenant.SchoolId, request.ScaleName, id))
                return BadRequest($"A scale named '{request.ScaleName}' already exists.");

            if (_repo.UpdateScale(Tenant.TenantDbName, Tenant.SchoolId, id, request) == 0)
                return NotFound("Scale not found.");
            return Ok(_repo.GetScales(Tenant.TenantDbName, Tenant.SchoolId), "Scale saved.");
        }

        // DELETE school/marks-grades/{id} — refused while exams use it
        [HttpDelete, Route("{id:int}")]
        public HttpResponseMessage DeleteScale(int id)
        {
            var used = _repo.CountExamsUsing(Tenant.TenantDbName, Tenant.SchoolId, id);
            if (used > 0)
                return BadRequest($"This scale is used by {used} exam row(s). Change those exams first, " +
                                  "or set the scale to Inactive instead.");
            if (_repo.DeleteScale(Tenant.TenantDbName, Tenant.SchoolId, id) == 0)
                return NotFound("Scale not found.");
            return Ok(true, "Scale deleted.");
        }

        // PUT school/marks-grades/{id}/bands — replaces the scale's whole band set
        [HttpPut, Route("{id:int}/bands")]
        public HttpResponseMessage SaveBands(int id, [FromBody] SaveMarksGradeBandsRequest request)
        {
            if (request == null)
                return BadRequest("Request body is required.");

            var bands = request.Bands ?? new List<MarksGradeBand>();
            var error = ValidateBands(bands);
            if (error != null) return BadRequest(error);

            _repo.SaveBands(Tenant.TenantDbName, Tenant.SchoolId, Tenant.FullName, id, bands);
            return Ok(_repo.GetBands(Tenant.TenantDbName, Tenant.SchoolId, id),
                bands.Count == 0 ? "Grade bands removed." : "Grade bands saved.");
        }

        private static string ValidateScale(SaveMarksGradeScaleRequest req)
        {
            if (req == null) return "Request body is required.";
            req.ScaleName = req.ScaleName?.Trim();
            if (string.IsNullOrEmpty(req.ScaleName)) return "Scale name is required.";
            if (req.ScaleName.Length > 100)          return "Scale name must be 100 characters or fewer.";
            req.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();
            if (req.Description != null && req.Description.Length > 200)
                return "Description must be 200 characters or fewer.";
            req.Status = string.IsNullOrWhiteSpace(req.Status) ? "Active" : req.Status.Trim();
            return null;
        }

        /// <summary>Returns an error message, or null when the band set is valid.</summary>
        private static string ValidateBands(List<MarksGradeBand> bands)
        {
            if (bands.Count > MaxBands)
                return $"A maximum of {MaxBands} grade bands is allowed.";

            for (int i = 0; i < bands.Count; i++)
            {
                var b = bands[i];
                var row = $"Row {i + 1}";
                if (b == null)                               return $"{row}: band is empty.";
                if (string.IsNullOrWhiteSpace(b.Grade))      return $"{row}: grade is required.";
                if (b.Grade.Trim().Length > 10)              return $"{row}: grade must be 10 characters or fewer.";
                if (b.GradePoint != null && b.GradePoint.Trim().Length > 10)
                                                             return $"{row}: grade point must be 10 characters or fewer.";
                if (b.Description != null && b.Description.Trim().Length > 100)
                                                             return $"{row}: description must be 100 characters or fewer.";
                if (b.MinMarks == null || b.MaxMarks == null) return $"{row}: min and max marks are required.";
                if (b.MinMarks < 0)                          return $"{row}: min marks cannot be negative.";
                if (b.MinMarks > b.MaxMarks)                 return $"{row}: min marks cannot exceed max marks.";
            }

            // Collation is case-insensitive, so 'a' and 'A' would collide in the unique index.
            var dup = bands.GroupBy(b => b.Grade.Trim(), StringComparer.OrdinalIgnoreCase)
                           .FirstOrDefault(g => g.Count() > 1);
            if (dup != null)
                return $"Grade '{dup.Key}' is used more than once.";

            var sorted = bands.OrderBy(b => b.MinMarks).ToList();
            for (int i = 1; i < sorted.Count; i++)
            {
                if (sorted[i].MinMarks <= sorted[i - 1].MaxMarks)
                    return $"Grades '{sorted[i - 1].Grade.Trim()}' ({sorted[i - 1].MinMarks}–{sorted[i - 1].MaxMarks}) and " +
                           $"'{sorted[i].Grade.Trim()}' ({sorted[i].MinMarks}–{sorted[i].MaxMarks}) overlap.";
            }
            return null;
        }
    }
}
