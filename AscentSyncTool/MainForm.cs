using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AscentSyncTool.Api;
using AscentSyncTool.Config;
using AscentSyncTool.Data;
using AscentSyncTool.Models;
using AscentSyncTool.Mappers;

namespace AscentSyncTool
{
    public class MainForm : Form
    {
        private readonly LegacyDb _legacy = new LegacyDb();

        // Lazy — the API client reads ApiKey from config; constructing it eagerly would
        // throw (and silently kill the app on startup) while the key is still unset.
        private AscentApiClient _api;
        private AscentApiClient Api => _api ?? (_api = new AscentApiClient());

        // Last-shown data per tab (used by Save)
        private List<LegacyFeeReceipt> _exportReceipts = new List<LegacyFeeReceipt>();
        private List<LegacyStudent>    _exportStudents = new List<LegacyStudent>();
        private List<ReceiptListItem>  _downloadReceipts = new List<ReceiptListItem>();

        private readonly SyncTab _tabReceipts = new SyncTab();
        private readonly SyncTab _tabStudents = new SyncTab();
        private readonly SyncTab _tabDownload = new SyncTab();

        public MainForm()
        {
            Text = "Ascent Sync Tool";
            Width = 1000;
            Height = 640;
            StartPosition = FormStartPosition.CenterScreen;

            var tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(MakePage("Export Students",   _tabStudents));
            tabs.TabPages.Add(MakePage("Download Receipts", _tabDownload));
            tabs.TabPages.Add(MakePage("Export Receipts",   _tabReceipts));
            
            Controls.Add(tabs);

            WireExportReceipts();
            WireExportStudents();
            WireDownloadReceipts();
        }

        private static TabPage MakePage(string title, Control content)
        {
            var page = new TabPage(title) { Padding = new Padding(4) };
            page.Controls.Add(content);
            return page;
        }

        // ── Tab 1: Export Receipts (legacy → new app) ─────────────────────────
        private void WireExportReceipts()
        {
            _tabReceipts.OnShow = async (from, to) =>
            {
                _exportReceipts = await Task.Run(() => _legacy.GetReceipts(from, to));
                _tabReceipts.Grid.DataSource = _exportReceipts;
                return _exportReceipts.Count;
            };

            _tabReceipts.OnSave = async () =>
            {
                var rows = _exportReceipts.Select(Mappers.Mappers.ToBulkReceipt).ToList();
                var total = new BulkImportResult();
                foreach (var batch in Chunk(rows, AppSettings.ReceiptBatchSize))
                {
                    _tabReceipts.SetStatus($"Uploading… ({total.Imported + total.Skipped + total.Failed}/{rows.Count})");
                    var r = await Api.BulkImportReceiptsAsync(batch);
                    Accumulate(total, r);
                }
                _tabReceipts.SetStatus(
                    $"Done. Imported {total.Imported}, skipped {total.Skipped}, failed {total.Failed}.");
                ShowResult("Export Receipts", total);
            };
        }

        // ── Tab 2: Export Students (legacy → new app, upsert) ─────────────────
        private void WireExportStudents()
        {
            _tabStudents.OnShow = async (from, to) =>
            {
                _exportStudents = await Task.Run(() => _legacy.GetStudents(from, to));
                _tabStudents.Grid.DataSource = _exportStudents;
                return _exportStudents.Count;
            };

            _tabStudents.OnSave = async () =>
            {
                var rows = _exportStudents.Select(Mappers.Mappers.ToBulkStudent).ToList();
                var total = new BulkImportResult();
                foreach (var batch in Chunk(rows, AppSettings.StudentBatchSize))
                {
                    _tabStudents.SetStatus($"Uploading… ({total.Imported + total.Updated + total.Failed}/{rows.Count})");
                    var r = await Api.BulkImportStudentsAsync(batch);
                    Accumulate(total, r);
                }
                _tabStudents.SetStatus(
                    $"Done. Inserted {total.Imported}, updated {total.Updated}, failed {total.Failed}.");
                ShowResult("Export Students", total);
            };
        }

        // ── Tab 3: Download Receipts (new app → legacy) ───────────────────────
        private void WireDownloadReceipts()
        {
            _tabDownload.OnShow = async (from, to) =>
            {
                _downloadReceipts = await Api.GetWebappReceiptsAsync(from, to);
                _tabDownload.Grid.DataSource = _downloadReceipts;
                return _downloadReceipts.Count;
            };

            _tabDownload.OnSave = async () =>
            {
                int inserted = 0, skipped = 0, failed = 0, processed = 0;
                var errors = new List<string>();

                foreach (var head in _downloadReceipts)
                {
                    processed++;
                    _tabDownload.SetStatus($"Saving to legacy… ({processed}/{_downloadReceipts.Count})");
                    try
                    {
                        if (await Task.Run(() => _legacy.ReceiptExists(head.ReceiptNo)))
                        {
                            skipped++;
                            continue;
                        }

                        var detail = await Api.GetReceiptDetailAsync(head.ReceiptId);
                        if (detail?.Items == null || detail.Items.Count == 0)
                        {
                            errors.Add($"{head.ReceiptNo}: no line items");
                            failed++;
                            continue;
                        }

                        await Task.Run(() =>
                        {
                            foreach (var item in detail.Items)
                                _legacy.InsertReceipt(Mappers.Mappers.ToLegacyReceipt(detail, item));
                        });
                        inserted++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{head.ReceiptNo}: {ex.Message}");
                        failed++;
                    }
                }

                _tabDownload.SetStatus(
                    $"Done. Inserted {inserted} receipt(s), skipped {skipped} (already in legacy), failed {failed}.");

                var sb = new StringBuilder();
                sb.AppendLine($"Inserted: {inserted}");
                sb.AppendLine($"Skipped (already exist): {skipped}");
                sb.AppendLine($"Failed: {failed}");
                if (errors.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("Errors:");
                    foreach (var e in errors.Take(20)) sb.AppendLine("  " + e);
                    if (errors.Count > 20) sb.AppendLine($"  …and {errors.Count - 20} more.");
                }
                MessageBox.Show(sb.ToString(), "Download Receipts",
                    MessageBoxButtons.OK, errors.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            };
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static IEnumerable<List<T>> Chunk<T>(List<T> source, int size)
        {
            for (int i = 0; i < source.Count; i += size)
                yield return source.GetRange(i, Math.Min(size, source.Count - i));
        }

        private static void Accumulate(BulkImportResult total, BulkImportResult r)
        {
            total.Total    += r.Total;
            total.Imported += r.Imported;
            total.Updated  += r.Updated;
            total.Skipped  += r.Skipped;
            total.Failed   += r.Failed;
            if (r.Errors != null) total.Errors.AddRange(r.Errors);
        }

        private static void ShowResult(string title, BulkImportResult r)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Total:    {r.Total}");
            sb.AppendLine($"Imported: {r.Imported}");
            if (r.Updated > 0) sb.AppendLine($"Updated:  {r.Updated}");
            if (r.Skipped > 0) sb.AppendLine($"Skipped:  {r.Skipped}");
            sb.AppendLine($"Failed:   {r.Failed}");
            if (r.Errors != null && r.Errors.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Errors:");
                foreach (var e in r.Errors.Take(20))
                    sb.AppendLine($"  row {e.Row} {e.AdmissionNo ?? e.Identifier}: {e.Reason}");
                if (r.Errors.Count > 20) sb.AppendLine($"  …and {r.Errors.Count - 20} more.");
            }
            MessageBox.Show(sb.ToString(), title,
                MessageBoxButtons.OK,
                (r.Failed > 0) ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }
    }
}
