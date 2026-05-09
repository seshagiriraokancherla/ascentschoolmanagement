using System.Web;

namespace AscentSchools.API.Middleware
{
    /// <summary>
    /// Holds mobile user information for the current HTTP request.
    /// Populated by JwtAuthenticationFilter for tokenType=student and tokenType=parent.
    /// </summary>
    public class MobileContext
    {
        private const string HttpContextKey = "MobileContext";

        public string TokenType   { get; set; }   // "student" or "parent"
        public int    AccountId   { get; set; }   // student_mobile_accounts.account_id (student only)
        public long   StudentId   { get; set; }   // students.student_id (0 = parent not yet selected child)
        public int    ParentId    { get; set; }   // parent_accounts.parent_id (parent only)
        public int    GroupId     { get; set; }
        public int    SchoolId    { get; set; }
        public string DbName      { get; set; }
        public string DisplayName { get; set; }
        public string ClassName   { get; set; }
        public string AdmissionNo { get; set; }

        /// <summary>True when child context (DbName) is present — required for data endpoints.</summary>
        public bool HasChildContext => !string.IsNullOrEmpty(DbName);

        public bool IsStudent => TokenType == "student";
        public bool IsParent  => TokenType == "parent";

        public static MobileContext Current
        {
            get => HttpContext.Current?.Items[HttpContextKey] as MobileContext;
            set
            {
                if (HttpContext.Current != null)
                    HttpContext.Current.Items[HttpContextKey] = value;
            }
        }
    }
}
