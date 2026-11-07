using System;
using System.IO;
using System.Linq;
using System.Text;

namespace AscentBellTool
{
    /// One plain-text file per day in C:\ProgramData\AscentBell\logs (open with Notepad).
    /// Records each alert, when it was confirmed, missed bells and errors. Keeps 180 days.
    public static class BellLog
    {
        public static string Folder => Path.Combine(DataStore.Folder, "logs");

        private static readonly object Gate = new object();
        private static DateTime lastPurge = DateTime.MinValue;

        public static void Write(string message)
        {
            try
            {
                lock (Gate)
                {
                    Directory.CreateDirectory(Folder);
                    var file = Path.Combine(Folder, DateTime.Now.ToString("yyyy-MM-dd") + ".txt");
                    File.AppendAllText(file, DateTime.Now.ToString("HH:mm:ss") + "  " + message + Environment.NewLine, Encoding.UTF8);
                    if (lastPurge.Date != DateTime.Today) Purge();
                }
            }
            catch
            {
                // Logging must never break the alerts.
            }
        }

        private static void Purge()
        {
            lastPurge = DateTime.Today;
            var cutoff = DateTime.Today.AddDays(-180);
            foreach (var f in Directory.GetFiles(Folder, "*.txt").Where(f => File.GetLastWriteTime(f) < cutoff))
            {
                try { File.Delete(f); } catch { /* ignore */ }
            }
        }
    }
}
