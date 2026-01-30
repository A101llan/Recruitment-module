using System;
using System.Web.Mvc;

namespace HR.Web.Controllers
{
    public class SimpleReportsController : Controller
    {
        // GET: SimpleReports
        public ActionResult Index()
        {
            return Content("SimpleReports Index is working! Reports controller issue is isolated.");
        }
    }
}
