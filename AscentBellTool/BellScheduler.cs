using System;
using System.Collections.Generic;
using System.Linq;

namespace AscentBellTool
{
    public class TodayBell
    {
        public BellItem Item;
        public DateTime Due;
        public int WarnMinutes;
    }

    public enum BellState { Upcoming, Alerting, Rung, Missed }

    /// Decides which bell is due. Ticked once a second from the UI thread by TrayApp.
    public class BellScheduler
    {
        /// A bell still fires if the app notices it up to this late (PC woke up, app just started).
        public static readonly TimeSpan Grace = TimeSpan.FromMinutes(2);

        public AppData Data { get; set; }

        /// Correction for a wrong PC clock (internet time − PC time). Zero when unknown/fine.
        public TimeSpan ClockOffset { get; set; } = TimeSpan.Zero;

        public DateTime? PausedDate { get; set; }

        public event Action<TodayBell> Warn;
        public event Action<TodayBell> Ring;

        private readonly HashSet<string> fired = new HashSet<string>();
        private readonly Dictionary<string, DateTime> rungAt = new Dictionary<string, DateTime>();
        private DateTime currentDate = DateTime.MinValue;

        public BellScheduler(AppData data) { Data = data; }

        public DateTime Now => DateTime.Now + ClockOffset;

        public bool IsPausedToday => PausedDate.HasValue && PausedDate.Value == Now.Date;

        /// Today's schedule, or null with the reason (holiday, no schedule for this weekday).
        public BellSchedule ScheduleFor(DateTime date, out string reason)
        {
            var holiday = Data.Holidays.FirstOrDefault(h =>
                TimeUtil.TryParseDate(h.Date, out var d) && d.Date == date.Date);
            if (holiday != null)
            {
                reason = "Holiday" + (string.IsNullOrWhiteSpace(holiday.Name) ? "" : " — " + holiday.Name);
                return null;
            }

            var sched = Data.Schedules.FirstOrDefault(s => s.Active && s.Days.Contains((int)date.DayOfWeek));
            if (sched == null)
            {
                reason = "No bells on " + date.DayOfWeek;
                return null;
            }

            reason = sched.Name;
            return sched;
        }

        public List<TodayBell> BellsFor(DateTime date)
        {
            var sched = ScheduleFor(date, out _);
            var list = new List<TodayBell>();
            if (sched == null) return list;
            foreach (var item in sched.Items)
            {
                if (!TimeUtil.TryParseTime(item.Time, out var t)) continue;
                list.Add(new TodayBell
                {
                    Item = item,
                    Due = date.Date + t,
                    WarnMinutes = Math.Max(0, item.WarnMinutes ?? Data.Settings.DefaultWarnMinutes),
                });
            }
            return list.OrderBy(b => b.Due).ToList();
        }

        public List<TodayBell> TodayBells() => BellsFor(Now.Date);

        public TodayBell NextBell()
        {
            var now = Now;
            return TodayBells().FirstOrDefault(b => b.Due > now);
        }

        public void Tick()
        {
            var now = Now;
            if (now.Date != currentDate)
            {
                currentDate = now.Date;
                fired.Clear();
                rungAt.Clear();
            }
            if (IsPausedToday) return;

            foreach (var b in TodayBells())
            {
                var key = Key(b.Due);

                if (b.WarnMinutes > 0)
                {
                    var warnAt = b.Due.AddMinutes(-b.WarnMinutes);
                    if (now >= warnAt && now < b.Due && fired.Add(key + "|W"))
                        Warn?.Invoke(b);
                }

                if (now >= b.Due && now < b.Due + Grace && fired.Add(key + "|R"))
                    Ring?.Invoke(b);
            }
        }

        public void MarkRung(DateTime due, DateTime at) => rungAt[Key(due)] = at;

        public BellState StateOf(TodayBell b)
        {
            var key = Key(b.Due);
            if (rungAt.ContainsKey(key)) return BellState.Rung;
            if (fired.Contains(key + "|R")) return BellState.Alerting;
            if (Now >= b.Due + Grace) return BellState.Missed;
            return BellState.Upcoming;
        }

        /// From 30 min before the first bell to 10 min after the last one, on a school day.
        public bool InSchoolHours()
        {
            if (IsPausedToday) return false;
            var bells = TodayBells();
            if (bells.Count == 0) return false;
            var now = Now;
            return now >= bells.First().Due.AddMinutes(-30) && now <= bells.Last().Due.AddMinutes(10);
        }

        private static string Key(DateTime due) => due.ToString("yyyy-MM-dd HH:mm");
    }
}
