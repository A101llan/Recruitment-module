using System;
using System.Linq;
using System.Web.Mvc;
using HR.Web.Data;
using HR.Web.Models;
using HR.Web.Services;

namespace HR.Web.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class DashboardController : Controller
    {
        private readonly UnitOfWork _uow = new UnitOfWork();
        private readonly AuditService _auditService = new AuditService();

        public ActionResult Index()
        {
            var openPositions = _uow.Positions.GetAll().Count(p => p.IsOpen);
            var pendingApplications = _uow.Applications.GetAll().Count(a => a.Status == "Interviewing");
            var scheduledInterviews = _uow.Interviews.GetAll().Count();
            var totalUsers = _uow.Users.GetAll().Count();

            ViewBag.OpenPositions = openPositions;
            ViewBag.PendingApplications = pendingApplications;
            ViewBag.ScheduledInterviews = scheduledInterviews;
            ViewBag.TotalUsers = totalUsers;

            ViewBag.PendingImpersonationRequests = _uow.ImpersonationRequests.GetAll()
                .Where(r => r.RequestedFrom == User.Identity.Name && r.Status == ImpersonationRequestStatus.Pending)
                .ToList();

            return View();
        }

        public JsonResult GetPendingRequests()
        {
            if (!User.IsInRole("Admin")) return Json(new { count = 0 }, JsonRequestBehavior.AllowGet);

            var requests = _uow.ImpersonationRequests.GetAll()
                .Where(r => r.RequestedFrom == User.Identity.Name && r.Status == ImpersonationRequestStatus.Pending)
                .Select(r => new {
                    id = r.Id,
                    requestedBy = r.RequestedBy,
                    requestDate = r.RequestDate.ToString("HH:mm dd MMM yyyy"),
                    reason = r.Reason
                })
                .ToList();

            return Json(new { count = requests.Count, requests = requests }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetImpersonationStatus()
        {
            // Only non-SuperAdmins (regular company admins) should be frozen
            if (!User.Identity.IsAuthenticated || User.IsInRole("SuperAdmin")) 
                return Json(new { isLocked = false }, JsonRequestBehavior.AllowGet);

            if (!User.IsInRole("Admin")) 
                return Json(new { isLocked = false }, JsonRequestBehavior.AllowGet);

            var username = User.Identity.Name;
            var user = _uow.Users.GetAll().FirstOrDefault(u => u.UserName == username);
            if (user == null || !user.CompanyId.HasValue) return Json(new { isLocked = false }, JsonRequestBehavior.AllowGet);

            // Check if there is an ACTIVE impersonation request for this company
            var activeImpersonation = _uow.ImpersonationRequests.GetAll()
                .Any(r => r.CompanyId == user.CompanyId && r.Status == ImpersonationRequestStatus.Active);

            return Json(new { isLocked = activeImpersonation }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult CheckRequestStatus(int requestId)
        {
            var request = _uow.ImpersonationRequests.Get(requestId);
            if (request == null) return Json(new { status = "NotFound" }, JsonRequestBehavior.AllowGet);

            return Json(new { status = request.Status.ToString() }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandleImpersonationRequest(int requestId, bool approved, string notes = null)
        {
            var request = _uow.ImpersonationRequests.Get(requestId);
            if (request == null || request.RequestedFrom != User.Identity.Name) return HttpNotFound();

            if (request.Status == ImpersonationRequestStatus.Pending)
            {
                request.Status = approved ? ImpersonationRequestStatus.Approved : ImpersonationRequestStatus.Rejected;
                request.DecisionDate = DateTime.Now;
                request.AdminNotes = notes;
                _uow.ImpersonationRequests.Update(request);
                _uow.Complete();

                _auditService.LogAction(
                    User.Identity.Name,
                    approved ? "IMPERSONATION_APPROVED" : "IMPERSONATION_REJECTED",
                    "Dashboard",
                    requestId.ToString(),
                    null,
                    new { 
                        SuperAdmin = request.RequestedBy, 
                        Outcome = request.Status.ToString(),
                        CompanyId = request.CompanyId 
                    }
                );

                TempData["SuccessMessage"] = approved ? "Elevation request approved." : "Elevation request rejected.";
            }

            return RedirectToAction("Index");
        }
    }
}







