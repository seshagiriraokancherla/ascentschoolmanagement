using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AscentSyncTool.Config;
using AscentSyncTool.Models;
using Newtonsoft.Json;

namespace AscentSyncTool.Api
{
    /// <summary>HTTP client for the new app integration API (X-Api-Key auth).</summary>
    public class AscentApiClient
    {
        private readonly HttpClient _http;

        public AscentApiClient()
        {
            _http = new HttpClient { BaseAddress = new Uri(AppSettings.ApiBaseUrl) };
            _http.DefaultRequestHeaders.Add("X-Api-Key", AppSettings.ApiKey);
            _http.DefaultRequestHeaders.Add("Accept", "application/json");
            _http.Timeout = TimeSpan.FromMinutes(5);
        }

        // ── Receipts list (download) ──────────────────────────────────────────
        public async Task<List<ReceiptListItem>> GetWebappReceiptsAsync(DateTime createdFrom, DateTime createdToInclusive)
        {
            var from = createdFrom.Date.ToString("yyyy-MM-ddTHH:mm:ss");
            var to   = createdToInclusive.Date.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-ddTHH:mm:ss");
            var url  = $"school/fees/receipts?source=webapp&createdAfter={from}&createdBefore={to}";

            var resp = await _http.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();
            var parsed = JsonConvert.DeserializeObject<ApiResponse<List<ReceiptListItem>>>(body);
            if (parsed == null || !parsed.success)
                throw new Exception(Fail(parsed, resp, body, "GET receipts"));
            return parsed.data ?? new List<ReceiptListItem>();
        }

        // ── Receipt detail (download line items) ──────────────────────────────
        public async Task<ReceiptDetail> GetReceiptDetailAsync(int receiptId)
        {
            var resp = await _http.GetAsync($"school/fees/receipts/{receiptId}");
            var body = await resp.Content.ReadAsStringAsync();
            var parsed = JsonConvert.DeserializeObject<ApiResponse<ReceiptDetail>>(body);
            if (parsed == null || !parsed.success)
                throw new Exception(Fail(parsed, resp, body, $"GET receipt {receiptId}"));
            return parsed.data;
        }

        // ── Attendance summary (download) ─────────────────────────────────────
        public async Task<List<AttendanceExportRow>> GetAttendanceSummaryAsync(int month, int year)
        {
            var resp = await _http.GetAsync($"school/attendance/monthly-export?month={month}&year={year}");
            var body = await resp.Content.ReadAsStringAsync();
            var parsed = JsonConvert.DeserializeObject<ApiResponse<List<AttendanceExportRow>>>(body);
            if (parsed == null || !parsed.success)
                throw new Exception(Fail(parsed, resp, body, "GET attendance summary"));
            return parsed.data ?? new List<AttendanceExportRow>();
        }

        // ── Bulk receipt import (export) ──────────────────────────────────────
        public Task<BulkImportResult> BulkImportReceiptsAsync(List<BulkReceiptRow> rows)
            => PostBulkAsync("school/fees/receipts/bulk", new BulkReceiptImportRequest { Rows = rows });

        // ── Bulk student import (export, upsert) ──────────────────────────────
        public Task<BulkImportResult> BulkImportStudentsAsync(List<StudentBulkRow> rows)
            => PostBulkAsync("school/students/bulk", new BulkStudentImportRequest { Rows = rows, Upsert = true });

        private async Task<BulkImportResult> PostBulkAsync(string url, object request)
        {
            var json = JsonConvert.SerializeObject(request);
            var resp = await _http.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
            var body = await resp.Content.ReadAsStringAsync();
            var parsed = JsonConvert.DeserializeObject<ApiResponse<BulkImportResult>>(body);
            if (parsed == null || !parsed.success)
                throw new Exception(Fail(parsed, resp, body, $"POST {url}"));
            return parsed.data ?? new BulkImportResult();
        }

        // The API returns business failures in the ApiResponse envelope, but an UNHANDLED
        // server exception comes back as Web API HttpError -- message is the placeholder
        // "An error has occurred." and the real text is in exceptionMessage. Show whichever
        // is meaningful, plus the HTTP status, so the operator sees the actual reason.
        private static string Fail<T>(ApiResponse<T> parsed, HttpResponseMessage resp, string body, string what)
        {
            var status = $"{what} failed (HTTP {(int)resp.StatusCode} {resp.StatusCode}).";
            var detail = parsed?.ErrorText(null);

            // Body that is not our JSON at all (e.g. an IIS HTML error page) -- show a snippet.
            if (string.IsNullOrWhiteSpace(detail) && !string.IsNullOrWhiteSpace(body))
                detail = body.Length > 500 ? body.Substring(0, 500) + "..." : body;

            return string.IsNullOrWhiteSpace(detail) ? status : status + " " + detail;
        }
    }
}
