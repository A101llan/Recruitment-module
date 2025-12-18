using System;
using System.Linq;
using System.Web.Mvc;
using HR.Web.Data;
using HR.Web.Models;
using HR.Web.Services;

namespace HR.Web.Controllers
{
    [Authorize]
    public class InterviewsController : Controller
    {
        private readonly UnitOfWork _uow = new UnitOfWork();
        private readonly IEmailService _email = new EmailService();

        public ActionResult Index()
        {
            // If the user is Admin or HR, show all interviews
            if (User != null && User.Identity != null && User.IsInRole("Admin"))
            {
                var items = _uow.Interviews.GetAll(i => i.Application.Applicant, i => i.Application.Position, i => i.Interviewer);
                return View(items);
            }
            // Otherwise, show only interviews for the logged-in applicant
            var user = _uow.Users.GetAll().FirstOrDefault(u => u.UserName == User.Identity.Name);
            if (user != null)
            {
                var applicant = _uow.Applicants.GetAll().FirstOrDefault(a => a.Email == user.Email);
                if (applicant != null)
                {
                    var items = _uow.Interviews.GetAll(i => i.Application.Applicant, i => i.Application.Position, i => i.Interviewer)
                        .Where(i => i.Application.ApplicantId == applicant.Id);
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
            var interview = new Interview
            {
                ApplicationId = applicationId,
                InterviewerId = interviewerId,
                ScheduledAt = scheduledAt,
                Mode = mode
            };
            _uow.Interviews.Add(interview);
            _uow.Complete();
            
            var interviewer = _uow.Users.Get(interviewerId);
            if (interviewer != null)
            {
                _email.SendAsync(interviewer.Email, "Interview scheduled", "You have a new interview scheduled.");
            }
            
            return RedirectToAction("Index");
        }

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

        public ActionResult Delete(int id)
        {
            var interview = _uow.Interviews.Get(id);
            if (interview == null)
            {
                return HttpNotFound();
            }
            return View(interview);
        }

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






