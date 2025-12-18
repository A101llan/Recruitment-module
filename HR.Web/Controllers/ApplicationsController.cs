using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HR.Web.Data;
using HR.Web.Models;
using HR.Web.Services;

namespace HR.Web.Controllers
{
[Authorize]
public class ApplicationsController : Controller
{
    private readonly UnitOfWork _uow = new UnitOfWork();
    private readonly IStorageService _storage = new StorageService();
    private readonly IEmailService _email = new EmailService();

    // Questionnaire for position application
    [Authorize]
    public ActionResult Questionnaire(int positionId)
    {
        var position = _uow.Positions.GetAll(p => p.PositionQuestions.Select(pq => pq.Question))
            .FirstOrDefault(p => p.Id == positionId);
        if (position == null)
            return HttpNotFound();
        ViewBag.Position = position;
        // Autofill applicant info from logged-in user
        if (User != null && User.Identity != null && User.Identity.IsAuthenticated)
        {
            var user = _uow.Users.GetAll().FirstOrDefault(u => u.UserName == User.Identity.Name);
            if (user != null)
            {
                var applicant = _uow.Applicants.GetAll().FirstOrDefault(a => a.Email == user.Email);
                if (applicant != null)
                {
                    ViewBag.Applicant = applicant;
                }
            }
        }
        ViewBag.PositionQuestions = position.PositionQuestions
            .OrderBy(pq => pq.Order)
            .ToList();

        return View();
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public ActionResult Questionnaire(
        int? positionId,
        string answer1,
        string otherWhy,
        string yearsInField,
        string yearsInRole,
        string answer3,
        string educationLevel,
        string workAvailability,
        string workMode,
        string availabilityToStart,
        HttpPostedFileBase resume)
    {
        if (!positionId.HasValue)
        {
            // Try to recover from form field if possible
            int parsed;
            var raw = Request["positionId"];
            if (!string.IsNullOrWhiteSpace(raw) && int.TryParse(raw, out parsed))
            {
                positionId = parsed;
            }
        }

        if (!positionId.HasValue)
        {
            return RedirectToAction("Index", "Positions");
        }

        var position = _uow.Positions.Get(positionId.Value);
        if (position == null)
            return HttpNotFound();

        // Normalize 'why interested'
        var why = answer1;
        if (string.Equals(answer1, "Other", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(otherWhy))
        {
            why = otherWhy;
        }

        // Determine applicant info for display
        string applicantName = null;
        string applicantEmail = null;
        if (User != null && User.Identity != null && User.Identity.IsAuthenticated)
        {
            var user = _uow.Users.GetAll().FirstOrDefault(u => u.UserName == User.Identity.Name);
            if (user != null)
            {
                var applicant = _uow.Applicants.GetAll().FirstOrDefault(a => a.Email == user.Email);
                applicantName = applicant != null ? applicant.FullName : user.UserName;
                applicantEmail = applicant != null ? applicant.Email : user.Email;
            }
        }

        // Save CV immediately so we can show it on review
        string resumePath = null;
        if (resume != null)
        {
            resumePath = _storage.SaveResume(resume);
        }

        var review = new ApplicationReviewViewModel
        {
            PositionId = positionId.HasValue ? positionId.Value : 0,
            PositionTitle = position.Title,
            ApplicantName = applicantName,
            ApplicantEmail = applicantEmail,
            WhyInterested = why,
            YearsInField = yearsInField,
            YearsInRole = yearsInRole,
            ExpectedSalary = answer3,
            EducationLevel = educationLevel,
            WorkAvailability = workAvailability,
            WorkMode = workMode,
            AvailabilityToStart = availabilityToStart,
            ResumePath = resumePath
        };

        // Capture dynamic answers
        foreach (string key in Request.Form.AllKeys)
        {
            if (key != null && key.StartsWith("dynamicAnswer_", StringComparison.OrdinalIgnoreCase))
            {
                var qidPart = key.Substring("dynamicAnswer_".Length);
                if (int.TryParse(qidPart, out var qid))
                {
                    var ans = Request.Form[key];
                    // For now we just append to WhyInterested for display; answers will be stored in ApplicationAnswer on finish.
                    if (!string.IsNullOrWhiteSpace(ans))
                    {
                        review.WhyInterested += "\n" + ans;
                    }
                }
            }
        }

        return View("QuestionnaireReview", review);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public ActionResult FinishQuestionnaire(ApplicationReviewViewModel model)
    {
        if (model == null || model.PositionId <= 0)
        {
            return RedirectToAction("Index", "Positions");
        }

        // Find or create applicant from logged-in user
        Applicant applicant = null;
        if (User != null && User.Identity != null && User.Identity.IsAuthenticated)
        {
            var user = _uow.Users.GetAll().FirstOrDefault(u => u.UserName == User.Identity.Name);
            if (user != null)
            {
                applicant = _uow.Applicants.GetAll().FirstOrDefault(a => a.Email == user.Email);
                if (applicant == null)
                {
                    // Create new applicant from user info
                    applicant = new Applicant
                    {
                        FullName = user.UserName,
                        Email = user.Email,
                        Phone = ""
                    };
                    _uow.Applicants.Add(applicant);
                    _uow.Complete();
                }
            }
        }

        if (applicant != null)
        {
            var application = new Application
            {
                ApplicantId = applicant.Id,
                PositionId = model.PositionId,
                Status = "Submitted",
                AppliedOn = DateTime.UtcNow,
                WorkExperienceLevel = model.YearsInRole,
                ResumePath = model.ResumePath
            };
            _uow.Applications.Add(application);
            _uow.Complete();

            // Store dynamic answers, if any
            foreach (string key in Request.Form.AllKeys)
            {
                if (key != null && key.StartsWith("dynamicAnswer_", StringComparison.OrdinalIgnoreCase))
                {
                    var qidPart = key.Substring("dynamicAnswer_".Length);
                    if (int.TryParse(qidPart, out var qid))
                    {
                        var ans = Request.Form[key];
                        if (!string.IsNullOrWhiteSpace(ans))
                        {
                            var appAns = new ApplicationAnswer
                            {
                                ApplicationId = application.Id,
                                QuestionId = qid,
                                AnswerText = ans
                            };
                            _uow.ApplicationAnswers.Add(appAns);
                        }
                    }
                }
            }
            _uow.Complete();
        }

        TempData["QuestionnaireSuccess"] = "Your application and questionnaire have been submitted.";
        return RedirectToAction("Index", "Positions");
    }

        [Authorize]
        public ActionResult Index()
        {
            // If the user is Admin or HR, show all applications
            if (User != null && User.Identity != null && User.IsInRole("Admin"))
            {
                var apps = _uow.Applications.GetAll(a => a.Applicant, a => a.Position);
                
                // Get interviewers for booking
                ViewBag.Interviewers = _uow.Users.GetAll().Where(u => u.Role == "Admin").ToList();
                
                // Get existing interview application IDs
                var interviewedAppIds = _uow.Interviews.GetAll().Select(i => i.ApplicationId).ToList();
                ViewBag.InterviewedAppIds = interviewedAppIds;
                
                return View(apps);
            }
            // Otherwise, show only applications for the logged-in applicant
            var user = _uow.Users.GetAll().FirstOrDefault(u => u.UserName == User.Identity.Name);
            if (user != null)
            {
                var applicant = _uow.Applicants.GetAll().FirstOrDefault(a => a.Email == user.Email);
                if (applicant != null)
                {
                    var apps = _uow.Applications.GetAll(a => a.Applicant, a => a.Position)
                        .Where(a => a.ApplicantId == applicant.Id);
                    return View(apps);
                }
            }
            // If not matched, show empty list
            return View(Enumerable.Empty<Application>());
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Details(int id)
        {
            var app = _uow.Applications.GetAll(a => a.Applicant, a => a.Position)
                .FirstOrDefault(a => a.Id == id);
            if (app == null)
            {
                return HttpNotFound();
            }
            return View(app);
        }

        public ActionResult Create(int? positionId)
        {
            // If the user is authenticated and not Admin/HR, attempt to preselect their Applicant record
            if (User != null && User.Identity != null && User.Identity.IsAuthenticated && !User.IsInRole("Admin"))
            {
                var user = _uow.Users.GetAll().FirstOrDefault(u => u.UserName == User.Identity.Name);
                if (user != null)
                {
                    var applicant = _uow.Applicants.GetAll().FirstOrDefault(a => a.Email == user.Email);
                    if (applicant != null)
                    {
                        ViewBag.CurrentApplicantId = applicant.Id;
                        ViewBag.CurrentApplicantName = applicant.FullName;
                    }
                }
            }
            LoadLookups();
            if (positionId.HasValue) ViewBag.PositionId = positionId.Value;
            return View(new Application { Status = "Submitted", AppliedOn = DateTime.UtcNow });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Application model, HttpPostedFileBase resume)
        {
            if (resume != null)
            {
                model.ResumePath = _storage.SaveResume(resume);
            }
            // Server-side checks: if user is regular (not Admin/HR), ensure they are applying as themselves
            if (User != null && User.Identity != null && User.Identity.IsAuthenticated && !User.IsInRole("Admin"))
            {
                var user = _uow.Users.GetAll().FirstOrDefault(u => u.UserName == User.Identity.Name);
                if (user == null)
                {
                    ModelState.AddModelError("", "User record not found.");
                    LoadLookups(model);
                    return View(model);
                }
                var applicant = _uow.Applicants.GetAll().FirstOrDefault(a => a.Email == user.Email);
                if (applicant == null)
                {
                    ModelState.AddModelError("", "No applicant profile matched to your account.");
                    LoadLookups(model);
                    return View(model);
                }
                if (model.ApplicantId != applicant.Id)
                {
                    ModelState.AddModelError("", "You may only apply using your own applicant profile.");
                    LoadLookups(model);
                    return View(model);
                }
            }

            if (!ModelState.IsValid)
            {
                LoadLookups(model);
                return View(model);
            }

            model.AppliedOn = DateTime.UtcNow;
            _uow.Applications.Add(model);
            _uow.Complete();
            var applicantEmail = model != null && model.Applicant != null ? model.Applicant.Email : null;
            _email.SendAsync(applicantEmail, "Application received", "We received your application.");
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Edit(int id)
        {
            var app = _uow.Applications.Get(id);
            if (app == null)
            {
                return HttpNotFound();
            }
            LoadLookups(app);
            return View(app);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(Application model, HttpPostedFileBase resume)
        {
            if (resume != null)
            {
                model.ResumePath = _storage.SaveResume(resume);
            }

            if (!ModelState.IsValid)
            {
                LoadLookups(model);
                return View(model);
            }

            _uow.Applications.Update(model);
            _uow.Complete();
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult PutOnHold(int applicationId)
        {
            var app = _uow.Applications.Get(applicationId);
            if (app == null)
            {
                return HttpNotFound();
            }
            app.Status = "On Hold";
            _uow.Applications.Update(app);
            _uow.Complete();
            TempData["Message"] = "Applicant has been put on hold.";
            return RedirectToAction("Index", "Applicants");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult ReleaseHold(int applicationId)
        {
            var app = _uow.Applications.Get(applicationId);
            if (app == null)
            {
                return HttpNotFound();
            }
            // Restore to a status the user can proceed from. Let's use 'Submitted'.
            app.Status = "Submitted";
            _uow.Applications.Update(app);
            _uow.Complete();
            TempData["Message"] = "Applicant has been released from hold.";
            return RedirectToAction("Index", "Applicants");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult UpdateStatus(int id, string status)
        {
            var app = _uow.Applications.Get(id);
            if (app == null)
            {
                return HttpNotFound();
            }
            app.Status = status;
            _uow.Applications.Update(app);
            _uow.Complete();
            return RedirectToAction("Details", new { id });
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int id)
        {
            var app = _uow.Applications.Get(id);
            if (app == null)
            {
                return HttpNotFound();
            }
            return View(app);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult DeleteConfirmed(int id)
        {
            var app = _uow.Applications.Get(id);
            if (app == null)
            {
                return HttpNotFound();
            }
            _uow.Applications.Remove(app);
            _uow.Complete();
            return RedirectToAction("Index");
        }

        private void LoadLookups(Application model = null)
        {
            ViewBag.ApplicantId = new SelectList(_uow.Applicants.GetAll(), "Id", "FullName", model != null ? (object)model.ApplicantId : null);
            ViewBag.PositionId = new SelectList(_uow.Positions.GetAll(), "Id", "Title", model != null ? (object)model.PositionId : null);
        }
    }
}






