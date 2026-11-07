using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace AscentBellTool
{
    /// Loads/saves C:\ProgramData\AscentBell\bells.json. Writes are atomic (temp file + replace,
    /// keeping a .bak), so a power cut mid-save can't leave the school without a schedule.
    public static class DataStore
    {
        public static readonly string Folder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "AscentBell");

        public static string FilePath => Path.Combine(Folder, "bells.json");
        public static string BackupPath => FilePath + ".bak";

        public static AppData Load()
        {
            Directory.CreateDirectory(Folder);

            if (!File.Exists(FilePath))
            {
                var seeded = Seed.DePaul();
                Save(seeded);
                BellLog.Write("First run — created schedule from the built-in De Paul timings.");
                return seeded;
            }

            try
            {
                return Normalize(FromJson(File.ReadAllText(FilePath, Encoding.UTF8)));
            }
            catch (Exception ex)
            {
                var bad = Path.Combine(Folder, $"bells.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}.json");
                try { File.Copy(FilePath, bad, true); } catch { /* best effort */ }
                BellLog.Write("ERROR  bells.json unreadable (" + ex.Message + ") — copied to " + bad);

                AppData recovered = null;
                if (File.Exists(BackupPath))
                {
                    try { recovered = Normalize(FromJson(File.ReadAllText(BackupPath, Encoding.UTF8))); }
                    catch { recovered = null; }
                }

                MessageBox.Show(
                    "The bell schedule file was damaged.\n\n" +
                    (recovered != null
                        ? "The previous saved copy has been restored. Please check Settings."
                        : "The default schedule has been loaded. Please check Settings."),
                    Program.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);

                var data = recovered ?? Seed.DePaul();
                Save(data);
                return data;
            }
        }

        public static void Save(AppData data)
        {
            Directory.CreateDirectory(Folder);
            var tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, ToJson(data), Encoding.UTF8);
            if (File.Exists(FilePath))
                File.Replace(tmp, FilePath, BackupPath, true);
            else
                File.Move(tmp, FilePath);
        }

        public static string ToJson(AppData data) => PrettyPrint(new JavaScriptSerializer().Serialize(data));

        public static AppData FromJson(string json)
        {
            var data = new JavaScriptSerializer().Deserialize<AppData>(json);
            if (data == null) throw new InvalidDataException("Empty schedule file.");
            return data;
        }

        public static AppData Clone(AppData data) => Normalize(FromJson(ToJson(data)));

        private static AppData Normalize(AppData d)
        {
            if (d.Settings == null) d.Settings = new AppSettings();
            if (d.Schedules == null) d.Schedules = new System.Collections.Generic.List<BellSchedule>();
            if (d.Holidays == null) d.Holidays = new System.Collections.Generic.List<Holiday>();
            foreach (var s in d.Schedules)
            {
                if (s.Days == null) s.Days = new System.Collections.Generic.List<int>();
                if (s.Items == null) s.Items = new System.Collections.Generic.List<BellItem>();
                s.Items = s.Items.Where(i => i != null).ToList();
            }
            d.Holidays = d.Holidays.Where(h => h != null).ToList();
            return d;
        }

        /// Minimal JSON indenter so the file is readable if anyone opens it in Notepad.
        private static string PrettyPrint(string json)
        {
            var sb = new StringBuilder();
            int indent = 0;
            bool inString = false;
            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                if (c == '"')
                {
                    int slashes = 0;
                    for (int j = i - 1; j >= 0 && json[j] == '\\'; j--) slashes++;
                    if (slashes % 2 == 0) inString = !inString;
                }
                if (inString) { sb.Append(c); continue; }
                switch (c)
                {
                    case '{':
                    case '[':
                        sb.Append(c).AppendLine().Append(new string(' ', ++indent * 2));
                        break;
                    case '}':
                    case ']':
                        sb.AppendLine().Append(new string(' ', --indent * 2)).Append(c);
                        break;
                    case ',':
                        sb.Append(c).AppendLine().Append(new string(' ', indent * 2));
                        break;
                    case ':':
                        sb.Append(": ");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
