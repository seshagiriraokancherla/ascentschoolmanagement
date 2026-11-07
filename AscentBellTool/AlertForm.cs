using System;
using System.Drawing;
using System.Windows.Forms;

namespace AscentBellTool
{
    /// Full-screen "RING THE BELL" alert. Loops the bell sound and stays on top until someone
    /// clicks the confirm button — it cannot be closed with Alt+F4 / Esc. After
    /// EscalateAfterSeconds it goes to full volume.
    public class AlertForm : Form
    {
        private readonly TodayBell bell;
        private readonly AppSettings settings;
        private readonly Action<DateTime> onConfirmed;
        private readonly bool isTest;

        private readonly Label lblTitle, lblWait;
        private readonly Button btnDone;
        private readonly Timer timer;
        private readonly DateTime shownAt = DateTime.Now;
        private bool confirmed, escalated, flash;
        private int ticks;

        private static readonly Color RedA = Color.FromArgb(176, 18, 18);
        private static readonly Color RedB = Color.FromArgb(214, 40, 40);

        public AlertForm(TodayBell bell, AppSettings settings, bool isTest, Action<DateTime> onConfirmed)
        {
            this.bell = bell;
            this.settings = settings;
            this.isTest = isTest;
            this.onConfirmed = onConfirmed;

            Text = "RING THE BELL — " + bell.Item.Label;
            Icon = AppIcon.Large;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = Screen.PrimaryScreen.Bounds;
            WindowState = FormWindowState.Maximized;
            TopMost = true;
            ShowInTaskbar = true;
            BackColor = RedA;
            ForeColor = Color.White;
            KeyPreview = true;

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7, BackColor = Color.Transparent };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 12));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 18));   // title
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 18));   // bell label
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 10));   // time
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 8));    // waiting
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 140)); // button
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 14));   // hint

            lblTitle = Big(isTest ? "TEST ALERT" : "RING THE BELL", 60, FontStyle.Bold, Color.White);
            var lblBell = Big(bell.Item.Label, 44, FontStyle.Bold, Color.FromArgb(255, 236, 140));
            var lblTime = Big("Bell time  " + TimeUtil.Show(bell.Due), 24, FontStyle.Regular, Color.White);
            lblWait = Big("", 18, FontStyle.Regular, Color.FromArgb(255, 220, 220));
            var hint = Big("This alert keeps ringing until you click the button.", 13, FontStyle.Regular, Color.FromArgb(255, 210, 210));

            btnDone = new Button
            {
                Text = isTest ? "OK — CLOSE TEST" : "I HAVE RUNG THE BELL",
                Font = new Font("Segoe UI", 26f, FontStyle.Bold),
                BackColor = Color.FromArgb(34, 139, 34),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(620, 110),
                Anchor = AnchorStyles.None,
                Enabled = false,          // enabled after 2 s so a stray click can't dismiss it
                Cursor = Cursors.Hand,
                TabStop = false,
            };
            btnDone.FlatAppearance.BorderColor = Color.White;
            btnDone.FlatAppearance.BorderSize = 3;
            btnDone.Click += (s, e) => Confirm();

            layout.Controls.Add(new Label(), 0, 0);
            layout.Controls.Add(lblTitle, 0, 1);
            layout.Controls.Add(lblBell, 0, 2);
            layout.Controls.Add(lblTime, 0, 3);
            layout.Controls.Add(lblWait, 0, 4);
            layout.Controls.Add(btnDone, 0, 5);
            layout.Controls.Add(hint, 0, 6);
            Controls.Add(layout);

            timer = new Timer { Interval = 500 };
            timer.Tick += (s, e) => OnTick();
        }

        private static Label Big(string text, float size, FontStyle style, Color color) => new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", size, style),
            ForeColor = color,
            BackColor = Color.Transparent,
            AutoEllipsis = true,
        };

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            AudioVolume.EnsureAudible(settings.MinVolumePercent);
            Sound.StartLoop(settings.CustomSoundFile);
            timer.Start();
            Activate();
        }

        private void OnTick()
        {
            ticks++;
            var waited = DateTime.Now - shownAt;

            flash = !flash;
            BackColor = flash ? RedB : RedA;

            if (!btnDone.Enabled && waited.TotalSeconds >= 2) btnDone.Enabled = true;

            lblWait.Text = "Waiting  " + (int)waited.TotalMinutes + ":" + waited.Seconds.ToString("00");

            if (!escalated && !isTest && waited.TotalSeconds >= Math.Max(10, settings.EscalateAfterSeconds))
            {
                escalated = true;
                AudioVolume.EnsureAudible(100);
                lblTitle.Text = "STILL WAITING — RING THE BELL";
                BellLog.Write($"ESCALATED  {TimeUtil.Show(bell.Due)} {bell.Item.Label} — not confirmed after {(int)waited.TotalSeconds}s, volume raised to 100%");
            }

            // Other programs can push us behind them; re-assert every 5 s.
            if (ticks % 10 == 0)
            {
                TopMost = false;
                TopMost = true;
                if (escalated) AudioVolume.EnsureAudible(100);
            }
        }

        private void Confirm()
        {
            confirmed = true;
            onConfirmed?.Invoke(DateTime.Now);
            Close();
        }

        // Swallow every key (Esc/Enter/Space would otherwise press or close) — this window takes
        // focus while someone may be typing elsewhere, so only a deliberate mouse click confirms.
        protected override void OnKeyDown(KeyEventArgs e) { e.Handled = true; e.SuppressKeyPress = true; }
        protected override void OnKeyUp(KeyEventArgs e) { e.Handled = true; }
        protected override bool ProcessDialogKey(Keys keyData) => true;
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData) =>
            (keyData & Keys.KeyCode) == Keys.F4 && (keyData & Keys.Alt) == Keys.Alt
                ? base.ProcessCmdKey(ref msg, keyData)   // Alt+F4 reaches OnFormClosing, which refuses it
                : true;

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!confirmed && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                return;
            }
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            timer.Stop();
            Sound.StopLoop();
            if (!confirmed && !isTest)
                BellLog.Write($"NOT CONFIRMED  {TimeUtil.Show(bell.Due)} {bell.Item.Label} — alert closed by {e.CloseReason}");
            base.OnFormClosed(e);
        }
    }
}
