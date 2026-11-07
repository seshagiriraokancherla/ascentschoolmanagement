using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AscentBellTool
{
    /// The app: a tray icon, a once-a-second scheduler tick, and the alert windows.
    public class TrayApp : ApplicationContext
    {
        private readonly NotifyIcon tray;
        private readonly ContextMenuStrip menu;
        private readonly Timer tickTimer, clockTimer;
        private readonly BellScheduler sched;
        private AppData data;
        private bool keepingAwake, settingsOpen;
        private string lastTooltip = "";

        public TrayApp()
        {
            data = DataStore.Load();
            sched = new BellScheduler(data);
            sched.Warn += OnWarn;
            sched.Ring += OnRing;

            menu = new ContextMenuStrip();
            menu.Opening += (s, e) => { BuildMenu(); e.Cancel = false; };

            tray = new NotifyIcon
            {
                Icon = AppIcon.Small,
                Text = Program.AppTitle,
                ContextMenuStrip = menu,
                Visible = true,
            };
            tray.DoubleClick += (s, e) => ShowStatusBalloon();

            tickTimer = new Timer { Interval = 1000 };
            tickTimer.Tick += (s, e) => OnTick();
            tickTimer.Start();

            clockTimer = new Timer { Interval = 60 * 60 * 1000 };   // hourly
            clockTimer.Tick += (s, e) => CheckClock();
            clockTimer.Start();

            BellLog.Write($"STARTED  Ascent Bell v{Program.Version}");

            var startup = new Timer { Interval = 2500 };
            startup.Tick += (s, e) =>
            {
                startup.Stop();
                startup.Dispose();
                ShowStatusBalloon();
                CheckClock();
            };
            startup.Start();
        }

        // ---------------------------------------------------------------- tick

        private void OnTick()
        {
            sched.Tick();
            UpdateTooltip();

            bool awake = data.Settings.KeepAwake && sched.InSchoolHours();
            if (awake != keepingAwake)
            {
                keepingAwake = awake;
                NativeMethods.KeepAwake(awake);
            }
        }

        private void UpdateTooltip()
        {
            // NotifyIcon.Text is limited to 63 characters.
            string text;
            if (sched.IsPausedToday) text = "Ascent Bell — paused for today";
            else
            {
                var next = sched.NextBell();
                if (next != null)
                    text = $"Next: {next.Item.Label} at {TimeUtil.Show(next.Due)} (in {Until(next.Due)})";
                else
                {
                    sched.ScheduleFor(sched.Now.Date, out var reason);
                    text = sched.TodayBells().Count > 0 ? "Ascent Bell — no more bells today" : "Ascent Bell — " + reason;
                }
            }
            if (text.Length > 63) text = text.Substring(0, 60) + "...";
            if (text != lastTooltip) { tray.Text = text; lastTooltip = text; }
        }

        private string Until(DateTime due)
        {
            var left = due - sched.Now;
            if (left.TotalMinutes < 1) return "<1 min";
            if (left.TotalHours < 1) return (int)left.TotalMinutes + " min";
            return (int)left.TotalHours + " h " + left.Minutes + " min";
        }

        // ---------------------------------------------------------------- alerts

        private void OnWarn(TodayBell b)
        {
            BellLog.Write($"HEADS-UP  {TimeUtil.Show(b.Due)} {b.Item.Label} (in {b.WarnMinutes} min)");
            new WarningForm(b, () => sched.Now).Show();
        }

        private void OnRing(TodayBell b) => ShowRingAlert(b, isTest: false, manual: false);

        private void ShowRingAlert(TodayBell b, bool isTest, bool manual)
        {
            if (!isTest)
                BellLog.Write($"ALERT  {TimeUtil.Show(b.Due)} {b.Item.Label}" + (manual ? " (opened manually)" : ""));

            var form = new AlertForm(b, data.Settings, isTest, confirmedAt =>
            {
                if (isTest) return;
                sched.MarkRung(b.Due, confirmedAt);
                var late = (int)Math.Max(0, (confirmedAt + sched.ClockOffset - b.Due).TotalSeconds);
                BellLog.Write($"RUNG  {TimeUtil.Show(b.Due)} {b.Item.Label} — confirmed at {confirmedAt:HH:mm:ss} ({late}s after bell time)");
            });
            form.Show();
        }

        // ---------------------------------------------------------------- menu

        private void BuildMenu()
        {
            menu.Items.Clear();

            var header = new ToolStripMenuItem(string.IsNullOrWhiteSpace(data.SchoolName) ? Program.AppTitle : data.SchoolName)
            { Enabled = false, Font = new Font(menu.Font, FontStyle.Bold) };
            menu.Items.Add(header);

            sched.ScheduleFor(sched.Now.Date, out var reason);
            menu.Items.Add(new ToolStripMenuItem("Today: " + reason + (sched.IsPausedToday ? " (PAUSED)" : "")) { Enabled = false });
            menu.Items.Add(new ToolStripSeparator());

            var today = new ToolStripMenuItem("Today's bells");
            var bells = sched.TodayBells();
            if (bells.Count == 0)
                today.DropDownItems.Add(new ToolStripMenuItem("No bells today") { Enabled = false });
            foreach (var b in bells)
            {
                var state = sched.StateOf(b);
                string mark;
                switch (state)
                {
                    case BellState.Rung: mark = "[done]  "; break;
                    case BellState.Alerting: mark = "[alerted]  "; break;
                    case BellState.Missed: mark = "[missed]  "; break;
                    default: mark = ""; break;
                }
                var bell = b;
                var item = new ToolStripMenuItem($"{TimeUtil.Show(b.Due),8}   {b.Item.Label}   {mark}")
                {
                    ToolTipText = "Click to open the ring alert for this bell now",
                    ForeColor = state == BellState.Missed ? Color.DarkRed : state == BellState.Rung ? Color.Gray : SystemColors.ControlText,
                };
                item.Click += (s, e) => ShowRingAlert(bell, isTest: false, manual: true);
                today.DropDownItems.Add(item);
            }
            menu.Items.Add(today);

            menu.Items.Add("Test alert (check sound)", null, (s, e) =>
                ShowRingAlert(new TodayBell { Item = new BellItem { Label = "This is only a test" }, Due = sched.Now }, isTest: true, manual: true));

            var pause = new ToolStripMenuItem("Pause bells for today") { Checked = sched.IsPausedToday };
            pause.Click += (s, e) => TogglePause();
            menu.Items.Add(pause);

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Settings…", null, (s, e) => OpenSettings());
            menu.Items.Add("Open log folder", null, (s, e) => OpenFolder(BellLog.Folder));
            menu.Items.Add("About", null, (s, e) => ShowAbout());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (s, e) => ExitApp());
        }

        private void TogglePause()
        {
            if (sched.IsPausedToday)
            {
                sched.PausedDate = null;
                BellLog.Write("RESUMED  bells resumed for today");
                return;
            }
            if (!PasswordPrompt.Verify(data.Settings.AdminPasswordHash, "Enter the admin password to pause today's bells.")) return;
            if (MessageBox.Show("No bell alerts will be shown for the rest of today.\n\nPause today's bells?",
                    Program.AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            sched.PausedDate = sched.Now.Date;
            BellLog.Write("PAUSED  bells paused for today");
        }

        private void OpenSettings()
        {
            if (settingsOpen) return;
            if (!PasswordPrompt.Verify(data.Settings.AdminPasswordHash, "Enter the admin password to change bell settings.")) return;
            settingsOpen = true;
            try
            {
                using (var f = new SettingsForm(data))
                {
                    if (f.ShowDialog() == DialogResult.OK)
                    {
                        data = f.Result;
                        DataStore.Save(data);
                        sched.Data = data;
                        BellLog.Write("SETTINGS  saved");
                        if (!data.Settings.CheckClock) sched.ClockOffset = TimeSpan.Zero;
                        ShowStatusBalloon();
                    }
                }
            }
            finally { settingsOpen = false; }
        }

        private void ShowStatusBalloon()
        {
            string msg;
            if (sched.IsPausedToday) msg = "Bells are paused for today.";
            else
            {
                sched.ScheduleFor(sched.Now.Date, out var reason);
                var next = sched.NextBell();
                msg = next != null
                    ? $"Today: {reason}\nNext bell: {next.Item.Label} at {TimeUtil.Show(next.Due)}"
                    : $"Today: {reason}\nNo more bells today.";
            }
            tray.ShowBalloonTip(6000, Program.AppTitle + " is running", msg, ToolTipIcon.Info);
        }

        private void ShowAbout()
        {
            MessageBox.Show(
                $"{Program.AppTitle} v{Program.Version}\n\n" +
                "Reminds the bell attender to ring the school bell, using the timings set in Settings.\n\n" +
                $"Schedule file: {DataStore.FilePath}\nLogs: {BellLog.Folder}\n\n" +
                (sched.ClockOffset == TimeSpan.Zero ? "PC clock: OK / not checked" : $"PC clock correction in use: {sched.ClockOffset.TotalMinutes:+0.0;-0.0} min") +
                "\n\nAscent Info Solutions",
                Program.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static void OpenFolder(string path)
        {
            try
            {
                System.IO.Directory.CreateDirectory(path);
                Process.Start("explorer.exe", "\"" + path + "\"");
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, Program.AppTitle); }
        }

        // ---------------------------------------------------------------- clock

        private async void CheckClock()
        {
            if (!data.Settings.CheckClock) { sched.ClockOffset = TimeSpan.Zero; return; }

            var offset = await Task.Run(() => ClockCheck.TryGetOffset());
            if (offset == null) return;                         // offline — keep the last known value

            if (Math.Abs(offset.Value.TotalSeconds) < 60)
            {
                if (sched.ClockOffset != TimeSpan.Zero) BellLog.Write("CLOCK  PC clock is now correct");
                sched.ClockOffset = TimeSpan.Zero;
                return;
            }

            bool changed = Math.Abs((sched.ClockOffset - offset.Value).TotalSeconds) > 30;
            sched.ClockOffset = offset.Value;
            if (changed)
            {
                var mins = Math.Abs(offset.Value.TotalMinutes);
                var dir = offset.Value > TimeSpan.Zero ? "slow" : "fast";
                BellLog.Write($"CLOCK  PC clock is {mins:0.0} min {dir}; alerts use the corrected time");
                tray.ShowBalloonTip(10000, "PC clock is wrong",
                    $"This computer's clock is {mins:0} min {dir}. Bell alerts are using the correct time, " +
                    "but please fix the Windows date & time.", ToolTipIcon.Warning);
            }
        }

        // ---------------------------------------------------------------- exit

        private void ExitApp()
        {
            if (!PasswordPrompt.Verify(data.Settings.AdminPasswordHash, "Enter the admin password to exit Ascent Bell.")) return;
            if (MessageBox.Show("If you exit, there will be NO bell alerts until Ascent Bell is started again.\n\nExit anyway?",
                    Program.AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            BellLog.Write("STOPPED  exited by user");
            ExitThread();
        }

        protected override void ExitThreadCore()
        {
            tickTimer.Stop();
            clockTimer.Stop();
            NativeMethods.KeepAwake(false);
            tray.Visible = false;
            tray.Dispose();
            base.ExitThreadCore();
        }
    }
}
