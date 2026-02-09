using System.Web.Mvc;

namespace HR.Web
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            
            // Global license check for multi-tenant enforcement
            filters.Add(new HR.Web.Filters.LicenseCheckAttribute());

            // Prevent browser caching for back-button security
            filters.Add(new HR.Web.Filters.NoCacheAttribute());
            
            // Global authentication is removed to allow anonymous browsing.
            // controllers/actions will specify their own [Authorize] requirements.
            // filters.Add(new AuthorizeAttribute());
        }
    }
}


