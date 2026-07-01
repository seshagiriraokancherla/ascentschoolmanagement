using AscentSchools.Core.DTOs.Mobile.AppConfig;
using AscentSchools.Core.Models;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.Mobile;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.Mobile
{
    [RoutePrefix("mobile/app")]
    public class MobileAppConfigController : ApiController
    {
        private readonly AppConfigRepository _appConfig;

        public MobileAppConfigController()
        {
            _appConfig = new AppConfigRepository(new TenantConnectionFactory());
        }

        // GET mobile/app/config?applicationId=in.educare.app&platform=android&versionCode=12
        // AllowAnonymous: called on cold start before any login / school selection.
        [HttpGet, Route("config"), AllowAnonymous]
        public HttpResponseMessage GetConfig(string applicationId = null, string platform = "android", int versionCode = 0)
        {
            if (string.IsNullOrWhiteSpace(applicationId)) applicationId = "*";
            if (string.IsNullOrWhiteSpace(platform))      platform      = "android";

            var cfg = _appConfig.GetConfig(applicationId.Trim(), platform.Trim());

            // Nothing configured -> fail open (never block on missing config).
            if (cfg == null)
                return Request.CreateResponse(HttpStatusCode.OK,
                    ApiResponse<AppVersionStatusDto>.Ok(new AppVersionStatusDto { UpdateRequired = false, UpdateAvailable = false }));

            var status = new AppVersionStatusDto
            {
                MinSupportedVersionCode = cfg.MinSupportedVersionCode,
                LatestVersionCode       = cfg.LatestVersionCode,
                Message                 = cfg.UpdateMessage,
                StoreUrl                = cfg.StoreUrl,
                // Auto-update prompts are opt-in per app_config row. When disabled,
                // never signal an update (the app shows a manual "Update App" button instead).
                UpdateRequired          = cfg.AutoUpdateEnabled
                                          && versionCode < cfg.MinSupportedVersionCode,
                UpdateAvailable         = cfg.AutoUpdateEnabled
                                          && versionCode >= cfg.MinSupportedVersionCode
                                          && versionCode < cfg.LatestVersionCode,
            };

            return Request.CreateResponse(HttpStatusCode.OK, ApiResponse<AppVersionStatusDto>.Ok(status));
        }
    }
}
