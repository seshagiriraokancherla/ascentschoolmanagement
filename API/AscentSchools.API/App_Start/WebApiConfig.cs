using AscentSchools.API.Filters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Web.Http;

namespace AscentSchools.API
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // CORS is handled in Global.asax Application_BeginRequest (pipeline level)
            // so that OPTIONS preflight is short-circuited before auth filters run.

            // Return full exception detail in all environments (aids production debugging)
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;

            // ── JSON formatting ───────────────────────────────────────────
            var json = config.Formatters.JsonFormatter;
            json.SerializerSettings.ContractResolver  = new CamelCasePropertyNamesContractResolver();
            json.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            json.SerializerSettings.DateFormatString  = "yyyy-MM-dd";
            config.Formatters.Remove(config.Formatters.XmlFormatter);

            // ── Filters ───────────────────────────────────────────────────
            config.Filters.Add(new JwtAuthenticationFilter());

            // ── Routes ────────────────────────────────────────────────────
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
