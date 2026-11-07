using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace AscentBellTool
{
    public static class AppIcon
    {
        private static Icon small, large;

        public static Icon Small => small ?? (small = Load(SystemInformation.SmallIconSize));
        public static Icon Large => large ?? (large = Load(SystemInformation.IconSize));

        private static Icon Load(Size size)
        {
            try
            {
                using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("AscentBellTool.bell.ico"))
                    if (s != null) return new Icon(s, size);
            }
            catch { /* fall through */ }
            return SystemIcons.Information;
        }
    }
}
