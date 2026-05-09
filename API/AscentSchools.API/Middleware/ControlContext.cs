using System.Web;

namespace AscentSchools.API.Middleware
{
    /// <summary>
    /// Per-request identity for control app users.
    /// Populated by ControlAuthAttribute on each control endpoint.
    /// </summary>
    public class ControlContext
    {
        public int    UserId   { get; set; }
        public string FullName { get; set; }
        public string Role     { get; set; }

        private const string Key = "ControlContext";

        public static ControlContext Current
        {
            get => HttpContext.Current?.Items[Key] as ControlContext;
            set { if (HttpContext.Current != null) HttpContext.Current.Items[Key] = value; }
        }
    }
}
