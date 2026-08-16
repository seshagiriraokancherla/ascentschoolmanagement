using System;
using System.Collections.Generic;
using System.Drawing;
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
        public const string Version = "1.2";

        private readonly LegacyDb _legacy = new LegacyDb();

        // Lazy — the API client reads ApiKey from config; constructing it eagerly would
        // throw (and silently kill the app on startup) while the key is still unset.
        private AscentApiClient _api;
        private AscentApiClient Api => _api ?? (_api = new AscentApiClient());

        // Last-shown data per tab (used by Save)
        private List<LegacyFeeReceipt>   _exportReceipts   = new List<LegacyFeeReceipt>();
        private List<LegacyStudent>      _exportStudents   = new List<LegacyStudent>();
        private List<ReceiptListItem>    _downloadReceipts = new List<ReceiptListItem>();
        private List<AttendanceExportRow> _attendance      = new List<AttendanceExportRow>();
        private DateTime _attendanceMonth;   // first day of the month shown in the grid

        private readonly SyncTab _tabReceipts   = new SyncTab();
        private readonly SyncTab _tabStudents   = new SyncTab();
        private readonly SyncTab _tabDownload   = new SyncTab();
        private readonly SyncTab _tabAttendance = new SyncTab();

        public MainForm()
        {
            Text = "Ascent Sync Tool  v" + Version;
            Width = 1000;
            Height = 640;
            StartPosition = FormStartPosition.CenterScreen;

            var header = new Label
            {
                Text = "Ascent Sync Tool  v" + Version,
                Dock = DockStyle.Top,
                Height = 46,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 51, 102),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0)
            };

            var tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(MakePage("Export Students",   _tabStudents));
            tabs.TabPages.Add(MakePage("Download Receipts", _tabDownload));
            tabs.TabPages.Add(MakePage("Export Receipts",   _tabReceipts));
            tabs.TabPages.Add(MakePage("Download Attendance", _tabAttendance));

            // The academic-year filter applies only to the legacy-source tabs.
            _tabStudents.ShowAcademicYearFilter = true;
            _tabReceipts.ShowAcademicYearFilter = true;

            // Attendance is summarised per calendar month, not a free date range.
            _tabAttendance.UseMonthYearFilter = true;

            Controls.Add(tabs);
            Controls.Add(header);   // added after tabs so Dock=Top sits above the fill

            WireExportReceipts();
            WireExportStudents();
            WireDownloadReceipts();
            WireAttendanceSummary();

            Load += (s, e) => LoadAcademicYears();
        }

        private void LoadAcademicYears()
        {
            try
            {
                var years = _legacy.GetAcademicYears();
                _tabStudents.SetAcademicYears(years);   // latest selected by default
                _tabReceipts.SetAcademicYears(years);
                if (years.Count == 0)
                {
                    _tabStudents.SetStatus("No academic years found in SAS_AcdYear.");
                    _tabReceipts.SetStatus("No academic years found in SAS_AcdYear.");
                }
            }
            catch (Exception ex)
            {
                _tabStudents.SetStatus("Could not load academic years: " + ex.Message);
                _tabReceipts.SetStatus("Could not load academic years: " + ex.Message);
            }
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
                var acdYear = _tabReceipts.SelectedAcademicYear;
                _exportReceipts = await Task.Run(() => _legacy.GetReceipts(from, to, acdYear));
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
                var acdYear = _tabStudents.SelectedAcademicYear;
                _exportStudents = await Task.Run(() => _legacy.GetStudents(from, to, acdYear));
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

        // ── Tab 4: Download Attendance (new app → legacy) ─────────────────────
        private void WireAttendanceSummary()
        {
            // Month/year filter → OnShowPeriod (not OnShow).
            _tabAttendance.OnShowPeriod = async (month, year) =>
            {
                _attendanceMonth = new DateTime(year, month, 1);
                _attendance = await Api.GetAttendanceSummaryAsync(month, year);
                _tabAttendance.Grid.DataSource = _attendance;
                return _attendance.Count;
            };

            _tabAttendance.OnSave = async () =>
            {
                // One branch id for the whole batch (it never varies per row).
                var branchId = await Task.Run(() => _legacy.GetBranchId());

                int inserted = 0, superseded = 0, failed = 0, processed = 0;
                var errors = new List<string>();

                foreach (var row in _attendance)
                {
                    processed++;
                    if (processed % 25 == 0 || processed == _attendance.Count)
                        _tabAttendance.SetStatus($"Saving to legacy… ({processed}/{_attendance.Count})");
                    try
                    {
                        var replaced = await Task.Run(
                            () => _legacy.SaveBulkAttendance(row, _attendanceMonth, branchId));
                        superseded += replaced;
                        inserted++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{row.AdmissionNo}: {ex.Message}");
                        failed++;
                    }
                }

                _tabAttendance.SetStatus(
                    $"Done. Inserted {inserted} row(s), superseded {superseded} earlier row(s), failed {failed}.");

                var sb = new StringBuilder();
                sb.AppendLine($"Month: {_attendanceMonth:MMMM yyyy}");
                sb.AppendLine($"Inserted: {inserted}");
                sb.AppendLine($"Superseded (marked 'D'): {superseded}");
                sb.AppendLine($"Failed: {failed}");
                if (errors.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("Errors:");
                    foreach (var e in errors.Take(20)) sb.AppendLine("  " + e);
                    if (errors.Count > 20) sb.AppendLine($"  …and {errors.Count - 20} more.");
                }
                MessageBox.Show(sb.ToString(), "Download Attendance",
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
