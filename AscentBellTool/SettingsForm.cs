using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AscentBellTool
{
    /// Edits a copy of AppData; Result is only set (and returned with OK) after validation passes.
    public class SettingsForm : Form
    {
        public AppData Result { get; private set; }

        private AppData work;
        private int current = -1;
        private const int InvalidWarn = -999;
        private bool loading;

        // Schedules tab
        private ListBox lstSchedules;
        private TextBox txtName;
        private CheckBox chkActive;
        private readonly CheckBox[] dayChecks = new CheckBox[7];
        private DataGridView gridBells;

        // Holidays tab
        private DataGridView gridHolidays;

        // General tab
        private TextBox txtSchool, txtSound;
        private NumericUpDown numWarn, numEscalate, numVolume;
        private CheckBox chkKeepAwake, chkClock;
        private Label lblPassword;

        private static readonly DayOfWeek[] DayOrder =
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
            DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday,
        };

        public SettingsForm(AppData data)
        {
            work = DataStore.Clone(data);

            Text = Program.AppTitle + " — Settings";
            Icon = AppIcon.Large;
            Font = new Font("Segoe UI", 9.75f);
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(940, 640);
            MinimumSize = new Size(820, 520);

            var tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(BuildSchedulesTab());
            tabs.TabPages.Add(BuildHolidaysTab());
            tabs.TabPages.Add(BuildGeneralTab());

            var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
            var btnCancel = new Button { Text = "Cancel", Width = 100, Height = 30, DialogResult = DialogResult.Cancel };
            var btnSave = new Button { Text = "Save", Width = 100, Height = 30 };
            btnSave.Click += (s, e) => SaveAndClose();
            bottom.Controls.Add(btnCancel);
            bottom.Controls.Add(btnSave);
            CancelButton = btnCancel;

            Controls.Add(tabs);
            Controls.Add(bottom);

            LoadAll();
        }

        // ================================================================ build UI

        private TabPage BuildSchedulesTab()
        {
            var page = new TabPage("Bell timings") { Padding = new Padding(8) };

            // Left: schedule list
            var left = new Panel { Dock = DockStyle.Left, Width = 210, Padding = new Padding(0, 0, 8, 0) };
            var lblList = new Label { Text = "Schedules", Dock = DockStyle.Top, Height = 22, Font = new Font(Font, FontStyle.Bold) };
            lstSchedules = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
            lstSchedules.SelectedIndexChanged += (s, e) => { if (!loading) SelectSchedule(lstSchedules.SelectedIndex); };
            var listButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 72, FlowDirection = FlowDirection.LeftToRight };
            listButtons.Controls.Add(Btn("Add", 64, (s, e) => AddSchedule(copy: false)));
            listButtons.Controls.Add(Btn("Copy", 64, (s, e) => AddSchedule(copy: true)));
            listButtons.Controls.Add(Btn("Delete", 64, (s, e) => DeleteSchedule()));
            listButtons.Controls.Add(new Label
            {
                Text = "e.g. Regular, Saturday, Exam day",
                AutoSize = true, ForeColor = Color.Gray, Margin = new Padding(3, 6, 3, 0),
            });
            left.Controls.Add(lstSchedules);
            left.Controls.Add(listButtons);
            left.Controls.Add(lblList);

            // Right: selected schedule
            var right = new Panel { Dock = DockStyle.Fill };

            var top = new TableLayoutPanel { Dock = DockStyle.Top, Height = 66, ColumnCount = 2, RowCount = 2 };
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            txtName = new TextBox { Width = 220 };
            txtName.TextChanged += (s, e) => RenameCurrent();
            chkActive = new CheckBox { Text = "Active", AutoSize = true, Margin = new Padding(16, 5, 3, 3) };
            var nameRow = new FlowLayoutPanel { AutoSize = true, Margin = Padding.Empty };
            nameRow.Controls.Add(txtName);
            nameRow.Controls.Add(chkActive);
            var daysRow = new FlowLayoutPanel { AutoSize = true, Margin = Padding.Empty };
            for (int i = 0; i < 7; i++)
            {
                dayChecks[i] = new CheckBox { Text = DayOrder[i].ToString().Substring(0, 3), AutoSize = true, Margin = new Padding(3, 5, 6, 3) };
                daysRow.Controls.Add(dayChecks[i]);
            }
            top.Controls.Add(new Label { Text = "Name", AutoSize = true, Margin = new Padding(3, 7, 3, 3) }, 0, 0);
            top.Controls.Add(nameRow, 1, 0);
            top.Controls.Add(new Label { Text = "Days", AutoSize = true, Margin = new Padding(3, 7, 3, 3) }, 0, 1);
            top.Controls.Add(daysRow, 1, 1);

            gridBells = NewGrid();
            gridBells.Columns.Add(new DataGridViewTextBoxColumn { Name = "Time", HeaderText = "Time (24-hour)", Width = 120 });
            gridBells.Columns.Add(new DataGridViewTextBoxColumn { Name = "Shows", HeaderText = "Shows as", Width = 90, ReadOnly = true });
            gridBells.Columns.Add(new DataGridViewTextBoxColumn { Name = "Label", HeaderText = "Bell / period", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            gridBells.Columns.Add(new DataGridViewTextBoxColumn { Name = "Warn", HeaderText = "Heads-up (min)", Width = 120 });
            gridBells.Columns["Shows"].DefaultCellStyle.ForeColor = Color.DimGray;
            gridBells.CellEndEdit += (s, e) => { if (e.ColumnIndex == 0 && e.RowIndex >= 0) NormalizeTimeCell(gridBells.Rows[e.RowIndex]); };

            var bellButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 64 };
            bellButtons.Controls.Add(Btn("Delete selected bells", 160, (s, e) => DeleteSelectedRows(gridBells)));
            bellButtons.Controls.Add(Btn("Sort by time", 110, (s, e) => SortBells()));
            bellButtons.Controls.Add(new Label
            {
                AutoSize = true, ForeColor = Color.DimGray, Margin = new Padding(8, 4, 3, 0),
                Text = "Type a new bell in the last (empty) row. Times are 24-hour: 1:15 PM = 13:15 (\"1:15 PM\" also works).\n" +
                       "Heads-up: minutes before the bell for the small reminder — leave blank for the default, 0 for none.",
            });

            right.Controls.Add(gridBells);
            right.Controls.Add(bellButtons);
            right.Controls.Add(top);

            page.Controls.Add(right);
            page.Controls.Add(left);
            return page;
        }

        private TabPage BuildHolidaysTab()
        {
            var page = new TabPage("Holidays") { Padding = new Padding(8) };
            var hint = new Label
            {
                Dock = DockStyle.Top, Height = 44, ForeColor = Color.DimGray,
                Text = "No bells are shown on these dates (in addition to days not ticked in a schedule, e.g. Sunday).\n" +
                       "Date format: dd-MM-yyyy, e.g. 15-08-2026. Type in the last (empty) row to add.",
            };
            gridHolidays = NewGrid();
            gridHolidays.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "Date (dd-MM-yyyy)", Width = 150 });
            gridHolidays.Columns.Add(new DataGridViewTextBoxColumn { Name = "Day", HeaderText = "Day", Width = 100, ReadOnly = true });
            gridHolidays.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Holiday", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            gridHolidays.Columns["Day"].DefaultCellStyle.ForeColor = Color.DimGray;
            gridHolidays.CellEndEdit += (s, e) => { if (e.ColumnIndex == 0 && e.RowIndex >= 0) NormalizeDateCell(gridHolidays.Rows[e.RowIndex]); };

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40 };
            buttons.Controls.Add(Btn("Delete selected", 130, (s, e) => DeleteSelectedRows(gridHolidays)));
            buttons.Controls.Add(Btn("Remove past holidays", 160, (s, e) => RemovePastHolidays()));

            page.Controls.Add(gridHolidays);
            page.Controls.Add(buttons);
            page.Controls.Add(hint);
            return page;
        }

        private TabPage BuildGeneralTab()
        {
            var page = new TabPage("General") { Padding = new Padding(12), AutoScroll = true };
            var t = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2 };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            txtSchool = new TextBox { Width = 360 };
            numWarn = new NumericUpDown { Minimum = 0, Maximum = 30, Width = 70 };
            numEscalate = new NumericUpDown { Minimum = 10, Maximum = 600, Increment = 10, Width = 70 };
            numVolume = new NumericUpDown { Minimum = 0, Maximum = 100, Increment = 5, Width = 70 };

            txtSound = new TextBox { Width = 300, ReadOnly = true };
            var soundRow = new FlowLayoutPanel { AutoSize = true, Margin = Padding.Empty };
            soundRow.Controls.Add(txtSound);
            soundRow.Controls.Add(Btn("Browse…", 80, (s, e) => BrowseSound()));
            soundRow.Controls.Add(Btn("Built-in", 80, (s, e) => txtSound.Text = ""));
            soundRow.Controls.Add(Btn("Test sound", 90, (s, e) => Sound.PlayRingOnce(txtSound.Text)));

            chkKeepAwake = new CheckBox { Text = "Keep this PC awake (no sleep / screen-off) during school hours", AutoSize = true };
            chkClock = new CheckBox { Text = "Check the PC clock against internet time when online, and correct for it", AutoSize = true };

            lblPassword = new Label { AutoSize = true, Margin = new Padding(3, 7, 10, 3) };
            var pwRow = new FlowLayoutPanel { AutoSize = true, Margin = Padding.Empty };
            pwRow.Controls.Add(lblPassword);
            pwRow.Controls.Add(Btn("Set / change…", 110, (s, e) => SetPassword()));
            pwRow.Controls.Add(Btn("Remove", 80, (s, e) => { work.Settings.AdminPasswordHash = null; ShowPasswordState(); }));

            var fileRow = new FlowLayoutPanel { AutoSize = true, Margin = Padding.Empty };
            fileRow.Controls.Add(Btn("Export to file…", 120, (s, e) => Export()));
            fileRow.Controls.Add(Btn("Import from file…", 130, (s, e) => Import()));
            fileRow.Controls.Add(Btn("Open data folder", 130, (s, e) =>
            {
                try { System.Diagnostics.Process.Start("explorer.exe", "\"" + DataStore.Folder + "\""); } catch { }
            }));

            AddRow(t, "School name", txtSchool);
            AddRow(t, "Default heads-up before each bell (minutes)", numWarn);
            AddRow(t, "If not confirmed, go to full volume after (seconds)", numEscalate);
            AddRow(t, "Minimum volume for ring alerts (%)", numVolume);
            AddRow(t, "Bell sound (.wav, optional)", soundRow);
            AddRow(t, "", chkKeepAwake);
            AddRow(t, "", chkClock);
            AddRow(t, "Admin password (protects Settings, Pause, Exit)", pwRow);
            AddRow(t, "Copy timings to / from another PC", fileRow);

            page.Controls.Add(t);
            return page;
        }

        private static void AddRow(TableLayoutPanel t, string label, Control c)
        {
            int r = t.RowCount++;
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            t.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(3, 8, 3, 3) }, 0, r);
            c.Margin = new Padding(3, 4, 3, 4);
            t.Controls.Add(c, 1, r);
        }

        private static Button Btn(string text, int width, EventHandler click)
        {
            var b = new Button { Text = text, Width = width, Height = 28 };
            b.Click += click;
            return b;
        }

        private static DataGridView NewGrid() => new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = true,
            AllowUserToDeleteRows = true,
            AllowUserToResizeRows = false,
            RowHeadersWidth = 28,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = SystemColors.Window,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2,
        };

        // ================================================================ load / commit

        private void LoadAll()
        {
            loading = true;
            lstSchedules.Items.Clear();
            foreach (var s in work.Schedules) lstSchedules.Items.Add(ListText(s));
            loading = false;
            current = -1;
            if (work.Schedules.Count > 0) { lstSchedules.SelectedIndex = 0; SelectSchedule(0); }
            else ClearScheduleEditor();

            gridHolidays.Rows.Clear();
            foreach (var h in work.Holidays.OrderBy(h => h.Date))
            {
                int r = gridHolidays.Rows.Add(TimeUtil.TryParseDate(h.Date, out var d) ? TimeUtil.ShowDate(d) : h.Date, "", h.Name);
                NormalizeDateCell(gridHolidays.Rows[r]);
            }

            var st = work.Settings;
            txtSchool.Text = work.SchoolName ?? "";
            numWarn.Value = Clamp(st.DefaultWarnMinutes, numWarn);
            numEscalate.Value = Clamp(st.EscalateAfterSeconds, numEscalate);
            numVolume.Value = Clamp(st.MinVolumePercent, numVolume);
            txtSound.Text = st.CustomSoundFile ?? "";
            chkKeepAwake.Checked = st.KeepAwake;
            chkClock.Checked = st.CheckClock;
            ShowPasswordState();
        }

        private static decimal Clamp(int v, NumericUpDown n) => Math.Max(n.Minimum, Math.Min(n.Maximum, v));

        private static string ListText(BellSchedule s) =>
            (string.IsNullOrWhiteSpace(s.Name) ? "(no name)" : s.Name) + (s.Active ? "" : "  (off)");

        private void SelectSchedule(int index)
        {
            if (index == current) return;
            CommitCurrent();
            current = index;
            if (index < 0 || index >= work.Schedules.Count) { ClearScheduleEditor(); return; }

            loading = true;
            var s = work.Schedules[index];
            txtName.Text = s.Name;
            chkActive.Checked = s.Active;
            for (int i = 0; i < 7; i++) dayChecks[i].Checked = s.Days.Contains((int)DayOrder[i]);
            gridBells.Rows.Clear();
            foreach (var b in s.Items)
            {
                int r = gridBells.Rows.Add(b.Time, "", b.Label,
                    b.WarnMinutes == InvalidWarn ? "?" : b.WarnMinutes?.ToString() ?? "");
                NormalizeTimeCell(gridBells.Rows[r]);
            }
            SetScheduleEditorEnabled(true);
            loading = false;
        }

        private void ClearScheduleEditor()
        {
            loading = true;
            txtName.Text = "";
            chkActive.Checked = false;
            foreach (var c in dayChecks) c.Checked = false;
            gridBells.Rows.Clear();
            SetScheduleEditorEnabled(false);
            loading = false;
        }

        private void SetScheduleEditorEnabled(bool on)
        {
            txtName.Enabled = chkActive.Enabled = gridBells.Enabled = on;
            foreach (var c in dayChecks) c.Enabled = on;
        }

        /// Copies the editor contents back into work.Schedules[current] (no validation here).
        private void CommitCurrent()
        {
            if (current < 0 || current >= work.Schedules.Count) return;
            gridBells.EndEdit();
            var s = work.Schedules[current];
            s.Name = txtName.Text.Trim();
            s.Active = chkActive.Checked;
            s.Days = Enumerable.Range(0, 7).Where(i => dayChecks[i].Checked).Select(i => (int)DayOrder[i]).ToList();
            s.Items = new List<BellItem>();
            foreach (DataGridViewRow row in gridBells.Rows)
            {
                if (row.IsNewRow) continue;
                var time = Cell(row, "Time");
                var label = Cell(row, "Label");
                var warn = Cell(row, "Warn");
                if (time == "" && label == "" && warn == "") continue;
                s.Items.Add(new BellItem
                {
                    Time = TimeUtil.TryParseTime(time, out var t) ? TimeUtil.To24(t) : time,
                    Label = label,
                    // Non-numeric heads-up → InvalidWarn, reported by Validate().
                    WarnMinutes = warn == "" ? (int?)null : int.TryParse(warn, out var w) ? w : InvalidWarn,
                });
            }
            if (lstSchedules.Items.Count > current) lstSchedules.Items[current] = ListText(s);
        }

        private void RenameCurrent()
        {
            if (loading || current < 0 || current >= work.Schedules.Count) return;
            work.Schedules[current].Name = txtName.Text.Trim();
            loading = true;
            lstSchedules.Items[current] = ListText(work.Schedules[current]);
            loading = false;
        }

        private static string Cell(DataGridViewRow row, string col) => (row.Cells[col].Value?.ToString() ?? "").Trim();

        private void NormalizeTimeCell(DataGridViewRow row)
        {
            if (row.IsNewRow) return;
            var raw = Cell(row, "Time");
            if (TimeUtil.TryParseTime(raw, out var t))
            {
                row.Cells["Time"].Value = TimeUtil.To24(t);
                row.Cells["Shows"].Value = TimeUtil.To12(t);
                row.Cells["Time"].Style.BackColor = t.Hours < 6 ? Color.MistyRose : Color.Empty;
            }
            else
            {
                row.Cells["Shows"].Value = raw == "" ? "" : "invalid";
                row.Cells["Time"].Style.BackColor = raw == "" ? Color.Empty : Color.MistyRose;
            }
        }

        private void NormalizeDateCell(DataGridViewRow row)
        {
            if (row.IsNewRow) return;
            var raw = Cell(row, "Date");
            if (TimeUtil.TryParseDate(raw, out var d))
            {
                row.Cells["Date"].Value = TimeUtil.ShowDate(d);
                row.Cells["Day"].Value = d.DayOfWeek.ToString();
                row.Cells["Date"].Style.BackColor = Color.Empty;
            }
            else
            {
                row.Cells["Day"].Value = raw == "" ? "" : "invalid";
                row.Cells["Date"].Style.BackColor = raw == "" ? Color.Empty : Color.MistyRose;
            }
        }

        // ================================================================ actions

        private void AddSchedule(bool copy)
        {
            CommitCurrent();
            BellSchedule s;
            if (copy && current >= 0)
            {
                var src = work.Schedules[current];
                s = new BellSchedule
                {
                    Name = src.Name + " (copy)",
                    Active = false,            // off until days are chosen, so it can't clash
                    Days = new List<int>(),
                    Items = src.Items.Select(i => new BellItem { Time = i.Time, Label = i.Label, WarnMinutes = i.WarnMinutes }).ToList(),
                };
            }
            else
            {
                s = new BellSchedule { Name = "New schedule", Active = false };
            }
            work.Schedules.Add(s);
            loading = true;
            lstSchedules.Items.Add(ListText(s));
            lstSchedules.SelectedIndex = lstSchedules.Items.Count - 1;
            loading = false;
            current = -1;
            SelectSchedule(work.Schedules.Count - 1);
            txtName.Focus();
            txtName.SelectAll();
        }

        private void DeleteSchedule()
        {
            if (current < 0) return;
            if (MessageBox.Show($"Delete the schedule \"{work.Schedules[current].Name}\" and all its bells?",
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            work.Schedules.RemoveAt(current);
            current = -1;
            loading = true;
            lstSchedules.Items.Clear();
            foreach (var s in work.Schedules) lstSchedules.Items.Add(ListText(s));
            loading = false;
            if (work.Schedules.Count > 0) { lstSchedules.SelectedIndex = 0; SelectSchedule(0); }
            else ClearScheduleEditor();
        }

        private static void DeleteSelectedRows(DataGridView grid)
        {
            grid.EndEdit();
            foreach (DataGridViewRow row in grid.SelectedRows.Cast<DataGridViewRow>().ToList())
                if (!row.IsNewRow) grid.Rows.Remove(row);
        }

        private void SortBells()
        {
            if (current < 0) return;
            CommitCurrent();
            var s = work.Schedules[current];
            s.Items = s.Items.OrderBy(i => TimeUtil.TryParseTime(i.Time, out var t) ? t : TimeSpan.MaxValue).ToList();
            var keep = current;
            current = -1;
            SelectSchedule(keep);
        }

        private void RemovePastHolidays()
        {
            gridHolidays.EndEdit();
            foreach (DataGridViewRow row in gridHolidays.Rows.Cast<DataGridViewRow>().ToList())
                if (!row.IsNewRow && TimeUtil.TryParseDate(Cell(row, "Date"), out var d) && d.Date < DateTime.Today)
                    gridHolidays.Rows.Remove(row);
        }

        private void BrowseSound()
        {
            using (var dlg = new OpenFileDialog { Filter = "WAV sound (*.wav)|*.wav", Title = "Choose a bell sound" })
                if (dlg.ShowDialog(this) == DialogResult.OK) txtSound.Text = dlg.FileName;
        }

        private void SetPassword()
        {
            var p = PasswordPrompt.AskNew();
            if (p == null) return;
            work.Settings.AdminPasswordHash = PasswordPrompt.Hash(p);
            ShowPasswordState();
        }

        private void ShowPasswordState()
        {
            bool set = !string.IsNullOrEmpty(work.Settings.AdminPasswordHash);
            lblPassword.Text = set ? "Password is set" : "No password";
            lblPassword.ForeColor = set ? Color.DarkGreen : Color.DimGray;
        }

        private void CommitGeneral()
        {
            work.SchoolName = txtSchool.Text.Trim();
            work.Settings.DefaultWarnMinutes = (int)numWarn.Value;
            work.Settings.EscalateAfterSeconds = (int)numEscalate.Value;
            work.Settings.MinVolumePercent = (int)numVolume.Value;
            work.Settings.CustomSoundFile = string.IsNullOrWhiteSpace(txtSound.Text) ? null : txtSound.Text.Trim();
            work.Settings.KeepAwake = chkKeepAwake.Checked;
            work.Settings.CheckClock = chkClock.Checked;

            gridHolidays.EndEdit();
            work.Holidays = new List<Holiday>();
            foreach (DataGridViewRow row in gridHolidays.Rows)
            {
                if (row.IsNewRow) continue;
                var date = Cell(row, "Date");
                var name = Cell(row, "Name");
                if (date == "" && name == "") continue;
                work.Holidays.Add(new Holiday
                {
                    Date = TimeUtil.TryParseDate(date, out var d) ? TimeUtil.StoreDate(d) : date,
                    Name = name,
                });
            }
        }

        private void Export()
        {
            CommitCurrent();
            CommitGeneral();
            using (var dlg = new SaveFileDialog
            {
                Filter = "Ascent Bell timings (*.json)|*.json",
                FileName = (string.IsNullOrWhiteSpace(work.SchoolName) ? "bell" : work.SchoolName) + " timings.json",
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                var copy = DataStore.Clone(work);
                copy.Settings.AdminPasswordHash = null;     // never export the password
                copy.Settings.CustomSoundFile = null;       // path only valid on this PC
                File.WriteAllText(dlg.FileName, DataStore.ToJson(copy), Encoding.UTF8);
                MessageBox.Show("Exported. On the other PC: Settings → General → Import from file.", Text);
            }
        }

        private void Import()
        {
            using (var dlg = new OpenFileDialog { Filter = "Ascent Bell timings (*.json)|*.json" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                AppData imported;
                try { imported = DataStore.Clone(DataStore.FromJson(File.ReadAllText(dlg.FileName, Encoding.UTF8))); }
                catch (Exception ex)
                {
                    MessageBox.Show("That file could not be read:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (MessageBox.Show($"Replace ALL schedules, holidays and settings with the ones in this file " +
                                    $"({imported.Schedules.Count} schedule(s), {imported.Holidays.Count} holiday(s))?\n\n" +
                                    "Your admin password and sound file are kept. Nothing is saved until you click Save.",
                        Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

                imported.Settings.AdminPasswordHash = work.Settings.AdminPasswordHash;
                imported.Settings.CustomSoundFile = work.Settings.CustomSoundFile;
                work = imported;
                current = -1;
                LoadAll();
            }
        }

        // ================================================================ save

        private void SaveAndClose()
        {
            CommitCurrent();
            CommitGeneral();

            var errors = Validate(work);
            if (errors.Count > 0)
            {
                var shown = errors.Take(15).ToList();
                if (errors.Count > 15) shown.Add($"…and {errors.Count - 15} more.");
                MessageBox.Show("Please fix these before saving:\n\n• " + string.Join("\n• ", shown),
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            foreach (var s in work.Schedules)
                s.Items = s.Items.OrderBy(i => { TimeUtil.TryParseTime(i.Time, out var t); return t; }).ToList();
            work.Holidays = work.Holidays.OrderBy(h => h.Date).ToList();

            Result = work;
            DialogResult = DialogResult.OK;
            Close();
        }

        private static List<string> Validate(AppData d)
        {
            var errors = new List<string>();

            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in d.Schedules)
            {
                var n = string.IsNullOrWhiteSpace(s.Name) ? "(no name)" : s.Name;
                if (string.IsNullOrWhiteSpace(s.Name)) errors.Add("A schedule has no name.");
                else if (!names.Add(s.Name)) errors.Add($"Two schedules are named \"{s.Name}\".");

                if (s.Active && s.Days.Count == 0) errors.Add($"\"{n}\" is Active but no days are ticked.");

                var times = new HashSet<string>();
                foreach (var b in s.Items)
                {
                    var label = b.Label;
                    var where = $"\"{n}\" — {(string.IsNullOrWhiteSpace(b.Time) ? "(no time)" : b.Time)} {label}".TrimEnd();

                    if (!TimeUtil.TryParseTime(b.Time, out var t))
                        errors.Add($"{where}: time is not valid (use e.g. 13:15).");
                    else
                    {
                        if (t.Hours < 6)
                            errors.Add($"{where}: {TimeUtil.To12(t)} is at night. Times are 24-hour — for the afternoon use {TimeUtil.To24(t.Add(TimeSpan.FromHours(12)))}.");
                        if (!times.Add(TimeUtil.To24(t)))
                            errors.Add($"{where}: there are two bells at {TimeUtil.To24(t)}.");
                    }
                    if (string.IsNullOrWhiteSpace(label)) errors.Add($"{where}: the bell needs a name (e.g. 3rd period).");
                    if (b.WarnMinutes.HasValue && (b.WarnMinutes < 0 || b.WarnMinutes > 30))
                        errors.Add($"{where}: heads-up must be a number of minutes from 0 to 30, or blank.");
                }
            }

            // Each weekday may belong to only one active schedule, otherwise it's ambiguous which rings.
            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                var owners = d.Schedules.Where(s => s.Active && s.Days.Contains((int)day)).Select(s => s.Name).ToList();
                if (owners.Count > 1)
                    errors.Add($"{day} is ticked in more than one active schedule ({string.Join(", ", owners)}). Untick it in all but one.");
            }

            foreach (var h in d.Holidays)
                if (!TimeUtil.TryParseDate(h.Date, out _))
                    errors.Add($"Holiday \"{h.Name}\": date \"{h.Date}\" is not valid (use dd-MM-yyyy).");

            return errors;
        }
    }
}
