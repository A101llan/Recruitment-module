using System.Web.Mvc;

namespace HR.Web
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            // Global authentication is removed to allow anonymous browsing.
            // controllers/actions will specify their own [Authorize] requirements.
            // filters.Add(new AuthorizeAttribute());
        }
    }
}


