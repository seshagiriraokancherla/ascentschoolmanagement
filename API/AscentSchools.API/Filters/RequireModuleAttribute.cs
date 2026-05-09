using AscentSchools.API.Middleware;
using AscentSchools.Data.ConnectionFactory;
using AscentSchools.Data.Repositories.Control;
using System;
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace AscentSchools.API.Filters
{
    /// <summary>
    /// Verifies the requested module is enabled for the current group + school branch.
    /// Returns 403 if the module is off at group level OR overridden off at branch level.
    ///
    /// Usage (declare before RequirePermission so module check runs first):
    ///   [RequireModule(ModuleCodes.StudentFee)]
    ///   [RequirePermission(PermissionCodes.StudentFee.Collect)]
    ///   public HttpResponseMessage CollectFee(...) { ... }
    ///
    /// Hierarchy enforced by the SQL query (see ModuleRepository.IsModuleEnabled):
    ///   group disabled  → 403 always (hard block, branch cannot override)
    ///   school row set  → use school setting
    ///   no school row   → inherit group setting (default: enabled)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RequireModuleAttribute : AuthorizationFilterAttribute
    {
        private readonly string _moduleCode;

        public RequireModuleAttribute(string moduleCode)
        {
            _moduleCode = moduleCode;
        }

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            var tenant = TenantContext.Current;

            if (tenant == null)
            {
                actionContext.Response = Deny(actionContext.Request,
                    HttpStatusCode.Unauthorized, "Authentication required.");
                return;
            }

            var repo    = new ModuleRepository(new TenantConnectionFactory());
            var enabled = repo.IsModuleEnabled(tenant.GroupId, tenant.SchoolId, _moduleCode);

            if (!enabled)
            {
                actionContext.Response = Deny(actionContext.Request,
                    HttpStatusCode.Forbidden,
                    $"The '{_moduleCode}' module is not enabled for this school.");
            }
        }

        private static HttpResponseMessage Deny(HttpRequestMessage req, HttpStatusCode code, string msg)
            => req.CreateResponse(code, new { success = false, message = msg });
    }
}
