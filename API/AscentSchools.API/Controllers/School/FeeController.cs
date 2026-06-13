using AscentSchools.API.Helpers;
using AscentSchools.Core.DTOs.School.Fee;
using AscentSchools.Core.DTOs.School.Students;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.School;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.School
{
    [RoutePrefix("school/fees")]
    public class FeeController : BaseSchoolController
    {
        private readonly FeeRepository     _repo;
        private readonly GatewayRepository _gatewayRepo;

        public FeeController()
        {
            var db       = new TenantConnectionFactory();
            _repo        = new FeeRepository(db);
            _gatewayRepo = new GatewayRepository(db);
        }

        // ── Fee Structure ─────────────────────────────────────────────────

        // GET school/fees/structure?classId=&feeCategoryId=&academicYearId=&admissionType=
        [HttpGet, Route("structure")]
        public HttpResponseMessage GetFeeStructure(
            int classId, int feeCategoryId, int academicYearId, string admissionType = null)
        {
            var items = _repo.GetFeeStructure(
                Tenant.TenantDbName, Tenant.SchoolId, classId, feeCategoryId, academicYearId, admissionType);
            return Ok(items);
        }

        // POST school/fees/structure
        [HttpPost, Route("structure")]
        public HttpResponseMessage SaveFeeStructure([FromBody] SaveFeeStructureRequest request)
        {
            if (request == null || !request.ClassId.HasValue || !request.FeeCategoryId.HasValue || !request.AcademicYearId.HasValue)
                return BadRequest("Class, fee category and academic year are required.");

            _repo.SaveFeeStructure(Tenant.TenantDbName, Tenant.SchoolId, request);

            var updated = _repo.GetFeeStructure(
                Tenant.TenantDbName, Tenant.SchoolId,
                request.ClassId.Value, request.FeeCategoryId.Value, request.AcademicYearId.Value,
                request.AdmissionType);
            return Ok(updated, "Fee structure saved.");
        }

        // ── Student Fee Summary ───────────────────────────────────────────

        // GET school/fees/student/{studentId}?academicYearId=
        [HttpGet, Route("student/{studentId:long}")]
        public HttpResponseMessage GetStudentFeeSummary(long studentId, int? academicYearId = null)
        {
            var summary = _repo.GetStudentFeeSummary(
                Tenant.TenantDbName, Tenant.SchoolId, studentId, academicYearId ?? 0);
            if (summary == null) return NotFound("Student not found.");
            return Ok(summary);
        }

        // ── Cross-year fee summary ────────────────────────────────────────

        // GET school/fees/student-unique/{uniqueId}?feeTypeCategory=Transport
        [HttpGet, Route("student-unique/{uniqueId:int}")]
        public HttpResponseMessage GetCrossYearFeeSummary(int uniqueId, string feeTypeCategory = null)
        {
            var summary = _repo.GetCrossYearFeeSummary(
                Tenant.TenantDbName, Tenant.SchoolId, uniqueId, feeTypeCategory);
            if (summary == null) return NotFound("Student not found.");
            return Ok(summary);
        }

        // ── Fee Collection (offline: cash / cheque) ───────────────────────

        // POST school/fees/collect
        [HttpPost, Route("collect")]
        public HttpResponseMessage CollectFee([FromBody] CollectFeeRequest request)
        {
            if (request == null || request.StudentId <= 0)
                return BadRequest("Student ID is required.");
            if (request.Items == null || request.Items.Count == 0)
                return BadRequest("At least one fee item must be selected.");
            if (!request.PaymentModeId.HasValue)
                return BadRequest("Payment mode is required.");

            var receiptId = _repo.CollectFee(
                Tenant.TenantDbName, Tenant.SchoolId, Tenant.FullName, request);
            var receipt = _repo.GetReceiptById(Tenant.TenantDbName, Tenant.SchoolId, receiptId);
            return Created(receipt, "Fee collected. Receipt generated.");
        }

        // ── Receipts ──────────────────────────────────────────────────────

        // GET school/fees/receipts?search=&dateFrom=&dateTo=&status=&createdAfter=&createdBefore=&source=
        [HttpGet, Route("receipts")]
        public HttpResponseMessage GetReceipts(
            string    search        = null,
            DateTime? dateFrom      = null,
            DateTime? dateTo        = null,
            string    status        = null,
            DateTime? createdAfter  = null,
            string    source        = null,
            DateTime? createdBefore = null)
        {
            var receipts = _repo.GetReceipts(
                Tenant.TenantDbName, Tenant.SchoolId, search, dateFrom, dateTo, status, createdAfter, source, createdBefore);
            return Ok(receipts);
        }

        // GET school/fees/receipts/{id}
        [HttpGet, Route("receipts/{id:int}")]
        public HttpResponseMessage GetReceipt(int id)
        {
            var receipt = _repo.GetReceiptById(Tenant.TenantDbName, Tenant.SchoolId, id);
            if (receipt == null) return NotFound("Receipt not found.");
            return Ok(receipt);
        }

        // PUT school/fees/receipts/{id}/cancel
        [HttpPut, Route("receipts/{id:int}/cancel")]
        public HttpResponseMessage CancelReceipt(int id, [FromBody] CancelReceiptRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.CancelReason))
                return BadRequest("Cancel reason is required.");

            _repo.CancelReceipt(
                Tenant.TenantDbName, Tenant.SchoolId, id, Tenant.FullName, request.CancelReason);
            return Ok<object>(null, "Receipt cancelled.");
        }

        // ── Payment Gateway Settings ──────────────────────────────────────

        // GET school/fees/gateway-config  (frontend: get keyId to open checkout)
        [HttpGet, Route("gateway-config")]
        public HttpResponseMessage GetGatewayConfig()
        {
            var config = _gatewayRepo.GetActiveConfig(Tenant.TenantDbName, Tenant.SchoolId);
            return Ok(config);
        }

        // GET school/fees/gateway-settings  (settings page: show current config)
        [HttpGet, Route("gateway-settings")]
        public HttpResponseMessage GetGatewaySettings()
        {
            var config = _gatewayRepo.GetSettingsConfig(Tenant.TenantDbName, Tenant.SchoolId);
            return Ok(config);
        }

        // PUT school/fees/gateway-settings
        [HttpPut, Route("gateway-settings")]
        public HttpResponseMessage SaveGatewaySettings([FromBody] SaveGatewayConfigRequest request)
        {
            if (request == null)
                return BadRequest("Request body is required.");
            if (string.IsNullOrWhiteSpace(request.GatewayName))
                return BadRequest("Gateway name is required.");
            if (string.IsNullOrWhiteSpace(request.KeyId))
                return BadRequest("Key ID is required.");

            _gatewayRepo.SaveConfig(Tenant.TenantDbName, Tenant.SchoolId, request);
            var updated = _gatewayRepo.GetSettingsConfig(Tenant.TenantDbName, Tenant.SchoolId);
            return Ok(updated, "Gateway settings saved.");
        }

        // ── Online Payment: create order ──────────────────────────────────

        // POST school/fees/payment-orders
        [HttpPost, Route("payment-orders")]
        public HttpResponseMessage CreatePaymentOrder([FromBody] CreatePaymentOrderRequest request)
        {
            if (request == null || request.StudentId <= 0)
                return BadRequest("Student ID is required.");
            if (request.Items == null || request.Items.Count == 0)
                return BadRequest("At least one fee item is required.");

            // Resolve active gateway for this school
            var config = _gatewayRepo.GetActiveConfig(Tenant.TenantDbName, Tenant.SchoolId);
            if (config == null)
                return BadRequest("No active payment gateway configured. Go to Settings → Payment Gateway to set up Razorpay.");

            var keySecret = _gatewayRepo.GetKeySecret(Tenant.TenantDbName, Tenant.SchoolId);

            // Compute total (amount − concession per line)
            decimal total = 0;
            foreach (var item in request.Items)
                total += Math.Max(0, item.Amount - item.ConcessionAmount);

            if (total <= 0)
                return BadRequest("Total amount must be greater than zero.");

            // Notes embedded in the Razorpay order for webhook identification
            var notes = new Dictionary<string, string>
            {
                { "group_id",  Tenant.GroupId.ToString() },
                { "school_id", Tenant.SchoolId.ToString() },
            };

            var receiptRef  = $"SCH{Tenant.SchoolId}-{DateTime.UtcNow:yyyyMMddHHmmss}";
            var gateway     = GatewayServiceFactory.Get(config.GatewayName);
            var orderResult = gateway.CreateExternalOrder(config.KeyId, keySecret, total, receiptRef, notes);

            if (!orderResult.Success)
                return ServerError($"Could not create payment order: {orderResult.Error}");

            // Persist the full request so we can recreate the receipt on verify / webhook
            var payloadJson = JsonConvert.SerializeObject(request);

            var gatewayOrderId = _gatewayRepo.CreateOrder(
                Tenant.TenantDbName, Tenant.SchoolId,
                config.GatewayName, orderResult.ExternalOrderId,
                total, request.StudentId, request.AcademicYearId,
                request.PaymentModeId, payloadJson, Tenant.FullName);

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

        // ── Online Payment: verify (JS callback) ─────────────────────────

        // POST school/fees/payment-orders/{gatewayOrderId}/verify
        [HttpPost, Route("payment-orders/{gatewayOrderId:int}/verify")]
        public HttpResponseMessage VerifyPayment(int gatewayOrderId, [FromBody] VerifyPaymentRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PaymentId)
                                || string.IsNullOrWhiteSpace(request.Signature))
                return BadRequest("PaymentId and Signature are required.");

            var order = _gatewayRepo.GetOrder(Tenant.TenantDbName, gatewayOrderId, Tenant.SchoolId);
            if (order == null)
                return NotFound("Payment order not found.");

            // Idempotent: already processed (e.g. verify called twice)
            if (order.Status == "Paid" && order.ReceiptId.HasValue)
            {
                var existingReceipt = _repo.GetReceiptById(
                    Tenant.TenantDbName, Tenant.SchoolId, order.ReceiptId.Value);
                return Ok(existingReceipt, "Payment already processed.");
            }

            if (order.Status == "Failed")
                return BadRequest("This payment order was marked as failed.");

            // Verify HMAC signature — prevents fraudulent payment claims
            var keySecret = _gatewayRepo.GetKeySecret(Tenant.TenantDbName, Tenant.SchoolId);
            var gateway   = GatewayServiceFactory.Get(order.GatewayName);

            if (!gateway.VerifyPaymentSignature(order.ExternalOrderId, request.PaymentId, request.Signature, keySecret))
            {
                _gatewayRepo.MarkOrderFailed(Tenant.TenantDbName, gatewayOrderId);
                return BadRequest("Payment signature verification failed. The order has been marked as failed. Please try again.");
            }

            // Reconstruct collect request from stored payload
            var originalRequest = JsonConvert.DeserializeObject<CreatePaymentOrderRequest>(order.PayloadJson);

            var collectRequest = new CollectFeeRequest
            {
                StudentId        = order.StudentId,
                StudentUniqueId  = originalRequest?.StudentUniqueId,
                FeeTypeCategory  = originalRequest?.FeeTypeCategory,
                AcademicYearId   = order.AcademicYearId,
                PaymentModeId    = order.PaymentModeId,
                PaymentDate      = originalRequest?.PaymentDate ?? DateTime.UtcNow,
                Remarks          = $"Online ({order.GatewayName}). Ref: {request.PaymentId}",
                Items            = originalRequest?.Items ?? new List<CollectFeeItem>(),
            };

            var receiptId = _repo.CollectFee(
                Tenant.TenantDbName, Tenant.SchoolId, order.CreatedBy, collectRequest);

            _gatewayRepo.MarkOrderPaid(Tenant.TenantDbName, gatewayOrderId, request.PaymentId, receiptId);

            var receipt = _repo.GetReceiptById(Tenant.TenantDbName, Tenant.SchoolId, receiptId);
            return Created(receipt, "Payment verified. Receipt generated.");
        }

        // ── Payment Webhook (Razorpay server → our server) ────────────────
        // AllowAnonymous — Razorpay calls this with no auth token.
        // Signature is verified using the webhook_secret stored in gateway_configs.
        // NOTE: Configure this URL in Razorpay Dashboard → Settings → Webhooks:
        //       https://{your-domain}/school/fees/payment-webhook
        //       Events to subscribe: payment.captured

        [HttpPost, Route("payment-webhook"), AllowAnonymous]
        public HttpResponseMessage PaymentWebhook()
        {
            var rawBody   = Request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var sigHeader = Request.Headers.TryGetValues("X-Razorpay-Signature", out var vs)
                            ? vs?.FirstOrDefault() : null;

            if (string.IsNullOrWhiteSpace(rawBody))
                return Request.CreateResponse(HttpStatusCode.OK);

            JObject payload;
            try { payload = JObject.Parse(rawBody); }
            catch { return Request.CreateResponse(HttpStatusCode.OK); }

            var eventType       = payload["event"]?.ToString();
            var externalOrderId = payload["payload"]?["payment"]?["entity"]?["order_id"]?.ToString();
            var paymentId       = payload["payload"]?["payment"]?["entity"]?["id"]?.ToString();
            var notes           = payload["payload"]?["payment"]?["entity"]?["notes"];

            // Only process captured payments
            if (eventType != "payment.captured" || string.IsNullOrWhiteSpace(externalOrderId))
                return Request.CreateResponse(HttpStatusCode.OK);

            // Identify tenant from notes embedded when order was created
            if (!int.TryParse(notes?["group_id"]?.ToString(),  out var groupId)  ||
                !int.TryParse(notes?["school_id"]?.ToString(), out var schoolId))
                return Request.CreateResponse(HttpStatusCode.OK);

            var dbName = $"ascent_group_{groupId}";

            // Verify webhook signature (optional but strongly recommended)
            var webhookSecret = _gatewayRepo.GetWebhookSecret(dbName, schoolId);
            if (!string.IsNullOrWhiteSpace(webhookSecret) && !string.IsNullOrWhiteSpace(sigHeader))
            {
                var gwSvc = GatewayServiceFactory.Get("Razorpay");
                if (!gwSvc.VerifyWebhookSignature(rawBody, sigHeader, webhookSecret))
                    return Request.CreateResponse(HttpStatusCode.Unauthorized);
            }

            // Look up the pending order
            var order = _gatewayRepo.GetOrderByExternalId(dbName, externalOrderId, schoolId);
            if (order == null || order.Status != "Pending")
                return Request.CreateResponse(HttpStatusCode.OK);   // already processed or unknown

            try
            {
                var originalRequest = JsonConvert.DeserializeObject<CreatePaymentOrderRequest>(order.PayloadJson);

                var collectRequest = new CollectFeeRequest
                {
                    StudentId        = order.StudentId,
                    StudentUniqueId  = originalRequest?.StudentUniqueId,
                    FeeTypeCategory  = originalRequest?.FeeTypeCategory,
                    AcademicYearId   = order.AcademicYearId,
                    PaymentModeId    = order.PaymentModeId,
                    PaymentDate      = originalRequest?.PaymentDate ?? DateTime.UtcNow,
                    Remarks          = $"Online ({order.GatewayName}) via webhook. Ref: {paymentId}",
                    Items            = originalRequest?.Items ?? new List<CollectFeeItem>(),
                };

                var receiptId = _repo.CollectFee(dbName, schoolId, order.CreatedBy, collectRequest);
                _gatewayRepo.MarkOrderPaid(dbName, order.GatewayOrderId, paymentId, receiptId);
            }
            catch
            {
                // Swallow — return 200 so Razorpay doesn't keep retrying.
                // Admin can reconcile via the payment_gateway_orders table.
            }

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        // POST school/fees/structure/bulk
        [HttpPost, Route("structure/bulk")]
        public HttpResponseMessage BulkImportFeeStructure([FromBody] BulkFeeStructureImportRequest request)
        {
            if (request == null || request.Rows == null || request.Rows.Count == 0)
                return BadRequest("No rows provided.");
            if (request.Rows.Count > 500)
                return BadRequest("Maximum 500 rows per upload.");

            for (int i = 0; i < request.Rows.Count; i++)
                request.Rows[i].RowNumber = i + 2;

            var result = _repo.BulkSaveFeeStructure(Tenant.TenantDbName, Tenant.SchoolId, request);
            return Ok(result);
        }

        // POST school/fees/receipts/bulk  — legacy receipt upload
        [HttpPost, Route("receipts/bulk")]
        public HttpResponseMessage BulkImportReceipts([FromBody] BulkReceiptImportRequest request)
        {
            if (request == null || request.Rows == null || request.Rows.Count == 0)
                return BadRequest("No rows provided.");
            if (request.Rows.Count > 1000)
                return BadRequest("Maximum 1000 rows per upload.");

            for (int i = 0; i < request.Rows.Count; i++)
                request.Rows[i].RowNumber = i + 2;

            var result = _repo.BulkImportReceipts(
                Tenant.TenantDbName, Tenant.SchoolId, Tenant.FullName, request);
            return Ok(result);
        }
    }
}
