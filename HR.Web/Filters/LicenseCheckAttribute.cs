using System;
using System.Web;
using System.Web.Mvc;
using HR.Web.Services;

namespace HR.Web.Filters
{
    public class LicenseCheckAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Skip license check for SuperAdmin
            var tenantService = new TenantService();
            
            if (tenantService.IsSuperAdmin())
            {
                base.OnActionExecuting(filterContext);
                return;
            }

            // Skip license check for anonymous users (they'll be redirected to login)
            if (!HttpContext.Current.User.Identity.IsAuthenticated)
            {
                base.OnActionExecuting(filterContext);
                return;
            }

            // Check if company license is active
            if (!tenantService.IsCurrentCompanyLicenseActive())
            {
                // Redirect to license expired page
                filterContext.Result = new RedirectResult("~/Account/LicenseExpired");
                return;
            }

            base.OnActionExecuting(filterContext);
        }
    }
}
