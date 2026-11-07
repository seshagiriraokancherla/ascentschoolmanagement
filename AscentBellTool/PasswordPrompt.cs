using System;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace AscentBellTool
{
    /// Optional admin password so the bell attender can't accidentally exit or change the timings.
    public static class PasswordPrompt
    {
        public static string Hash(string password)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes("AscentBell:" + password));
                var sb = new StringBuilder();
                foreach (var b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        /// True if no password is set, or the user typed the right one.
        public static bool Verify(string hash, string purpose)
        {
            if (string.IsNullOrEmpty(hash)) return true;
            var typed = Ask("Admin password", purpose, confirm: false);
            if (typed == null) return false;
            if (Hash(typed) == hash) return true;
            MessageBox.Show("Wrong password.", Program.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        /// Asks for a new password (typed twice). Returns null if cancelled.
        public static string AskNew()
        {
            var p = Ask("Set admin password", "Enter a new password (at least 4 characters).", confirm: true);
            return p;
        }

        private static string Ask(string title, string message, bool confirm)
        {
            using (var f = new Form())
            {
                f.Text = title;
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.StartPosition = FormStartPosition.CenterScreen;
                f.MinimizeBox = f.MaximizeBox = false;
                f.TopMost = true;
                f.ShowInTaskbar = false;
                f.Font = new Font("Segoe UI", 10f);
                f.ClientSize = new Size(380, confirm ? 190 : 140);
                f.Icon = AppIcon.Large;

                var lbl = new Label { Text = message, Left = 14, Top = 12, Width = 350, Height = 40 };
                var txt1 = new TextBox { Left = 14, Top = 56, Width = 350, UseSystemPasswordChar = true };
                var txt2 = new TextBox { Left = 14, Top = 100, Width = 350, UseSystemPasswordChar = true, Visible = confirm };
                var lbl2 = new Label { Text = "Type it again:", Left = 14, Top = 80, Width = 350, Visible = confirm };
                int btnTop = confirm ? 142 : 96;
                var ok = new Button { Text = "OK", Left = 188, Top = btnTop, Width = 85, DialogResult = DialogResult.OK };
                var cancel = new Button { Text = "Cancel", Left = 279, Top = btnTop, Width = 85, DialogResult = DialogResult.Cancel };
                f.Controls.AddRange(new Control[] { lbl, txt1, lbl2, txt2, ok, cancel });
                f.AcceptButton = ok;
                f.CancelButton = cancel;

                while (true)
                {
                    if (f.ShowDialog() != DialogResult.OK) return null;
                    if (!confirm) return txt1.Text;
                    if (txt1.Text.Length < 4)
                        MessageBox.Show("Use at least 4 characters.", Program.AppTitle);
                    else if (txt1.Text != txt2.Text)
                        MessageBox.Show("The two passwords don't match.", Program.AppTitle);
                    else
                        return txt1.Text;
                }
            }
        }
    }
}
