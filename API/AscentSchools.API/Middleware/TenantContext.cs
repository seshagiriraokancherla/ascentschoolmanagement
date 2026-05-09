using System.Web;

namespace AscentSchools.API.Middleware
{
    /// <summary>
    /// Holds tenant information for the current HTTP request.
    /// Populated by JwtAuthenticationFilter after token validation.
    /// Accessed anywhere in the request pipeline via TenantContext.Current.
    /// </summary>
    public class TenantContext
    {
        private const string HttpContextKey = "TenantContext";

        public int     UserId       { get; set; }
        public string  FullName     { get; set; }
        public int     GroupId      { get; set; }
        public int     SchoolId     { get; set; }
        public string  TenantDbName { get; set; }   // e.g. "ascent_group_3"
        public string[] Permissions { get; set; }   // e.g. ["STUDENT_FEE.COLLECT", ...]

        /// <summary>Gets or sets the TenantContext for the current HTTP request.</summary>
        public static TenantContext Current
        {
            get => HttpContext.Current?.Items[HttpContextKey] as TenantContext;
            set
            {
                if (HttpContext.Current != null)
                    HttpContext.Current.Items[HttpContextKey] = value;
            }
        }

        public bool HasPermission(string permissionCode)
        {
            if (Permissions == null) return false;
            foreach (var p in Permissions)
                if (p == permissionCode) return true;
            return false;
        }
    }
}
