using System.Web.Mvc;

namespace HR.Web
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            // Enforce authentication globally; controllers/actions can opt-out with [AllowAnonymous]
            filters.Add(new AuthorizeAttribute());
        }
    }
}


