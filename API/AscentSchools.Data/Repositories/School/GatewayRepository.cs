using AscentSchools.Core.DTOs.School.Fee;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System;
using System.Collections.Generic;

namespace AscentSchools.Data.Repositories.School
{
    public class GatewayRepository
    {
        private readonly IConnectionFactory _db;
        public GatewayRepository(IConnectionFactory db) { _db = db; }

        // ── Config queries (school-specific row wins over group-wide NULL row) ──

        /// <summary>Returns gateway info for the frontend (key_id only, no secret).</summary>
        public GatewayConfigDto GetActiveConfig(string tenantDbName, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QueryFirstOrDefault<GatewayConfigDto>(
                    @"SELECT TOP 1
                             gateway_config_id GatewayConfigId,
                             gateway_name      GatewayName,
                             display_name      DisplayName,
                             key_id            KeyId,
                             is_active         IsActive,
                             environment       Environment
                      FROM   gateway_configs
                      WHERE  is_active = 1
                        AND  (school_id = @schoolId OR school_id IS NULL)
                      ORDER BY school_id DESC",
                    new { schoolId });
        }

        /// <summary>
        /// Active gateway credentials incl. key_secret — for server-side gateway calls only.
        /// Never send to a client.
        /// </summary>
        public GatewayCredentials GetCredentials(string tenantDbName, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QueryFirstOrDefault<GatewayCredentials>(
                    @"SELECT TOP 1
                             gateway_name GatewayName,
                             key_id       KeyId,
                             key_secret   KeySecret,
                             environment  Environment
                      FROM   gateway_configs
                      WHERE  is_active = 1
                        AND  (school_id = @schoolId OR school_id IS NULL)
                      ORDER BY school_id DESC",
                    new { schoolId });
        }

        /// <summary>Parent contact details for gateways that require a customer (Easebuzz).</summary>
        public PaymentCustomerDto GetPaymentCustomer(string tenantDbName, int schoolId, long studentId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QueryFirstOrDefault<PaymentCustomerDto>(
                    @"SELECT student_name  StudentName,
                             father_name   FatherName,
                             father_mobile FatherMobile,
                             mother_mobile MotherMobile,
                             email         Email
                      FROM   students
                      WHERE  student_id = @studentId AND school_id = @schoolId",
                    new { studentId, schoolId });
        }

        /// <summary>Returns config for the settings page (no key_secret; hasWebhookSecret flag only).</summary>
        public GatewayConfigSettingsDto GetSettingsConfig(string tenantDbName, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QueryFirstOrDefault<GatewayConfigSettingsDto>(
                    @"SELECT TOP 1
                             gateway_config_id GatewayConfigId,
                             gateway_name      GatewayName,
                             display_name      DisplayName,
                             key_id            KeyId,
                             is_active         IsActive,
                             environment       Environment,
                             CASE WHEN webhook_secret IS NOT NULL AND LEN(webhook_secret) > 0
                                  THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END HasWebhookSecret
                      FROM   gateway_configs
                      WHERE  (school_id = @schoolId OR school_id IS NULL)
                      ORDER BY school_id DESC",
                    new { schoolId });
        }

        /// <summary>Returns raw key_secret for internal gateway calls only. Never send to client.</summary>
        public string GetKeySecret(string tenantDbName, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QueryFirstOrDefault<string>(
                    @"SELECT TOP 1 key_secret
                      FROM   gateway_configs
                      WHERE  is_active = 1
                        AND  (school_id = @schoolId OR school_id IS NULL)
                      ORDER BY school_id DESC",
                    new { schoolId });
        }

        /// <summary>Returns the payment_mode_id for the online payment mode (used by parent portal).</summary>
        public int? GetOnlinePaymentModeId(string tenantDbName, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QueryFirstOrDefault<int?>(
                    @"SELECT TOP 1 payment_mode_id FROM payment_modes
                      WHERE is_online = 1 AND school_id = @schoolId",
                    new { schoolId });
        }

        /// <summary>Returns raw webhook_secret for webhook signature verification only.</summary>
        public string GetWebhookSecret(string tenantDbName, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QueryFirstOrDefault<string>(
                    @"SELECT TOP 1 webhook_secret
                      FROM   gateway_configs
                      WHERE  is_active = 1
                        AND  (school_id = @schoolId OR school_id IS NULL)
                      ORDER BY school_id DESC",
                    new { schoolId });
        }

