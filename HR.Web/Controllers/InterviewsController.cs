using System;
using System.Linq;
using System.Web.Mvc;
using HR.Web.Data;
using HR.Web.Models;
using HR.Web.Services;

namespace HR.Web.Controllers
{
    public class InterviewsController : Controller
    {
        private readonly UnitOfWork _uow = new UnitOfWork();
        private readonly IEmailService _email = new EmailService();
        private readonly AuditService _auditService = new AuditService();
        private readonly TenantService _tenantService = new TenantService();

        public ActionResult Index()
        {
            // If the user is Admin or HR, show all interviews
            if (User != null && User.Identity != null && User.IsInRole("Admin"))
            {
                var itemsQuery = _uow.Interviews.GetAll(i => i.Application.Applicant, i => i.Application.Position, i => i.Interviewer).AsQueryable();
                itemsQuery = _tenantService.ApplyTenantFilter(itemsQuery);
                var items = itemsQuery.ToList();
                return View(items);
            }
            // Otherwise, show only interviews for the logged-in applicant
            var user = _uow.Users.GetAll().FirstOrDefault(u => u.UserName == User.Identity.Name);
            if (user != null)
            {
                var applicant = _uow.Applicants.GetAll().FirstOrDefault(a => a.Email == user.Email);
                if (applicant != null)
                {
                    var itemsQuery = _uow.Interviews.GetAll(i => i.Application.Applicant, i => i.Application.Position, i => i.Interviewer)
                        .Where(i => i.Application.ApplicantId == applicant.Id).AsQueryable();
                    itemsQuery = _tenantService.ApplyTenantFilter(itemsQuery);
                    var items = itemsQuery.ToList();
                    return View(items);
                }
            }
            // If not matched, show empty list
            return View(Enumerable.Empty<Interview>());
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult BookInterview(int applicationId, int interviewerId, DateTime scheduledAt, string mode)
        {
            try
            {
                var interview = new Interview
                {
                    ApplicationId = applicationId,
                    InterviewerId = interviewerId,
                    ScheduledAt = scheduledAt,
                    Mode = mode
                };
                _uow.Interviews.Add(interview);
                _uow.Complete();
                
                // Log interview booking
                var newValues = new { 
                    ApplicationId = applicationId,
                    InterviewerId = interviewerId,
                    ScheduledAt = scheduledAt,
                    Mode = mode
                };
                _auditService.LogCreate(User.Identity.Name, "Interviews", interview.Id.ToString(), newValues);
                
                var interviewer = _uow.Users.Get(interviewerId);
                if (interviewer != null)
                {
                    _email.SendAsync(interviewer.Email, "Interview scheduled", "You have a new interview scheduled.");
                }
                
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _auditService.LogAction(User.Identity.Name, "CREATE", "Interviews", "new", 
                    wasSuccessful: false, errorMessage: ex.Message);
                
                TempData["Error"] = "Error booking interview: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        public ActionResult Details(int id)
        {
            var interview = _uow.Interviews.GetAll(i => i.Application.Applicant, i => i.Application.Position, i => i.Interviewer)
                .FirstOrDefault(i => i.Id == id);
            if (interview == null)
            {
                return HttpNotFound();
            }
            return View(interview);
        }

        [Authorize]
        public ActionResult Create(int? applicationId)
        {
            LoadLookups();
            var interview = new Interview { ScheduledAt = DateTime.UtcNow.AddDays(1) };
            if (applicationId.HasValue)
            {
                interview.ApplicationId = applicationId.Value;
                ViewBag.ApplicationId = new SelectList(_uow.Applications.GetAll(a => a.Applicant, a => a.Position), "Id", "Id", applicationId.Value);
            }
            return View(interview);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Interview model)
        {
            if (!ModelState.IsValid)
            {
                LoadLookups(model);
                return View(model);
            }
            _uow.Interviews.Add(model);
            _uow.Complete();
            var interviewerEmail = model != null && model.Interviewer != null ? model.Interviewer.Email : null;
            _email.SendAsync(interviewerEmail, "Interview scheduled", "Please attend.");
            return RedirectToAction("Index");
        }

        [Authorize]
        public ActionResult Edit(int id)
        {
            var interview = _uow.Interviews.Get(id);
            if (interview == null)
            {
                return HttpNotFound();
            }
            LoadLookups(interview);
            return View(interview);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Interview model)
        {
            if (!ModelState.IsValid)
            {
                LoadLookups(model);
                return View(model);
            }

            _uow.Interviews.Update(model);
            _uow.Complete();
            return RedirectToAction("Index");
        }

        [Authorize]
        public ActionResult Delete(int id)
        {
            var interview = _uow.Interviews.Get(id);
            if (interview == null)
            {
                return HttpNotFound();
            }
            return View(interview);
        }

        [Authorize]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var interview = _uow.Interviews.Get(id);
            if (interview == null)
            {
                return HttpNotFound();
            }
            _uow.Interviews.Remove(interview);
            _uow.Complete();
            return RedirectToAction("Index");
        }

        private void LoadLookups(Interview model = null)
        {
            ViewBag.ApplicationId = new SelectList(_uow.Applications.GetAll(a => a.Applicant, a => a.Position), "Id", "Id", model != null ? (object)model.ApplicationId : null);
            ViewBag.InterviewerId = new SelectList(_uow.Users.GetAll(), "Id", "UserName", model != null ? (object)model.InterviewerId : null);
        }
    }
}






