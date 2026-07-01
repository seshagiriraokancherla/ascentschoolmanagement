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

            var receiptId = ReconcileOrderToReceipt(
                Tenant.TenantDbName, Tenant.SchoolId, order, request.PaymentId, "");

            var receipt = _repo.GetReceiptById(Tenant.TenantDbName, Tenant.SchoolId, receiptId);
            return Created(receipt, "Payment verified. Receipt generated.");
        }

        // Shared receipt-creation path used by the verify callback, the webhook, and the
        // manual reconcile endpoint — keeps all three producing identical receipts.
        // Reconstructs the collect request from the stored payload, creates the receipt,
        // and marks the order Paid. viaLabel distinguishes the source in the receipt remarks
        // (e.g. " via webhook", " via reconcile"; "" for the direct callback).
        private int ReconcileOrderToReceipt(
            string dbName, int schoolId, GatewayOrderRecord order, string paymentId, string viaLabel)
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
                Remarks          = $"Online ({order.GatewayName}){viaLabel}. Ref: {paymentId}",
                Items            = originalRequest?.Items ?? new List<CollectFeeItem>(),
            };

            var receiptId = _repo.CollectFee(dbName, schoolId, order.CreatedBy, collectRequest);
            _gatewayRepo.MarkOrderPaid(dbName, order.GatewayOrderId, paymentId, receiptId);
            return receiptId;
        }

        // ── Reconciliation: list pending orders, check live status, reconcile ──
        // Lets an admin recover payments where the parent paid but left the app before
        // the success callback ran (and no webhook is configured). Uses the gateway API
        // keys already stored in gateway_configs — no Razorpay dashboard access needed.

        // GET school/fees/payment-orders/pending?createdAfter=&createdBefore=
        [HttpGet, Route("payment-orders/pending")]
        public HttpResponseMessage GetPendingPaymentOrders(
            DateTime? createdAfter = null, DateTime? createdBefore = null)
        {
            var orders = _gatewayRepo.GetPendingOrders(
                Tenant.TenantDbName, Tenant.SchoolId, createdAfter, createdBefore);

            var result = orders.Select(o =>
            {
                string category = null;
                try { category = JsonConvert.DeserializeObject<CreatePaymentOrderRequest>(o.PayloadJson)?.FeeTypeCategory; }
                catch { /* malformed payload — leave category null */ }

                return new PendingPaymentOrderDto
                {
                    GatewayOrderId  = o.GatewayOrderId,
                    ExternalOrderId = o.ExternalOrderId,
                    Amount          = o.Amount,
                    AcademicYearId  = o.AcademicYearId,
                    FeeTypeCategory = category,
                    CreatedBy       = o.CreatedBy,
                    CreatedAt       = o.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    StudentId       = o.StudentId,
                    StudentName     = o.StudentName,
                    AdmissionNo     = o.AdmissionNo,
                };
            }).ToList();

            return Ok(result);
        }

        // GET school/fees/payment-orders/{gatewayOrderId}/status
        // Fetches the order's live status from the gateway (Check status button).
        [HttpGet, Route("payment-orders/{gatewayOrderId:int}/status")]
        public HttpResponseMessage GetPaymentOrderStatus(int gatewayOrderId)
        {
            var order = _gatewayRepo.GetOrder(Tenant.TenantDbName, gatewayOrderId, Tenant.SchoolId);
            if (order == null) return NotFound("Payment order not found.");

            var config = _gatewayRepo.GetActiveConfig(Tenant.TenantDbName, Tenant.SchoolId);
            if (config == null) return BadRequest("No active payment gateway configured.");

            var keySecret = _gatewayRepo.GetKeySecret(Tenant.TenantDbName, Tenant.SchoolId);
            var gateway   = GatewayServiceFactory.Get(order.GatewayName);
            var status    = gateway.FetchOrderPayment(config.KeyId, keySecret, order.ExternalOrderId);

            if (!status.Success)
                return ServerError($"Could not fetch payment status from gateway: {status.Error}");

            var (reconcilable, note) = EvaluateStatus(order.Amount, order.Status, status);

            return Ok(new PaymentOrderStatusDto
            {
                GatewayOrderId = order.GatewayOrderId,
                OrderStatus    = order.Status,
                Found          = status.Found,
                GatewayStatus  = status.Status,
                PaymentId      = status.PaymentId,
                Method         = status.Method,
                Amount         = status.AmountInRupees,
                Reconcilable   = reconcilable,
                Note           = note,
            });
        }

        // Decides whether an order can be reconciled and the human-readable reason.
        // Shared by the single-order status check and the bulk scan so both agree.
        private (bool reconcilable, string note) EvaluateStatus(
            decimal orderAmount, string orderDbStatus, OrderPaymentStatus status)
        {
            var amountMatches = Math.Abs(status.AmountInRupees - orderAmount) <= 0.01m;
            var captured      = status.Found && status.Status == "captured";
            var reconcilable  = captured && amountMatches && orderDbStatus != "Paid";

            string note;
            if (!status.Found)                note = "No payment attempt found — the parent did not complete payment.";
            else if (!captured)               note = $"Payment is '{status.Status}', not captured. Not reconcilable.";
            else if (!amountMatches)          note = $"Captured ₹{status.AmountInRupees:0.00} does not match order ₹{orderAmount:0.00}. Reconcile manually.";
            else if (orderDbStatus == "Paid") note = "Already reconciled.";
            else                              note = "Captured — ready to reconcile.";
            return (reconcilable, note);
        }

        // GET school/fees/payment-orders/scan?createdAfter=&createdBefore=&onlyReconcilable=true
        // Checks every pending order against the gateway in one pass and returns the rows
        // (by default only the captured/reconcilable ones) so the admin doesn't have to
        // click "Check status" on each row across many pages.
        [HttpGet, Route("payment-orders/scan")]
        public HttpResponseMessage ScanPendingPaymentOrders(
            DateTime? createdAfter = null, DateTime? createdBefore = null, bool onlyReconcilable = true)
        {
            var config = _gatewayRepo.GetActiveConfig(Tenant.TenantDbName, Tenant.SchoolId);
            if (config == null) return BadRequest("No active payment gateway configured.");

            var keySecret = _gatewayRepo.GetKeySecret(Tenant.TenantDbName, Tenant.SchoolId);
            var orders    = _gatewayRepo.GetPendingOrders(
                Tenant.TenantDbName, Tenant.SchoolId, createdAfter, createdBefore);

            var result = new List<ScannedPaymentOrderDto>();
            foreach (var o in orders)
            {
                var gateway = GatewayServiceFactory.Get(
                    string.IsNullOrWhiteSpace(o.GatewayName) ? config.GatewayName : o.GatewayName);
                var status  = gateway.FetchOrderPayment(config.KeyId, keySecret, o.ExternalOrderId);
                if (!status.Success) continue;   // skip rows we couldn't reach; admin can re-scan / check individually

                var (reconcilable, note) = EvaluateStatus(o.Amount, "Pending", status);
                if (onlyReconcilable && !reconcilable) continue;

                string category = null;
                try { category = JsonConvert.DeserializeObject<CreatePaymentOrderRequest>(o.PayloadJson)?.FeeTypeCategory; }
                catch { /* malformed payload — leave category null */ }

                result.Add(new ScannedPaymentOrderDto
                {
                    GatewayOrderId  = o.GatewayOrderId,
                    ExternalOrderId = o.ExternalOrderId,
                    Amount          = o.Amount,
                    AcademicYearId  = o.AcademicYearId,
                    FeeTypeCategory = category,
                    CreatedBy       = o.CreatedBy,
                    CreatedAt       = o.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    StudentId       = o.StudentId,
                    StudentName     = o.StudentName,
                    AdmissionNo     = o.AdmissionNo,
                    Found           = status.Found,
                    GatewayStatus   = status.Status,
                    PaymentId       = status.PaymentId,
                    Method          = status.Method,
                    GatewayAmount   = status.AmountInRupees,
                    Reconcilable    = reconcilable,
                    Note            = note,
                });
            }

            return Ok(result);
        }

        // POST school/fees/payment-orders/{gatewayOrderId}/reconcile
        // Creates the receipt for a pending order that actually captured at the gateway.
        [HttpPost, Route("payment-orders/{gatewayOrderId:int}/reconcile")]
        public HttpResponseMessage ReconcilePaymentOrder(int gatewayOrderId)
        {
            var order = _gatewayRepo.GetOrder(Tenant.TenantDbName, gatewayOrderId, Tenant.SchoolId);
            if (order == null) return NotFound("Payment order not found.");

            // Idempotent: already processed (e.g. a late webhook landed first).
            if (order.Status == "Paid" && order.ReceiptId.HasValue)
            {
                var existing = _repo.GetReceiptById(Tenant.TenantDbName, Tenant.SchoolId, order.ReceiptId.Value);
                return Ok(existing, "Payment already reconciled.");
            }

            var config = _gatewayRepo.GetActiveConfig(Tenant.TenantDbName, Tenant.SchoolId);
            if (config == null) return BadRequest("No active payment gateway configured.");

            var keySecret = _gatewayRepo.GetKeySecret(Tenant.TenantDbName, Tenant.SchoolId);
            var gateway   = GatewayServiceFactory.Get(order.GatewayName);
            var status    = gateway.FetchOrderPayment(config.KeyId, keySecret, order.ExternalOrderId);

            if (!status.Success)
                return ServerError($"Could not fetch payment status from gateway: {status.Error}");

            if (!status.Found || status.Status != "captured")
                return BadRequest($"No captured payment found for this order (gateway status: {status.Status ?? "none"}). The order remains pending.");

            if (Math.Abs(status.AmountInRupees - order.Amount) > 0.01m)
                return BadRequest($"Captured amount (₹{status.AmountInRupees:0.00}) does not match the order amount (₹{order.Amount:0.00}). Not reconciled — please verify manually.");

            var receiptId = ReconcileOrderToReceipt(
                Tenant.TenantDbName, Tenant.SchoolId, order, status.PaymentId, " via reconcile");

            var receipt = _repo.GetReceiptById(Tenant.TenantDbName, Tenant.SchoolId, receiptId);
            return Created(receipt, "Payment reconciled. Receipt generated.");
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
                ReconcileOrderToReceipt(dbName, schoolId, order, paymentId, " via webhook");
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