        /// <summary>Upserts gateway config for the school.</summary>
        public void SaveConfig(string tenantDbName, int schoolId, SaveGatewayConfigRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var existingId = conn.QueryFirstOrDefault<int?>(
                    @"SELECT TOP 1 gateway_config_id FROM gateway_configs
                      WHERE school_id = @schoolId OR school_id IS NULL
                      ORDER BY school_id DESC",
                    new { schoolId });

                if (existingId.HasValue)
                {
                    conn.Execute(
                        @"UPDATE gateway_configs SET
                            gateway_name   = @GatewayName,
                            display_name   = @DisplayName,
                            key_id         = @KeyId,
                            key_secret     = CASE WHEN @KeySecret IS NOT NULL AND LEN(@KeySecret) > 0
                                                  THEN @KeySecret ELSE key_secret END,
                            webhook_secret = CASE WHEN @WebhookSecret IS NOT NULL AND LEN(@WebhookSecret) > 0
                                                  THEN @WebhookSecret ELSE webhook_secret END,
                            is_active      = @IsActive,
                            environment    = @Environment
                          WHERE gateway_config_id = @Id",
                        new
                        {
                            r.GatewayName, r.DisplayName, r.KeyId, r.KeySecret, r.WebhookSecret,
                            IsActive    = r.IsActive ?? true,
                            Environment = NormalizeEnvironment(r.Environment),
                            Id          = existingId.Value
                        });
                }
                else
                {
                    conn.Execute(
                        @"INSERT INTO gateway_configs
                            (gateway_name, display_name, key_id, key_secret, webhook_secret, is_active, school_id, environment)
                          VALUES
                            (@GatewayName, @DisplayName, @KeyId, @KeySecret, @WebhookSecret, @IsActive, @SchoolId, @Environment)",
                        new
                        {
                            r.GatewayName, r.DisplayName, r.KeyId, r.KeySecret, r.WebhookSecret,
                            IsActive    = r.IsActive ?? true,
                            SchoolId    = (int?)schoolId,
                            Environment = NormalizeEnvironment(r.Environment)
                        });
                }
            }
        }

        // Only "test" is stored as-is; anything else means live (NULL).
        private static string NormalizeEnvironment(string env) =>
            string.Equals(env?.Trim(), "test", StringComparison.OrdinalIgnoreCase) ? "test" : null;

        // ── Order lifecycle ───────────────────────────────────────────────────

