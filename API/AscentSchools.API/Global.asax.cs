using System.Web;
using System.Web.Http;

namespace AscentSchools.API
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }

        protected void Application_BeginRequest()
        {
            var origin = HttpContext.Current.Request.Headers["Origin"];

            if (!string.IsNullOrEmpty(origin))
            {
                HttpContext.Current.Response.Headers.Set("Access-Control-Allow-Origin",      origin);
                HttpContext.Current.Response.Headers.Set("Access-Control-Allow-Credentials", "true");
                HttpContext.Current.Response.Headers.Set("Access-Control-Allow-Methods",     "GET, POST, PUT, DELETE, OPTIONS");
                HttpContext.Current.Response.Headers.Set("Access-Control-Allow-Headers",     "Content-Type, Authorization, X-Subdomain");
                HttpContext.Current.Response.Headers.Set("Access-Control-Expose-Headers",    "Set-Cookie");
            }

            // Short-circuit OPTIONS preflight immediately — do not run auth filters
            if (HttpContext.Current.Request.HttpMethod == "OPTIONS")
            {
                HttpContext.Current.Response.StatusCode = 200;
                HttpContext.Current.Response.End();
            }
        }
    }
}
