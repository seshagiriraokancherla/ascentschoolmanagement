using AscentSchools.API.Middleware;
using AscentSchools.Core.DTOs.Mobile.Notifications;
using AscentSchools.Core.Models;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.Mobile;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace AscentSchools.API.Controllers.Mobile
{
    /// <summary>
    /// FCM device-token registration for push notifications.
    /// No auth attribute: the global JwtAuthenticationFilter populates MobileContext
    /// (parent) or TeacherContext (teacher) from the bearer token; we accept either.
    /// </summary>
    [RoutePrefix("mobile/notifications")]
    public class MobileNotificationsController : ApiController
    {
        private readonly DevicePushTokenRepository _repo;

        public MobileNotificationsController()
        {
            _repo = new DevicePushTokenRepository(new TenantConnectionFactory());
        }

        // POST /mobile/notifications/register-token
        [HttpPost, Route("register-token")]
        public HttpResponseMessage Register([FromBody] RegisterPushTokenRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.FcmToken))
                return Fail(HttpStatusCode.BadRequest, "fcmToken is required.");

            var platform = string.IsNullOrWhiteSpace(request.Platform) ? "android" : request.Platform;

            var parent = MobileContext.Current;
            if (parent != null && parent.IsParent && parent.ParentId > 0)
            {
                _repo.UpsertParentToken(request.FcmToken, parent.ParentId, parent.GroupId,
                    parent.SchoolId, parent.DbName, request.ApplicationId, platform);
                return Ok(true, "Registered.");
            }

            var teacher = TeacherContext.Current;
            if (teacher != null && teacher.UserId > 0)
            {
                _repo.UpsertTeacherToken(request.FcmToken, teacher.UserId, teacher.GroupId,
                    teacher.SchoolId, teacher.DbName, request.ApplicationId, platform);
                return Ok(true, "Registered.");
            }

            return Fail(HttpStatusCode.Unauthorized, "A parent or teacher token is required.");
        }

        // POST /mobile/notifications/unregister-token
        // No auth required — a logging-out client just removes its token by value.
        [HttpPost, Route("unregister-token")]
        public HttpResponseMessage Unregister([FromBody] RegisterPushTokenRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.FcmToken))
                return Fail(HttpStatusCode.BadRequest, "fcmToken is required.");
            _repo.DeleteToken(request.FcmToken);
            return Ok(true, "Unregistered.");
        }

        private HttpResponseMessage Ok<T>(T data, string message = null) =>
            Request.CreateResponse(HttpStatusCode.OK, ApiResponse<T>.Ok(data, message));

        private HttpResponseMessage Fail(HttpStatusCode code, string message) =>
            Request.CreateResponse(code, new { success = false, message });
    }
}
