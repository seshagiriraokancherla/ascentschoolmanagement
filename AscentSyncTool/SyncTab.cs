using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AscentSyncTool
{
    /// <summary>
    /// Reusable tab chrome: From/To date pickers, Show/Save/Cancel buttons,
    /// a data grid and a status label. Hosts wire OnShow / OnSave callbacks.
    /// </summary>
    public class SyncTab : UserControl
    {
        private readonly Label          _fromLbl = new Label { Text = "From:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) };
        private readonly DateTimePicker _from = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120 };
        private readonly Label          _toLbl = new Label { Text = "To:", AutoSize = true, Margin = new Padding(12, 6, 4, 0) };
        private readonly DateTimePicker _to   = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120 };
        private readonly Label    _yearLbl  = new Label    { Text = "Academic Year:", AutoSize = true, Margin = new Padding(12, 6, 4, 0) };
        private readonly ComboBox _year     = new ComboBox { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly Label    _monthLbl = new Label    { Text = "Month:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) };
        private readonly ComboBox _month    = new ComboBox { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly Label    _calYearLbl = new Label  { Text = "Year:", AutoSize = true, Margin = new Padding(12, 6, 4, 0) };
        private readonly ComboBox _calYear  = new ComboBox { Width = 90, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly Button _btnShow   = new Button { Text = "Show",   Width = 80 };
        private readonly Button _btnSave   = new Button { Text = "Save",   Width = 80, Enabled = false };
        private readonly Button _btnCancel = new Button { Text = "Cancel", Width = 80, Enabled = false };
        private readonly Label  _status    = new Label  { AutoSize = false, Dock = DockStyle.Bottom, Height = 24,
                                                          TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0) };
        private readonly DataGridView _grid = new DataGridView
        {
            Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
            AllowUserToDeleteRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, RowHeadersVisible = false,
            AllowUserToResizeRows = false
        };

        /// <summary>Called on Show: receives From/To, returns the row count loaded into the grid.</summary>
        public Func<DateTime, DateTime, Task<int>> OnShow { get; set; }
        /// <summary>Called on Show when the tab uses the Month/Year filter: receives month (1-12) and year.</summary>
        public Func<int, int, Task<int>> OnShowPeriod { get; set; }
        /// <summary>Called on Save: persists the shown rows.</summary>
        public Func<Task> OnSave { get; set; }

        public DataGridView Grid => _grid;
        public DateTime FromDate => _from.Value;
        public DateTime ToDate   => _to.Value;

        /// <summary>Whether the academic-year filter dropdown is shown (legacy-source tabs only).</summary>
        public bool ShowAcademicYearFilter
        {
            get => _yearLbl.Visible;
            set { _yearLbl.Visible = value; _year.Visible = value; }
        }

        /// <summary>The currently selected academic year, or null when the filter is hidden/empty.</summary>
        public string SelectedAcademicYear =>
            (ShowAcademicYearFilter && _year.SelectedItem != null) ? _year.SelectedItem.ToString() : null;

        /// <summary>Populate the academic-year dropdown; selects the first (latest) by default.</summary>
        public void SetAcademicYears(System.Collections.Generic.IEnumerable<string> years)
        {
            _year.Items.Clear();
            foreach (var y in years) _year.Items.Add(y);
            if (_year.Items.Count > 0) _year.SelectedIndex = 0;
        }

        /// <summary>
        /// Swaps the From/To date range for Month + Year dropdowns (defaults to the
        /// current month). Show then calls <see cref="OnShowPeriod"/> instead of OnShow.
        /// </summary>
        public bool UseMonthYearFilter
        {
            get => _monthLbl.Visible;
            set
            {
                _monthLbl.Visible = _month.Visible = _calYearLbl.Visible = _calYear.Visible = value;
                _fromLbl.Visible  = _from.Visible  = _toLbl.Visible      = _to.Visible      = !value;
            }
        }

        /// <summary>Selected month (1-12).</summary>
        public int SelectedMonth => _month.SelectedIndex + 1;
        /// <summary>Selected calendar year.</summary>
        public int SelectedYear => _calYear.SelectedItem != null
            ? int.Parse(_calYear.SelectedItem.ToString())
            : DateTime.Today.Year;

        public SyncTab()
        {
            Dock = DockStyle.Fill;

            var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(8, 8, 8, 8) };
            bar.Controls.Add(_fromLbl);
            bar.Controls.Add(_from);
            bar.Controls.Add(_toLbl);
            bar.Controls.Add(_to);
            bar.Controls.Add(_monthLbl);
            bar.Controls.Add(_month);
            bar.Controls.Add(_calYearLbl);
            bar.Controls.Add(_calYear);
            bar.Controls.Add(_yearLbl);
            bar.Controls.Add(_year);
            _yearLbl.Visible = false;
            _year.Visible = false;
            _monthLbl.Visible = false;
            _month.Visible = false;
            _calYearLbl.Visible = false;
            _calYear.Visible = false;

            // Month/year dropdown contents (harmless when the filter stays hidden).
            for (int m = 1; m <= 12; m++)
                _month.Items.Add(System.Globalization.CultureInfo.CurrentCulture
                    .DateTimeFormat.GetMonthName(m));
            _month.SelectedIndex = DateTime.Today.Month - 1;

            for (int y = DateTime.Today.Year + 1; y >= DateTime.Today.Year - 5; y--)
                _calYear.Items.Add(y.ToString());
            _calYear.SelectedItem = DateTime.Today.Year.ToString();

            bar.Controls.Add(_btnShow);
            bar.Controls.Add(_btnSave);
            bar.Controls.Add(_btnCancel);

            _from.Value = DateTime.Today;
            _to.Value   = DateTime.Today;

            _btnShow.Click   += async (s, e) => await RunShow();
            _btnSave.Click   += async (s, e) => await RunSave();
            _btnCancel.Click += (s, e) => Reset();

            Controls.Add(_grid);
            Controls.Add(bar);
            Controls.Add(_status);
        }

        public void SetStatus(string text) => _status.Text = text;

        private void Reset()
        {
            _grid.DataSource = null;
            _btnSave.Enabled = false;
            _btnCancel.Enabled = false;
            _status.Text = "Cleared.";
        }

        private async Task RunShow()
        {
            if (UseMonthYearFilter ? OnShowPeriod == null : OnShow == null) return;
            if (!UseMonthYearFilter && _to.Value.Date < _from.Value.Date)
            {
                MessageBox.Show("'To' date cannot be before 'From' date.", "Invalid range",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            SetBusy(true);
            _status.Text = "Loading…";
            try
            {
                var count = UseMonthYearFilter
                    ? await OnShowPeriod(SelectedMonth, SelectedYear)
                    : await OnShow(_from.Value, _to.Value);
                _status.Text = $"Loaded {count} row(s).";
                _btnSave.Enabled = count > 0;
                _btnCancel.Enabled = count > 0;
            }
            catch (Exception ex)
            {
                _status.Text = "Error.";
                MessageBox.Show(ex.Message, "Load failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { SetBusy(false); }
        }

        private async Task RunSave()
        {
            if (OnSave == null) return;
            if (_grid.DataSource == null)
            {
                MessageBox.Show("Nothing to save. Click Show first.", "Save",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            SetBusy(true, keepCancel: false);
            _status.Text = "Saving…";
            try
            {
                await OnSave();
            }
            catch (Exception ex)
            {
                _status.Text = "Save error.";
                MessageBox.Show(ex.Message, "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { SetBusy(false); }
        }

        private void SetBusy(bool busy, bool keepCancel = true)
        {
            _btnShow.Enabled   = !busy;
            _btnSave.Enabled   = !busy && _grid.DataSource != null;
            _btnCancel.Enabled = !busy && keepCancel && _grid.DataSource != null;
            _from.Enabled    = !busy;
            _to.Enabled      = !busy;
            _year.Enabled    = !busy;
            _month.Enabled   = !busy;
            _calYear.Enabled = !busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        }
    }
}
