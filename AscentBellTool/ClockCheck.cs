using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;

namespace AscentBellTool
{
    /// Old PCs often have a drifting clock. When the PC happens to be online we read the
    /// standard HTTP "Date" header from a well-known site and work out how far off the clock is.
    /// Offline → null, and the app simply trusts the PC clock.
    public static class ClockCheck
    {
        private static readonly string[] Urls = { "http://www.google.com/", "http://www.microsoft.com/" };

        /// Internet time minus PC time, or null if it couldn't be determined.
        public static TimeSpan? TryGetOffset()
        {
            foreach (var url in Urls)
            {
                try
                {
                    var req = (HttpWebRequest)WebRequest.Create(url);
                    req.Method = "HEAD";
                    req.Timeout = 5000;
                    req.AllowAutoRedirect = false;
                    var sw = Stopwatch.StartNew();
                    using (var resp = (HttpWebResponse)req.GetResponse())
                    {
                        sw.Stop();
                        var header = resp.Headers["Date"];
                        if (string.IsNullOrEmpty(header)) continue;
                        if (!DateTime.TryParseExact(header, "r", CultureInfo.InvariantCulture,
                                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var serverUtc))
                            continue;
                        // The header has 1-second resolution; half the round trip is a fair midpoint.
                        var pcUtc = DateTime.UtcNow - TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds / 2.0);
                        return serverUtc - pcUtc;
                    }
                }
                catch (WebException ex) when (ex.Response is HttpWebResponse r && !string.IsNullOrEmpty(r.Headers["Date"]))
                {
                    // A 3xx/4xx still carries a valid Date header.
                    if (DateTime.TryParseExact(r.Headers["Date"], "r", CultureInfo.InvariantCulture,
                            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var serverUtc))
                        return serverUtc - DateTime.UtcNow;
                }
                catch
                {
                    // offline / blocked — try the next URL
                }
            }
            return null;
        }
    }
}
