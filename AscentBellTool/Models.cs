using System.Collections.Generic;

namespace AscentBellTool
{
    // Everything the app stores, serialised to C:\ProgramData\AscentBell\bells.json.
    public class AppData
    {
        public int FormatVersion { get; set; } = 1;
        public string SchoolName { get; set; } = "";
        public AppSettings Settings { get; set; } = new AppSettings();
        public List<BellSchedule> Schedules { get; set; } = new List<BellSchedule>();
        public List<Holiday> Holidays { get; set; } = new List<Holiday>();
    }

    public class AppSettings
    {
        /// Minutes before a bell to show the "bell coming up" heads-up (0 = none).
        public int DefaultWarnMinutes { get; set; } = 2;
        /// Seconds an unconfirmed ring alert waits before going to full volume.
        public int EscalateAfterSeconds { get; set; } = 60;
        /// Ring alerts unmute the PC and raise volume to at least this.
        public int MinVolumePercent { get; set; } = 70;
        /// Optional .wav to use instead of the built-in bell sound.
        public string CustomSoundFile { get; set; }
        /// SHA-256 of the admin password (protects Settings and Exit). Empty = no password.
        public string AdminPasswordHash { get; set; }
        /// Stop Windows sleeping / blanking the screen during school hours.
        public bool KeepAwake { get; set; } = true;
        /// Compare the PC clock with internet time when online, and correct alerts for drift.
        public bool CheckClock { get; set; } = true;
    }

    public class BellSchedule
    {
        public string Name { get; set; } = "";
        public bool Active { get; set; } = true;
        /// System.DayOfWeek values (0 = Sunday … 6 = Saturday).
        public List<int> Days { get; set; } = new List<int>();
        public List<BellItem> Items { get; set; } = new List<BellItem>();
    }

    public class BellItem
    {
        /// 24-hour "HH:mm".
        public string Time { get; set; } = "";
        public string Label { get; set; } = "";
        /// Per-bell heads-up override; null = use AppSettings.DefaultWarnMinutes.
        public int? WarnMinutes { get; set; }
    }

    public class Holiday
    {
        /// "yyyy-MM-dd".
        public string Date { get; set; } = "";
        public string Name { get; set; } = "";
    }

    public static class Seed
    {
        /// First-run data: De Paul EM School's printed timings, plus the end-of-day bell.
        public static AppData DePaul()
        {
            var regular = new BellSchedule
            {
                Name = "Regular",
                Active = true,
                Days = new List<int> { 1, 2, 3, 4, 5, 6 },   // Mon–Sat
            };
            void Add(string t, string label) => regular.Items.Add(new BellItem { Time = t, Label = label });

            Add("08:50", "1st bell");
            Add("08:55", "2nd bell");
            Add("09:00", "3rd bell - Assembly");
            Add("09:10", "1st period");
            Add("09:55", "2nd period");
            Add("10:35", "Tiffin break");
            Add("10:50", "3rd period");
            Add("11:30", "4th period");
            Add("12:10", "5th period");
            Add("12:50", "Lunch break");
            Add("13:15", "6th period");
            Add("13:55", "7th period");
            Add("14:35", "8th period");
            Add("15:15", "School over");

            return new AppData { SchoolName = "De Paul EM School", Schedules = { regular } };
        }
    }
}
