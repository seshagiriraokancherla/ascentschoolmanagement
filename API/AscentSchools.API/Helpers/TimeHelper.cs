using System;

namespace AscentSchools.API.Helpers
{
    /// <summary>
    /// Central time helper. The production server runs in US Eastern time, but the
    /// application serves schools in India. Any server-stamped business date (e.g. a
    /// fee payment date) must be computed in India Standard Time — otherwise a payment
    /// made in the early IST morning is stamped with the previous calendar day.
    /// </summary>
    public static class TimeHelper
    {
        private static readonly TimeZoneInfo Ist =
            TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

        /// <summary>Current wall-clock time in India Standard Time.</summary>
        public static DateTime IstNow()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Ist);
        }

        /// <summary>Today's date in India Standard Time (time component stripped).</summary>
        public static DateTime IstToday()
        {
            return IstNow().Date;
        }
    }
}
