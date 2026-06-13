using System;
using System.Windows.Forms;

namespace AscentSyncTool
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Surface any unhandled error instead of the app silently failing to open.
            Application.ThreadException += (s, e) => ShowFatal(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => ShowFatal(e.ExceptionObject as Exception);

            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                ShowFatal(ex);
            }
        }

        private static void ShowFatal(Exception ex)
        {
            MessageBox.Show(
                (ex?.Message ?? "Unknown error") +
                "\n\nCheck AscentSyncTool.exe.config (LegacyDb connection string and ApiKey).",
                "Ascent Sync Tool — error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
