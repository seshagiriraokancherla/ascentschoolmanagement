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
        private readonly DateTimePicker _from = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120 };
        private readonly DateTimePicker _to   = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120 };
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
        /// <summary>Called on Save: persists the shown rows.</summary>
        public Func<Task> OnSave { get; set; }

        public DataGridView Grid => _grid;
        public DateTime FromDate => _from.Value;
        public DateTime ToDate   => _to.Value;

        public SyncTab()
        {
            Dock = DockStyle.Fill;

            var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(8, 8, 8, 8) };
            bar.Controls.Add(new Label { Text = "From:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
            bar.Controls.Add(_from);
            bar.Controls.Add(new Label { Text = "To:", AutoSize = true, Margin = new Padding(12, 6, 4, 0) });
            bar.Controls.Add(_to);
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
            if (OnShow == null) return;
            if (_to.Value.Date < _from.Value.Date)
            {
                MessageBox.Show("'To' date cannot be before 'From' date.", "Invalid range",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            SetBusy(true);
            _status.Text = "Loading…";
            try
            {
                var count = await OnShow(_from.Value, _to.Value);
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
            _from.Enabled = !busy;
            _to.Enabled   = !busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        }
    }
}
