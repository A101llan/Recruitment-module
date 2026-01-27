using System.Linq;
using System.Web.Mvc;
using HR.Web.Data;

namespace HR.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly UnitOfWork _uow = new UnitOfWork();

        public ActionResult Index()
        {
            var openPositions = _uow.Positions.GetAll().Count(p => p.IsOpen);
            var pendingApplications = _uow.Applications.GetAll().Count(a => a.Status == "Interviewing");
            var scheduledInterviews = _uow.Interviews.GetAll().Count();

            ViewBag.OpenPositions = openPositions;
            ViewBag.PendingApplications = pendingApplications;
            ViewBag.ScheduledInterviews = scheduledInterviews;

            return View();
        }
    }
}







