using AscentSchools.API.Filters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Web.Http;
using System.Web.Http.Cors;

namespace AscentSchools.API
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // ── CORS ──────────────────────────────────────────────────────
            // Tighten origins in production; wildcard is fine for dev only
            var cors = new EnableCorsAttribute(
                origins: "*",
                headers: "*",
                methods: "*"
            )
            { SupportsCredentials = true };
            config.EnableCors(cors);

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
