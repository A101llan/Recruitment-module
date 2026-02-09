using System.Web.Mvc;
using System.Web.Routing;

namespace HR.Web
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("Content/{*pathInfo}");
            routes.IgnoreRoute("Scripts/{*pathInfo}");

            routes.MapRoute(
                name: "SuperAdminLegacy",
                url: "SuperAdmin/{action}/{id}",
                defaults: new { controller = "Companies", action = "Index", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Positions", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}


