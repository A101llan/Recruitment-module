using System;
using System.Web.Mvc;

namespace HR.Web.Controllers
{
    public class ReportingController : Controller
    {
        // GET: Reporting
        public ActionResult Index()
        {
            return Content("Reporting controller is working!");
        }
    }
}
