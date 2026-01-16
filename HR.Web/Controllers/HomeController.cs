using System.Web.Mvc;

namespace HR.Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Test()
        {
            return View();
        }
        
        public ActionResult Index()
        {
            return RedirectToAction("Index", "Positions");
        }
    }
}