        public int CreateOrder(
            string tenantDbName,
            int    schoolId,
            string gatewayName,
            string externalOrderId,
            decimal amount,
            long   studentId,
            int    academicYearId,
            int    paymentModeId,
            string payloadJson,
            string createdBy)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QuerySingle<int>(
                    @"INSERT INTO payment_gateway_orders
                        (gateway_name, external_order_id, amount, student_id, academic_year_id,
                         payment_mode_id, payload_json, created_by, status, school_id)
                      VALUES
                        (@gatewayName, @externalOrderId, @amount, @studentId, @academicYearId,
                         @paymentModeId, @payloadJson, @createdBy, 'Pending', @schoolId);
                      SELECT CAST(SCOPE_IDENTITY() AS INT)",
                    new { gatewayName, externalOrderId, amount, studentId, academicYearId,
                          paymentModeId, payloadJson, createdBy, schoolId });
        }

        public GatewayOrderRecord GetOrder(string tenantDbName, int gatewayOrderId, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QueryFirstOrDefault<GatewayOrderRecord>(
                    @"SELECT gateway_order_id GatewayOrderId, gateway_name GatewayName,
                             external_order_id ExternalOrderId, amount Amount,
                             student_id StudentId, academic_year_id AcademicYearId,
                             payment_mode_id PaymentModeId, payload_json PayloadJson,
                             created_by CreatedBy, status Status, receipt_id ReceiptId, school_id SchoolId
                      FROM   payment_gateway_orders
                      WHERE  gateway_order_id = @gatewayOrderId AND school_id = @schoolId",
                    new { gatewayOrderId, schoolId });
        }

        /// <summary>Used by the webhook handler to look up an order by Razorpay's order_id.</summary>
        public GatewayOrderRecord GetOrderByExternalId(string tenantDbName, string externalOrderId, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QueryFirstOrDefault<GatewayOrderRecord>(
                    @"SELECT gateway_order_id GatewayOrderId, gateway_name GatewayName,
                             external_order_id ExternalOrderId, amount Amount,
                             student_id StudentId, academic_year_id AcademicYearId,
                             payment_mode_id PaymentModeId, payload_json PayloadJson,
                             created_by CreatedBy, status Status, receipt_id ReceiptId, school_id SchoolId
                      FROM   payment_gateway_orders
                      WHERE  external_order_id = @externalOrderId AND school_id = @schoolId",
                    new { externalOrderId, schoolId });
        }

        public void MarkOrderPaid(string tenantDbName, int gatewayOrderId, string paymentId, int receiptId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE payment_gateway_orders
                      SET    status = 'Paid', payment_id = @paymentId, receipt_id = @receiptId
                      WHERE  gateway_order_id = @gatewayOrderId",
                    new { paymentId, receiptId, gatewayOrderId });
        }

        /// <summary>
        /// Atomically claims a Pending order for receipt creation (Pending → Processing).
        /// The client callback, the webhook and the return URL can all arrive within the same
        /// second — only the caller that gets true may create the receipt.
        /// </summary>
        public bool TryClaimOrder(string tenantDbName, int gatewayOrderId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Execute(
                    @"UPDATE payment_gateway_orders SET status = 'Processing'
                      WHERE  gateway_order_id = @gatewayOrderId AND status = 'Pending'",
                    new { gatewayOrderId }) == 1;
        }

        /// <summary>Returns a claimed order to Pending when receipt creation failed.</summary>
        public void ReleaseClaim(string tenantDbName, int gatewayOrderId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE payment_gateway_orders SET status = 'Pending'
                      WHERE  gateway_order_id = @gatewayOrderId AND status = 'Processing'",
                    new { gatewayOrderId });
        }

        public void MarkOrderFailed(string tenantDbName, int gatewayOrderId)
        {
            // Never overwrite an order that is Paid or being processed.
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    "UPDATE payment_gateway_orders SET status = 'Failed' WHERE gateway_order_id = @gatewayOrderId AND status = 'Pending'",
                    new { gatewayOrderId });
        }

        /// <summary>
        /// Lists orders still in 'Pending' state (client callback / webhook never completed),
        /// joined to the student for display. Used by the admin reconciliation page.
        /// </summary>
        public List<GatewayOrderListItem> GetPendingOrders(
            string tenantDbName, int schoolId, DateTime? createdAfter, DateTime? createdBefore)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<GatewayOrderListItem>(
                    @"SELECT o.gateway_order_id  GatewayOrderId,
                             o.gateway_name      GatewayName,
                             o.external_order_id ExternalOrderId,
                             o.amount            Amount,
                             o.academic_year_id  AcademicYearId,
                             o.created_by        CreatedBy,
                             o.created_at        CreatedAt,
                             o.payload_json      PayloadJson,
                             o.student_id        StudentId,
                             s.student_name      StudentName,
                             s.admission_no      AdmissionNo
                      FROM   payment_gateway_orders o
                      LEFT  JOIN students s ON s.student_id = o.student_id
                      WHERE  o.status IN ('Pending', 'Processing')
                        AND  o.school_id = @schoolId
                        AND  (@createdAfter  IS NULL OR o.created_at >= @createdAfter)
                        AND  (@createdBefore IS NULL OR o.created_at <= @createdBefore)
                      ORDER BY o.created_at DESC",
                    new { schoolId, createdAfter, createdBefore }).AsList();
        }
    }

    /// <summary>Row shape for the pending-orders reconciliation list (data layer).</summary>
    public class GatewayOrderListItem
    {
        public int      GatewayOrderId  { get; set; }
        public string   GatewayName     { get; set; }
        public string   ExternalOrderId { get; set; }
        public decimal  Amount          { get; set; }
        public int      AcademicYearId  { get; set; }
        public string   CreatedBy       { get; set; }
        public DateTime CreatedAt       { get; set; }
        public string   PayloadJson     { get; set; }
        public long     StudentId       { get; set; }
        public string   StudentName     { get; set; }
        public string   AdmissionNo     { get; set; }
    }

    public class GatewayOrderRecord
    {
        public int     GatewayOrderId  { get; set; }
        public string  GatewayName     { get; set; }
        public string  ExternalOrderId { get; set; }
        public decimal Amount          { get; set; }
        public long    StudentId       { get; set; }
        public int     AcademicYearId  { get; set; }
        public int     PaymentModeId   { get; set; }
        public string  PayloadJson     { get; set; }
        public string  CreatedBy       { get; set; }
        public string  Status          { get; set; }
        public int?    ReceiptId       { get; set; }
        public int     SchoolId        { get; set; }
    }
}
