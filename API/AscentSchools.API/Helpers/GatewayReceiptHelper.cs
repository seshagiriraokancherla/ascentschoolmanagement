using AscentSchools.Core.DTOs.School.Fee;
using AscentSchools.Data.Repositories.School;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;

namespace AscentSchools.API.Helpers
{
    /// <summary>
    /// The single receipt-creation path for a verified online payment — used by the school
    /// verify callback, the mobile/parent verify callback, the webhooks, the return URL and
    /// the manual reconcile, so all of them produce identical receipts.
    ///
    /// Concurrency: the client callback, the webhook and (Easebuzz) the return URL can arrive
    /// together. The order is claimed atomically (Pending → Processing) first; only the claimer
    /// creates the receipt, everyone else waits briefly and returns that same receipt.
    /// </summary>
    public static class GatewayReceiptHelper
    {
        public static int CreateReceiptForOrder(
            FeeRepository feeRepo, GatewayRepository gatewayRepo,
            string dbName, int schoolId, GatewayOrderRecord order,
            string paymentId, string createdBy, string remarks)
        {
            if (!gatewayRepo.TryClaimOrder(dbName, order.GatewayOrderId))
                return WaitForReceipt(gatewayRepo, dbName, schoolId, order.GatewayOrderId);

            try
            {
                // Reconstruct the collect request from the payload stored at order creation.
                var original = JsonConvert.DeserializeObject<CreatePaymentOrderRequest>(order.PayloadJson);
                var collect  = new CollectFeeRequest
                {
                    StudentId       = order.StudentId,
                    StudentUniqueId = original?.StudentUniqueId,
                    FeeTypeCategory = original?.FeeTypeCategory,
                    AcademicYearId  = order.AcademicYearId,
                    PaymentModeId   = order.PaymentModeId,
                    PaymentDate     = original?.PaymentDate ?? TimeHelper.IstToday(),
                    Remarks         = remarks,
                    Items           = original?.Items ?? new List<CollectFeeItem>(),
                };

                var receiptId = feeRepo.CollectFee(dbName, schoolId, createdBy, collect);
                gatewayRepo.MarkOrderPaid(dbName, order.GatewayOrderId, paymentId, receiptId);
                return receiptId;
            }
            catch
            {
                gatewayRepo.ReleaseClaim(dbName, order.GatewayOrderId);
                throw;
            }
        }

        // Another request claimed the order — give it a few seconds to finish, then return its receipt.
        private static int WaitForReceipt(GatewayRepository gatewayRepo, string dbName, int schoolId, int gatewayOrderId)
        {
            for (var i = 0; i < 16; i++)
            {
                var current = gatewayRepo.GetOrder(dbName, gatewayOrderId, schoolId);
                if (current?.ReceiptId != null) return current.ReceiptId.Value;
                if (current == null || current.Status == "Failed") break;
                Thread.Sleep(500);
            }
            throw new InvalidOperationException(
                "This payment is already being processed. Please refresh in a moment to see the receipt.");
        }
    }
}
