using AscentSchools.API.Filters;
using AscentSchools.API.Helpers;
using AscentSchools.API.Middleware;
using AscentSchools.Core.DTOs.Mobile.Data;
using AscentSchools.Core.DTOs.School.Fee;
using AscentSchools.Core.Models;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.Mobile;
using AscentSchools.Data.Repositories.School;
using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.Mobile
{
    /// <summary>
    /// Data endpoints for mobile app.
    /// Requires tokenType=student OR tokenType=parent (with child context).
    /// </summary>
    [RoutePrefix("mobile/student")]
    [MobileAuth(requireChildContext: true)]
    public class MobileStudentController : ApiController
    {
        private readonly MobileDataRepository _data;
        private readonly FeeRepository        _feeRepo;
        private readonly GatewayRepository    _gwRepo;
        private readonly CalendarRepository   _calendar;

        public MobileStudentController()
        {
            var factory = new TenantConnectionFactory();
            _data     = new MobileDataRepository(factory);
            _feeRepo  = new FeeRepository(factory);
            _gwRepo   = new GatewayRepository(factory);
            _calendar = new CalendarRepository(factory);
        }

        private MobileContext Mobile => MobileContext.Current;

        // GET mobile/student/profile
        [HttpGet, Route("profile")]
        public HttpResponseMessage GetProfile()
        {
            var profile = _data.GetStudentProfile(Mobile.DbName, Mobile.StudentId);
            if (profile == null)
                return Request.CreateResponse(HttpStatusCode.NotFound, ApiResponse.Fail("Student not found."));

            // Serve photo at absolute URL if relative path
            if (!string.IsNullOrEmpty(profile.PhotoPath) && profile.PhotoPath.StartsWith("/"))
            {
                var apiBase = System.Configuration.ConfigurationManager.AppSettings["ApiBaseUrl"] ?? "";
                profile.PhotoPath = apiBase + profile.PhotoPath;
            }

            return Request.CreateResponse(HttpStatusCode.OK, ApiResponse<object>.Ok(profile));
        }

        // GET mobile/student/attendance?month=4&year=2025
        [HttpGet, Route("attendance")]
        public HttpResponseMessage GetAttendance([FromUri] int month = 0, [FromUri] int year = 0)
        {
            if (month <= 0 || month > 12) month = System.DateTime.Today.Month;
            if (year  <= 0)              year  = System.DateTime.Today.Year;

            var summary = _data.GetAttendance(Mobile.DbName, Mobile.StudentId, month, year);
            return Request.CreateResponse(HttpStatusCode.OK, ApiResponse<object>.Ok(summary));
        }

        // GET mobile/student/marks?academicYearId=1
        [HttpGet, Route("marks")]
        public HttpResponseMessage GetMarks([FromUri] int academicYearId = 0)
        {
            if (academicYearId <= 0)
            {
                using (var conn = new TenantConnectionFactory().GetTenantConnection(Mobile.DbName))
                    academicYearId = Dapper.SqlMapper.QueryFirstOrDefault<int>(conn,
                        "SELECT TOP 1 academic_year_id FROM academic_years ORDER BY academic_year_id DESC");
            }
            if (academicYearId <= 0)
                return Request.CreateResponse(HttpStatusCode.OK, ApiResponse<object>.Ok(new object[0]));

            var marks = _data.GetMarks(Mobile.DbName, Mobile.StudentId, academicYearId);
            return Request.CreateResponse(HttpStatusCode.OK, ApiResponse<object>.Ok(marks));
        }

        // GET mobile/student/homework
        [HttpGet, Route("homework")]
        public HttpResponseMessage GetHomework()
        {
            if (string.IsNullOrEmpty(Mobile.ClassName))
                return Request.CreateResponse(HttpStatusCode.OK, ApiResponse<object>.Ok(new object[0]));

            // Resolve classId from className
            int classId = GetClassId(Mobile.DbName, Mobile.ClassName, Mobile.SchoolId);
            if (classId == 0)
                return Request.CreateResponse(HttpStatusCode.OK, ApiResponse<object>.Ok(new object[0]));

            int? sectionId = GetStudentSectionId(Mobile.DbName, Mobile.StudentId);
            var homework = _data.GetHomework(Mobile.DbName, classId, sectionId, Mobile.SchoolId);

            // Convert relative file paths to absolute
            var apiBase = System.Configuration.ConfigurationManager.AppSettings["ApiBaseUrl"] ?? "";
            foreach (var hw in homework)
                foreach (var att in hw.Attachments)
                    if (!string.IsNullOrEmpty(att.FilePath) && att.FilePath.StartsWith("/"))
                        att.FilePath = apiBase + att.FilePath;

            return Request.CreateResponse(HttpStatusCode.OK, ApiResponse<object>.Ok(homework));
        }

        // GET mobile/student/announcements
        [HttpGet, Route("announcements")]
        public HttpResponseMessage GetAnnouncements()
        {
            int? classId = null;
            if (!string.IsNullOrEmpty(Mobile.ClassName))
                classId = GetClassIdNullable(Mobile.DbName, Mobile.ClassName, Mobile.SchoolId);

            // Resolve the child's section so section-targeted announcements only reach
            // matching-section parents; class-wide (section_id NULL) always reach everyone.
            int? sectionId = GetStudentSectionId(Mobile.DbName, Mobile.StudentId);

            var announcements = _data.GetAnnouncements(Mobile.DbName, Mobile.SchoolId, classId, sectionId);
            return Request.CreateResponse(HttpStatusCode.OK, ApiResponse<object>.Ok(announcements));
        }

        // GET mobile/student/events
        [HttpGet, Route("events")]
        public HttpResponseMessage GetEvents()
        {
            int? classId = null;
            if (!string.IsNullOrEmpty(Mobile.ClassName))
                classId = GetClassIdNullable(Mobile.DbName, Mobile.ClassName, Mobile.SchoolId);

            var events = _data.GetEvents(Mobile.DbName, Mobile.SchoolId, classId);
            return Request.CreateResponse(HttpStatusCode.OK, ApiResponse<object>.Ok(events));
        }

        // GET mobile/student/calendar?month=7&year=2026 — the month's holidays/exams/events
        // (school-wide). Defaults to the current IST month when not supplied.
        [HttpGet, Route("calendar")]
        public HttpResponseMessage GetCalendar([FromUri] int month = 0, [FromUri] int year = 0)
        {
            var istToday = TimeHelper.IstToday();
            if (month <= 0 || month > 12) month = istToday.Month;
            if (year  <= 0)              year  = istToday.Year;

            var events = _calendar.GetEvents(Mobile.DbName, Mobile.SchoolId, month, year, null);
            return Request.CreateResponse(HttpStatusCode.OK, ApiResponse<object>.Ok(events));
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private int GetClassId(string dbName, string className, int schoolId)
        {
            using (var conn = new TenantConnectionFactory().GetTenantConnection(dbName))
                return Dapper.SqlMapper.QueryFirstOrDefault<int>(conn,
                    "SELECT class_id FROM classes WHERE class_name = @className AND school_id = @schoolId",
                    new { className, schoolId });
        }

        private int? GetClassIdNullable(string dbName, string className, int schoolId)
        {
            using (var conn = new TenantConnectionFactory().GetTenantConnection(dbName))
                return Dapper.SqlMapper.QueryFirstOrDefault<int?>(conn,
                    "SELECT class_id FROM classes WHERE class_name = @className AND school_id = @schoolId",
                    new { className, schoolId });
        }

        private int? GetStudentSectionId(string dbName, long studentId)
        {
            using (var conn = new TenantConnectionFactory().GetTenantConnection(dbName))
                return Dapper.SqlMapper.QueryFirstOrDefault<int?>(conn,
                    "SELECT section_id FROM students WHERE student_id = @studentId",
                    new { studentId });
        }

        // ── Fee Endpoints ─────────────────────────────────────────────────────

        // GET mobile/student/fees?academicYearId=0
        [HttpGet, Route("fees")]
        public HttpResponseMessage GetFees([FromUri] int academicYearId = 0)
        {
            var summary = _feeRepo.GetStudentFeeSummary(
                Mobile.DbName, Mobile.SchoolId, Mobile.StudentId, academicYearId);

            if (summary == null)
                return Request.CreateResponse(HttpStatusCode.NotFound, ApiResponse.Fail("Fee data not found for student."));

            var lineItems = (summary.LineItems ?? new List<FeeLineItemDto>())
                .Select(li => new MobileFeeLineItemDto
                {
                    FeeTypeId   = li.FeeTypeId,
                    FeeTypeName = li.FeeTypeName,
                    TermId      = li.TermId,
                    TermName    = li.TermName,
                    Amount      = li.StructureAmount,
                    PaidAmount  = li.PaidAmount,
                    Outstanding = li.Outstanding,
                    IsPaid      = li.Outstanding <= 0
                }).ToList();

            var dto = new MobileFeeSummaryDto
            {
                AcademicYearId    = summary.AcademicYearId,
                AcademicYear      = summary.AcademicYear,
                TotalAmount       = lineItems.Sum(l => l.Amount),
                PaidAmount        = lineItems.Sum(l => l.PaidAmount),
                OutstandingAmount = lineItems.Sum(l => l.Outstanding < 0 ? 0 : l.Outstanding),
                LineItems         = lineItems
            };

            return Request.CreateResponse(HttpStatusCode.OK, ApiResponse<object>.Ok(dto));
        }

        // POST mobile/student/fees/order
        [HttpPost, Route("fees/order")]
        public HttpResponseMessage CreateFeeOrder([FromBody] MobileCreateOrderRequest request)
        {
            if (request == null || request.Items == null || request.Items.Count == 0)
                return Request.CreateResponse(HttpStatusCode.BadRequest, ApiResponse.Fail("At least one fee item is required."));

            var config = _gwRepo.GetActiveConfig(Mobile.DbName, Mobile.SchoolId);
            if (config == null)
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                    ApiResponse.Fail("Online payment is not configured for this school."));

            var keySecret = _gwRepo.GetKeySecret(Mobile.DbName, Mobile.SchoolId);
            decimal total = request.Items.Sum(i => Math.Max(0, i.Amount - i.ConcessionAmount));

            if (total <= 0)
                return Request.CreateResponse(HttpStatusCode.BadRequest, ApiResponse.Fail("Total amount must be greater than zero."));

            var notes = new Dictionary<string, string>
            {
                { "group_id",  Mobile.GroupId.ToString() },
                { "school_id", Mobile.SchoolId.ToString() },
            };
            var receiptRef  = $"MOB{Mobile.SchoolId}-{DateTime.UtcNow:yyyyMMddHHmmss}";
            var gateway     = GatewayServiceFactory.Get(config.GatewayName);
            var orderResult = gateway.CreateExternalOrder(config.KeyId, keySecret, total, receiptRef, notes);

            if (!orderResult.Success)
                return Request.CreateResponse(HttpStatusCode.InternalServerError,
                    ApiResponse.Fail($"Could not create payment order: {orderResult.Error}"));

            var payloadJson = JsonConvert.SerializeObject(new CreatePaymentOrderRequest
            {
                StudentId      = Mobile.StudentId,
                AcademicYearId = request.AcademicYearId,
                PaymentModeId  = request.PaymentModeId,
                PaymentDate    = DateTime.UtcNow,
                Remarks        = $"Mobile payment by {Mobile.DisplayName}",
                Items          = request.Items.Select(i => new CollectFeeItem
                {
                    FeeTypeId        = i.FeeTypeId,
                    TermId           = i.TermId,
                    Amount           = i.Amount,
                    ConcessionAmount = i.ConcessionAmount
                }).ToList()
            });

            var gatewayOrderId = _gwRepo.CreateOrder(
                Mobile.DbName, Mobile.SchoolId,
                config.GatewayName, orderResult.ExternalOrderId,
                total, Mobile.StudentId, request.AcademicYearId,
                request.PaymentModeId, payloadJson, Mobile.DisplayName ?? "Mobile");

            var response = new MobileOrderResponse
            {
                GatewayOrderId  = gatewayOrderId,
                ExternalOrderId = orderResult.ExternalOrderId,
                KeyId           = config.KeyId,
                AmountInPaise   = (long)(total * 100),
                Currency        = "INR"
            };

            return Request.CreateResponse(HttpStatusCode.OK, ApiResponse<object>.Ok(response));
        }

        // POST mobile/student/fees/verify
        [HttpPost, Route("fees/verify")]
        public HttpResponseMessage VerifyFeePayment([FromBody] MobileVerifyRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PaymentId)
                                || string.IsNullOrWhiteSpace(request.Signature))
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                    ApiResponse.Fail("PaymentId and Signature are required."));

            var order = _gwRepo.GetOrder(Mobile.DbName, request.GatewayOrderId, Mobile.SchoolId);
            if (order == null)
                return Request.CreateResponse(HttpStatusCode.NotFound, ApiResponse.Fail("Payment order not found."));

            if (order.Status == "Paid" && order.ReceiptId.HasValue)
            {
                var existingReceipt = _feeRepo.GetReceiptById(Mobile.DbName, Mobile.SchoolId, order.ReceiptId.Value);
                return Request.CreateResponse(HttpStatusCode.OK, ApiResponse<object>.Ok(new MobilePaymentResultDto
                {
                    ReceiptId = order.ReceiptId.Value,
                    ReceiptNo = existingReceipt?.ReceiptNo,
                    Amount    = existingReceipt?.TotalAmount ?? 0,
                    Message   = "Payment already processed."
                }));
            }

            if (order.Status == "Failed")
                return Request.CreateResponse(HttpStatusCode.BadRequest, ApiResponse.Fail("This payment order was marked as failed."));

            var keySecret = _gwRepo.GetKeySecret(Mobile.DbName, Mobile.SchoolId);
            var gateway   = GatewayServiceFactory.Get(order.GatewayName);

            if (!gateway.VerifyPaymentSignature(order.ExternalOrderId, request.PaymentId, request.Signature, keySecret))
            {
                _gwRepo.MarkOrderFailed(Mobile.DbName, request.GatewayOrderId);
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                    ApiResponse.Fail("Payment signature verification failed."));
            }

            var originalRequest = JsonConvert.DeserializeObject<CreatePaymentOrderRequest>(order.PayloadJson);
            var collectRequest = new CollectFeeRequest
            {
                StudentId      = order.StudentId,
                AcademicYearId = order.AcademicYearId,
                PaymentModeId  = order.PaymentModeId,
                PaymentDate    = DateTime.UtcNow,
                Remarks        = $"Mobile payment. Ref: {request.PaymentId}",
                Items          = originalRequest?.Items ?? new List<CollectFeeItem>()
            };

            var receiptId = _feeRepo.CollectFee(Mobile.DbName, Mobile.SchoolId, Mobile.DisplayName ?? "Mobile", collectRequest);
            _gwRepo.MarkOrderPaid(Mobile.DbName, request.GatewayOrderId, request.PaymentId, receiptId);

            var receipt = _feeRepo.GetReceiptById(Mobile.DbName, Mobile.SchoolId, receiptId);
            var result = new MobilePaymentResultDto
            {
                ReceiptId = receiptId,
                ReceiptNo = receipt?.ReceiptNo,
                Amount    = receipt?.TotalAmount ?? 0,
                Message   = "Payment successful. Receipt generated."
            };

            return Request.CreateResponse(HttpStatusCode.OK, ApiResponse<object>.Ok(result));
        }
    }
}
