using System;
using System.Threading;
using System.Windows.Forms;

namespace AscentBellTool
{
    internal static class Program
    {
        public const string Version = "1.0";
        public const string AppTitle = "Ascent Bell";

        // Must match AppMutex in Installer\AscentBell.iss (the installer uses it to
        // ask the user to close the app before upgrading/uninstalling).
        public const string MutexName = "AscentBellTool_SingleInstance";

        [STAThread]
        static void Main()
        {
            bool createdNew;
            using (var mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show(
                        "Ascent Bell is already running.\n\nLook for the bell icon near the clock " +
                        "(bottom-right of the screen; it may be inside the ^ arrow).",
                        AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Never let one bad event kill the app — a dead bell app is the worst outcome.
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (s, e) => ReportError(e.Exception);
                AppDomain.CurrentDomain.UnhandledException += (s, e) => ReportError(e.ExceptionObject as Exception);

                Application.Run(new TrayApp());
                GC.KeepAlive(mutex);
            }
        }

        private static void ReportError(Exception ex)
        {
            BellLog.Write("ERROR  " + (ex?.ToString() ?? "unknown error"));
            MessageBox.Show(
                (ex?.Message ?? "Unknown error") + "\n\nAscent Bell will keep running. Details are in the log folder.",
                AppTitle + " — error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
