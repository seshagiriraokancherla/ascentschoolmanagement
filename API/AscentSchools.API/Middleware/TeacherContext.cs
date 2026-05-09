using System.Web;

namespace AscentSchools.API.Middleware
{
    /// <summary>
    /// Holds teacher session state for the current HTTP request.
    /// Populated by JwtAuthenticationFilter for tokenType=teacher.
    /// </summary>
    public class TeacherContext
    {
        private const string HttpContextKey = "TeacherContext";

        public int    UserId   { get; set; }
        public int    SchoolId { get; set; }
        public int    GroupId  { get; set; }
        public string DbName   { get; set; }
        public string FullName { get; set; }

        public static TeacherContext Current
        {
            get => HttpContext.Current?.Items[HttpContextKey] as TeacherContext;
            set { if (HttpContext.Current != null) HttpContext.Current.Items[HttpContextKey] = value; }
        }
    }
}
