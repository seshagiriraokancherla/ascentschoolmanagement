using System;
using System.Globalization;

namespace AscentBellTool
{
    public static class TimeUtil
    {
        private static readonly string[] TimeFormats =
        {
            "H:mm", "HH:mm", "h:mm tt", "hh:mm tt", "h:mmtt", "hh:mmtt", "h tt", "htt",
        };

        private static readonly string[] DateFormats =
        {
            "dd-MM-yyyy", "d-M-yyyy", "dd/MM/yyyy", "d/M/yyyy", "dd.MM.yyyy", "yyyy-MM-dd",
        };

        /// Accepts "13:15", "8:50", "8.50" (as on printed timetables) and "1:15 PM".
        public static bool TryParseTime(string s, out TimeSpan time)
        {
            time = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim().ToUpperInvariant().Replace('.', ':');
            if (DateTime.TryParseExact(s, TimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var d))
            {
                time = d.TimeOfDay;
                return true;
            }
            return false;
        }

        public static string To24(TimeSpan t) => $"{t.Hours:00}:{t.Minutes:00}";

        public static string To12(TimeSpan t) => DateTime.Today.Add(t).ToString("h:mm tt", CultureInfo.InvariantCulture);

        public static string Show(DateTime d) => d.ToString("h:mm tt", CultureInfo.InvariantCulture);

        public static bool TryParseDate(string s, out DateTime date)
        {
            date = DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(s)) return false;
            return DateTime.TryParseExact(s.Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        public static string StoreDate(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        public static string ShowDate(DateTime d) => d.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
    }
}
