using System;
using System.Drawing;
using System.Windows.Forms;

namespace AscentBellTool
{
    /// Small amber "bell coming up" heads-up in the bottom-right corner. Stays on top but does
    /// NOT steal keyboard focus. Closes itself at bell time (the full ring alert takes over).
    public class WarningForm : Form
    {
        private readonly TodayBell bell;
        private readonly Func<DateTime> now;
        private readonly Label lblCountdown;
        private readonly Timer timer;

        public WarningForm(TodayBell bell, Func<DateTime> now)
        {
            this.bell = bell;
            this.now = now;

            Text = Program.AppTitle;
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            ControlBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.FromArgb(255, 193, 7);
            ForeColor = Color.Black;
            ClientSize = new Size(470, 170);
            var wa = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(wa.Right - Width - 12, wa.Bottom - Height - 12);

            lblCountdown = new Label
            {
                Left = 16, Top = 10, Width = 440, Height = 40,
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
            };
            var lblBell = new Label
            {
                Left = 16, Top = 54, Width = 440, Height = 34,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                Text = bell.Item.Label,
                AutoEllipsis = true,
            };
            var lblTime = new Label
            {
                Left = 16, Top = 92, Width = 300, Height = 28,
                Font = new Font("Segoe UI", 12f),
                Text = "Bell time " + TimeUtil.Show(bell.Due) + " — get ready",
            };
            var ok = new Button
            {
                Text = "OK", Left = 360, Top = 120, Width = 95, Height = 38,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                BackColor = Color.White, FlatStyle = FlatStyle.Flat,
            };
            ok.Click += (s, e) => Close();
            Controls.AddRange(new Control[] { lblCountdown, lblBell, lblTime, ok });

            timer = new Timer { Interval = 1000 };
            timer.Tick += (s, e) => OnTimer();
            UpdateCountdown();
        }

        // Topmost + no-activate + tool window, so typing in another program isn't interrupted.
        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x00000008 /* WS_EX_TOPMOST */ | 0x08000000 /* WS_EX_NOACTIVATE */ | 0x00000080 /* WS_EX_TOOLWINDOW */;
                return cp;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Sound.PlayChime();
            timer.Start();
        }

        private void OnTimer()
        {
            if (now() >= bell.Due) { Close(); return; }
            UpdateCountdown();
        }

        private void UpdateCountdown()
        {
            var left = bell.Due - now();
            if (left < TimeSpan.Zero) left = TimeSpan.Zero;
            lblCountdown.Text = "Bell in " + (int)left.TotalMinutes + ":" + left.Seconds.ToString("00");
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            timer.Stop();
            base.OnFormClosed(e);
        }
    }
}
