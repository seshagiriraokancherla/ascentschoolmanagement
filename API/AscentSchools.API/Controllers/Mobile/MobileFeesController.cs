using AscentSchools.API.Filters;
using AscentSchools.API.Helpers;
using AscentSchools.API.Middleware;
using AscentSchools.Core.DTOs.School.Fee;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace AscentSchools.API.Controllers.Mobile
{
    [RoutePrefix("mobile/fees")]
    [MobileAuth(requireChildContext: true)]
    public class MobileFeesController : BaseApiController
    {
        private readonly FeeRepository     _feeRepo;
        private readonly GatewayRepository _gwRepo;
        private readonly StudentRepository _studentRepo;

        public MobileFeesController()
        {
            var cf       = new TenantConnectionFactory();
            _feeRepo     = new FeeRepository(cf);
            _gwRepo      = new GatewayRepository(cf);
            _studentRepo = new StudentRepository(cf);
        }

        // Receipt "Collected by" label. The Android app sends X-Client-App: mobile;
        // the parent web portal (parent.{subdomain}...) sends no such header, so it
        // defaults to "Parent Portal".
        private string ResolveClientSource()
        {
            if (Request.Headers.TryGetValues("X-Client-App", out var vals))
            {
                var v = vals?.FirstOrDefault()?.Trim();
                if (string.Equals(v, "mobile",  StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(v, "android", StringComparison.OrdinalIgnoreCase))
                    return "Mobile App";
            }
            return "Parent Portal";
        }

        // GET /mobile/fees/outstanding?feeTypeCategory=School
        [HttpGet, Route("outstanding")]
        public System.Net.Http.HttpResponseMessage GetOutstanding(string feeTypeCategory = "School")
        {
            var ctx      = MobileContext.Current;
            var uniqueId = _studentRepo.GetStudentUniqueId(ctx.DbName, ctx.SchoolId, ctx.StudentId);
            if (uniqueId == null) return NotFound("Student not found.");

            var summary = _feeRepo.GetCrossYearFeeSummary(ctx.DbName, ctx.SchoolId, uniqueId.Value, feeTypeCategory);
            return Ok(summary);
        }

        // GET /mobile/fees/gateway-config
        [HttpGet, Route("gateway-config")]
        public System.Net.Http.HttpResponseMessage GetGatewayConfig()
        {
            var ctx    = MobileContext.Current;
            var config = _gwRepo.GetActiveConfig(ctx.DbName, ctx.SchoolId);
            if (config == null) return NotFound("No payment gateway configured for this school.");
            return Ok(config);
        }

        // POST /mobile/fees/payment-orders
        [HttpPost, Route("payment-orders")]
        public System.Net.Http.HttpResponseMessage CreateOrder([FromBody] MobileParentOrderRequest request)
        {
            if (request == null || request.AcademicYearId <= 0)
                return BadRequest("Academic year is required.");
            if (request.Items == null || request.Items.Count == 0)
                return BadRequest("At least one fee item must be selected.");

            var ctx = MobileContext.Current;

            var uniqueId = _studentRepo.GetStudentUniqueId(ctx.DbName, ctx.SchoolId, ctx.StudentId);
            if (uniqueId == null) return NotFound("Student not found.");

            var yearStudentId = _studentRepo.GetStudentIdForYear(
                ctx.DbName, ctx.SchoolId, uniqueId.Value, request.AcademicYearId);
            if (yearStudentId == null)
                return BadRequest("Student record not found for the selected academic year.");

            var config = _gwRepo.GetActiveConfig(ctx.DbName, ctx.SchoolId);
            if (config == null) return BadRequest("No payment gateway configured for this school.");

            var keySecret = _gwRepo.GetKeySecret(ctx.DbName, ctx.SchoolId);
            var modeId    = _gwRepo.GetOnlinePaymentModeId(ctx.DbName, ctx.SchoolId);
            if (modeId == null) return BadRequest("No online payment mode configured for this school.");

            decimal total = 0;
            foreach (var item in request.Items)
                total += Math.Max(0, item.Amount - item.ConcessionAmount);
            if (total <= 0) return BadRequest("Total amount must be greater than zero.");

            var notes = new Dictionary<string, string>
            {
                { "group_id",  ctx.GroupId.ToString()  },
                { "school_id", ctx.SchoolId.ToString() },
            };
            var receiptRef  = $"PAR{ctx.SchoolId}-{DateTime.UtcNow:yyyyMMddHHmmss}";
            var gateway     = GatewayServiceFactory.Get(config.GatewayName);
            var orderResult = gateway.CreateExternalOrder(config.KeyId, keySecret, total, receiptRef, notes);
            if (!orderResult.Success)
                return ServerError($"Could not create payment order: {orderResult.Error}");

            // Persist payload using same shape as school-side so verify logic is reusable
            var payloadRequest = new CreatePaymentOrderRequest
            {
                StudentId       = yearStudentId.Value,
                StudentUniqueId = uniqueId,
                FeeTypeCategory = request.FeeTypeCategory,
                AcademicYearId  = request.AcademicYearId,
                PaymentModeId   = modeId.Value,
                PaymentDate     = DateTime.Today,
                Items           = request.Items,
            };

            var gatewayOrderId = _gwRepo.CreateOrder(
                ctx.DbName, ctx.SchoolId,
                config.GatewayName, orderResult.ExternalOrderId,
                total, yearStudentId.Value, request.AcademicYearId,
                modeId.Value, JsonConvert.SerializeObject(payloadRequest),
                ResolveClientSource());

            return Created(new CreatePaymentOrderResponse
            {
                GatewayOrderId  = gatewayOrderId,
                ExternalOrderId = orderResult.ExternalOrderId,
                KeyId           = config.KeyId,
                AmountInPaise   = (long)(total * 100),
                Currency        = "INR",
                GatewayName     = config.GatewayName,
            }, "Payment order created.");
        }

        // POST /mobile/fees/payment-orders/{gatewayOrderId}/verify
        [HttpPost, Route("payment-orders/{gatewayOrderId:int}/verify")]
        public System.Net.Http.HttpResponseMessage VerifyOrder(
            int gatewayOrderId, [FromBody] VerifyPaymentRequest request)
        {
            if (request == null
                || string.IsNullOrWhiteSpace(request.PaymentId)
                || string.IsNullOrWhiteSpace(request.Signature))
                return BadRequest("PaymentId and Signature are required.");

            var ctx   = MobileContext.Current;
            var order = _gwRepo.GetOrder(ctx.DbName, gatewayOrderId, ctx.SchoolId);
            if (order == null) return NotFound("Payment order not found.");

            if (order.Status == "Paid" && order.ReceiptId.HasValue)
            {
                var existing = _feeRepo.GetReceiptById(ctx.DbName, ctx.SchoolId, order.ReceiptId.Value);
                return Ok(existing, "Payment already processed.");
            }
            if (order.Status == "Failed")
                return BadRequest("This payment order was marked as failed. Please try again.");

            var keySecret = _gwRepo.GetKeySecret(ctx.DbName, ctx.SchoolId);
            var gateway   = GatewayServiceFactory.Get(order.GatewayName);

            if (!gateway.VerifyPaymentSignature(
                    order.ExternalOrderId, request.PaymentId, request.Signature, keySecret))
            {
                _gwRepo.MarkOrderFailed(ctx.DbName, gatewayOrderId);
                return BadRequest("Payment signature verification failed.");
            }

            var source   = ResolveClientSource();
            var original = JsonConvert.DeserializeObject<CreatePaymentOrderRequest>(order.PayloadJson);
            var collect  = new CollectFeeRequest
            {
                StudentId       = order.StudentId,
                StudentUniqueId = original?.StudentUniqueId,
                FeeTypeCategory = original?.FeeTypeCategory,
                AcademicYearId  = order.AcademicYearId,
                PaymentModeId   = order.PaymentModeId,
                PaymentDate     = original?.PaymentDate ?? DateTime.UtcNow,
                Remarks         = $"Online ({order.GatewayName}) via {source}. Ref: {request.PaymentId}",
                Items           = original?.Items ?? new List<CollectFeeItem>(),
            };

            var receiptId = _feeRepo.CollectFee(ctx.DbName, ctx.SchoolId, source, collect);
            _gwRepo.MarkOrderPaid(ctx.DbName, gatewayOrderId, request.PaymentId, receiptId);

            var receipt = _feeRepo.GetReceiptById(ctx.DbName, ctx.SchoolId, receiptId);
            return Created(receipt, "Payment verified. Receipt generated.");
        }

        // GET /mobile/fees/receipts/{id}
        // Receipt detail for the in-app print / save-as-PDF view.
        // Validated to belong to the currently selected child (by student_unique_id).
        [HttpGet, Route("receipts/{id:int}")]
        public System.Net.Http.HttpResponseMessage GetReceipt(int id)
        {
            var ctx      = MobileContext.Current;
            var uniqueId = _studentRepo.GetStudentUniqueId(ctx.DbName, ctx.SchoolId, ctx.StudentId);
            if (uniqueId == null) return NotFound("Student not found.");

            if (!_feeRepo.ReceiptBelongsToUniqueId(ctx.DbName, ctx.SchoolId, id, uniqueId.Value))
                return NotFound("Receipt not found.");

            var receipt = _feeRepo.GetReceiptById(ctx.DbName, ctx.SchoolId, id);
            if (receipt == null) return NotFound("Receipt not found.");
            return Ok(receipt);
        }
    }
}
