using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
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
    private readonly ICandidateEvaluationService _evaluationService = new CandidateEvaluationService();

    // Questionnaire for position application
    [Authorize]
    public ActionResult Questionnaire(int positionId)
    {
        var position = _uow.Positions.GetAll(p => p.PositionQuestions.Select(pq => pq.Question).Select(q => q.QuestionOptions))
            .FirstOrDefault(p => p.Id == positionId);
        if (position == null)
            return HttpNotFound();
        
        // Debug: Log questions and their options
        System.Diagnostics.Debug.WriteLine($"=== Position {position.Title} Questions ===");
        foreach (var pq in position.PositionQuestions)
        {
            System.Diagnostics.Debug.WriteLine($"Question: {pq.Question.Text} (Type: {pq.Question.Type})");
            System.Diagnostics.Debug.WriteLine($"Options count: {pq.Question.QuestionOptions?.Count() ?? 0}");
            if (pq.Question.QuestionOptions != null)
            {
                foreach (var option in pq.Question.QuestionOptions)
                {
                    System.Diagnostics.Debug.WriteLine($"  - Option: {option.Text} (Points: {option.Points})");
                }
            }
        }
        System.Diagnostics.Debug.WriteLine($"=== End Questions ===");
        
        // Check if user has already applied for this position
        if (User != null && User.Identity != null && User.Identity.IsAuthenticated)
        {
            var user = _uow.Users.GetAll().FirstOrDefault(u => u.UserName == User.Identity.Name);
            if (user != null)
            {
                var applicant = _uow.Applicants.GetAll().FirstOrDefault(a => a.Email == user.Email);
                if (applicant != null)
                {
                    var existingApplication = _uow.Applications.GetAll()
                        .FirstOrDefault(a => a.ApplicantId == applicant.Id && a.PositionId == positionId);
                    if (existingApplication != null)
                    {
                        TempData["ErrorMessage"] = "You have already applied for this position.";
                        return RedirectToAction("Index", "Positions");
                    }
                }
            }
        }
        
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
    public ActionResult Questionnaire(int positionId, FormCollection form, HttpPostedFileBase resume)
    {
        if (positionId <= 0)
        {
            return RedirectToAction("Index", "Positions");
        }

        var position = _uow.Positions.Get(positionId);
        if (position == null)
            return HttpNotFound();

        // Get position questions
        var positionQuestions = _uow.Context.Set<PositionQuestion>()
            .Where(pq => pq.PositionId == positionId)
            .Include(pq => pq.Question)
            .Include(pq => pq.Question.QuestionOptions)
            .OrderBy(pq => pq.Order)
            .ToList();

        // Determine applicant info for display
        string applicantName = null;
        string applicantEmail = null;
        if (User != null && User.Identity != null && User.Identity.IsAuthenticated)
        {
            var user = _uow.Users.GetAll().FirstOrDefault(u => u.UserName == User.Identity.Name);
            if (user != null)
            {
                var applicant = _uow.Applicants.GetAll().FirstOrDefault(a => a.Email == user.Email);
                if (applicant != null)
                {
                    applicantName = applicant.FullName;
                    applicantEmail = applicant.Email;
                }
            }
        }

        // Create application review model
        var review = new ApplicationReviewViewModel
        {
            PositionId = positionId,
            PositionTitle = position.Title,
            ApplicantName = applicantName,
            ApplicantEmail = applicantEmail,
            QuestionAnswers = new List<QuestionAnswerViewModel>()
        };

        // Process dynamic question answers
        foreach (var pq in positionQuestions)
        {
            var questionFieldName = "question_" + pq.Question.Id;
            var answer = form[questionFieldName];
            
            review.QuestionAnswers.Add(new QuestionAnswerViewModel
            {
                QuestionId = pq.Question.Id,
                QuestionText = pq.Question.Text,
                QuestionType = pq.Question.Type,
                Answer = answer ?? ""
            });
        }

        // Handle resume upload
        string resumePath = null;
        if (resume != null && resume.ContentLength > 0)
        {
            // Validate file size (5MB max)
            if (resume.ContentLength > 5 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "Resume file size must be less than 5MB.";
                return RedirectToAction("Questionnaire", new { positionId = positionId });
            }

            // Validate file type
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
            var fileExtension = System.IO.Path.GetExtension(resume.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                TempData["ErrorMessage"] = "Only PDF, DOC, and DOCX files are allowed.";
                return RedirectToAction("Questionnaire", new { positionId = positionId });
            }

            try
            {
                resumePath = _storage.SaveResume(resume);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error uploading resume: " + ex.Message;
                return RedirectToAction("Questionnaire", new { positionId = positionId });
            }
        }
        else
        {
            // CV required
            TempData["ErrorMessage"] = "Please upload your CV/Resume to continue.";
            return RedirectToAction("Questionnaire", new { positionId = positionId });
        }

        // Store answers in session for processing
        Session["QuestionnaireAnswers"] = review.QuestionAnswers;
        Session["PositionId"] = positionId;
        Session["ResumePath"] = resumePath;

        // Update review model with resume path
        review.ResumePath = resumePath;

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
            // Check if applicant has already applied for this position
            var existingApplication = _uow.Applications.GetAll()
                .FirstOrDefault(a => a.ApplicantId == applicant.Id && a.PositionId == model.PositionId);
            if (existingApplication != null)
            {
                TempData["ErrorMessage"] = "You have already applied for this position.";
                return RedirectToAction("Index", "Positions");
            }
            
            // Get resume path from session
        var resumePath = Session["ResumePath"] as string;
        
        var application = new Application
            {
                ApplicantId = applicant.Id,
                PositionId = model.PositionId,
                Status = "Submitted",
                AppliedOn = DateTime.UtcNow,
                WorkExperienceLevel = model.YearsInRole ?? "Not specified",
                ResumePath = resumePath ?? model.ResumePath
            };
            _uow.Applications.Add(application);
            _uow.Complete();

            // Store dynamic answers from session
            var applicationAnswers = new List<ApplicationAnswer>();
            var questionAnswers = Session["QuestionnaireAnswers"] as List<QuestionAnswerViewModel>;
            if (questionAnswers != null)
            {
                foreach (var qa in questionAnswers)
                {
                    if (!string.IsNullOrWhiteSpace(qa.Answer))
                    {
                        var appAns = new ApplicationAnswer
                        {
                            ApplicationId = application.Id,
                            QuestionId = qa.QuestionId,
                            AnswerText = qa.Answer
                        };
                        _uow.ApplicationAnswers.Add(appAns);
                        applicationAnswers.Add(appAns);
                    }
                }
            }
            _uow.Complete();

            // Evaluate candidate using AI scoring
            try
            {
                var score = _evaluationService.EvaluateApplication(application.Id, model, applicationAnswers);
                application.Score = score.Score;
                application.ScoreReason = score.Reason;
                _uow.Applications.Update(application);
                _uow.Complete();
            }
            catch (Exception ex)
            {
                // Log error but don't fail the application submission
                System.Diagnostics.Debug.WriteLine("Error evaluating application: " + ex.Message);
            }
        }

        // Clean up session
        Session.Remove("QuestionnaireAnswers");
        Session.Remove("PositionId");
        Session.Remove("ResumePath");

        TempData["QuestionnaireSuccess"] = "Your application and questionnaire have been submitted.";
        return RedirectToAction("Index", "Positions");
    }

        [Authorize]
        public ActionResult Index()
        {
            // If the user is Admin or HR, show all applications
            if (User != null && User.Identity != null && User.IsInRole("Admin"))
            {
                var apps = _uow.Applications.GetAll(a => a.Applicant, a => a.Position)
                    .OrderByDescending(a => a.Score ?? 0)
                    .ThenByDescending(a => a.AppliedOn);
                
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
            if (positionId.HasValue)
            {
                var model = new Application { Status = "Submitted", AppliedOn = DateTime.UtcNow, PositionId = positionId.Value };
                LoadLookups(model);
                return View(model);
            }
            LoadLookups();
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






