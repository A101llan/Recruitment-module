using System;
using System.Linq;
using System.Web.Mvc;
using HR.Web.Data;
using HR.Web.Models;
using HR.Web.Services;

namespace HR.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ApplicantsController : Controller
    {
        private readonly UnitOfWork _uow = new UnitOfWork();
        private readonly AuditService _auditService = new AuditService();

        public ActionResult Index(string sortOrder)
        {
            ViewBag.ProficiencySortParam = string.IsNullOrEmpty(sortOrder) ? "proficiency_desc" : "";
            
            var items = _uow.Applicants.GetAll(a => a.Applications).ToList();
            
            // Sort by proficiency (from latest application's WorkExperienceLevel)
            switch (sortOrder)
            {
                case "proficiency_desc":
                    items = items.OrderByDescending(a => GetProficiencyValue(a)).ToList();
                    break;
                case "proficiency_asc":
                    items = items.OrderBy(a => GetProficiencyValue(a)).ToList();
                    break;
                default:
                    items = items.OrderBy(a => a.FullName).ToList();
                    break;
            }
            
            // Get interviewers for booking
            ViewBag.Interviewers = _uow.Users.GetAll().Where(u => u.Role == "Admin").ToList();
            
            // Get existing interview application IDs
            var interviewedAppIds = _uow.Interviews.GetAll().Select(i => i.ApplicationId).ToList();
            ViewBag.InterviewedAppIds = interviewedAppIds;
            
            return View(items);
        }
        
        private int GetProficiencyValue(Applicant applicant)
        {
            if (applicant.Applications == null || !applicant.Applications.Any())
                return -1;
            var latest = applicant.Applications.OrderByDescending(a => a.AppliedOn).FirstOrDefault();
            if (latest == null || string.IsNullOrEmpty(latest.WorkExperienceLevel))
                return -1;
            int val;
            if (int.TryParse(latest.WorkExperienceLevel, out val))
                return val;
            return -1;
        }

        public ActionResult Details(int id)
        {
            var applicant = _uow.Applicants.GetAll(a => a.Applications).FirstOrDefault(a => a.Id == id);
            if (applicant == null)
            {
                return HttpNotFound();
            }
            // Get latest application
            var latestApp = applicant.Applications != null ? applicant.Applications.OrderByDescending(a => a.AppliedOn).FirstOrDefault() : null;
            if (latestApp != null)
            {
                // Get questionnaire answers
                var answers = _uow.ApplicationAnswers.GetAll(aa => aa.Question)
                    .Where(aa => aa.ApplicationId == latestApp.Id)
                    .ToList();
                ViewBag.LatestApplication = latestApp;
                ViewBag.QuestionnaireAnswers = answers;
            }
            return View(applicant);
        }

        public ActionResult Create()
        {
            return View(new Applicant());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Applicant model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            
            try
            {
                _uow.Applicants.Add(model);
                _uow.Complete();
                
                // Log applicant creation
                _auditService.LogCreate(User.Identity.Name, "Applicants", model.Id.ToString(), new { 
                    FullName = model.FullName, 
                    Email = model.Email, 
                    Phone = model.Phone 
                });
                
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _auditService.LogAction(User.Identity.Name, "CREATE", "Applicants", "new", 
                    wasSuccessful: false, errorMessage: ex.Message);
                
                ModelState.AddModelError("", "Error creating applicant: " + ex.Message);
                return View(model);
            }
        }

        public ActionResult Edit(int id)
        {
            var item = _uow.Applicants.Get(id);
            if (item == null)
            {
                return HttpNotFound();
            }
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Applicant model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            
            try
            {
                // Get old values for audit
                var oldApplicant = _uow.Applicants.Get(model.Id);
                var oldValues = new { 
                    FullName = oldApplicant?.FullName, 
                    Email = oldApplicant?.Email, 
                    Phone = oldApplicant?.Phone 
                };
                
                _uow.Applicants.Update(model);
                _uow.Complete();
                
                // Log applicant update
                var newValues = new { 
                    FullName = model.FullName, 
                    Email = model.Email, 
                    Phone = model.Phone 
                };
                
                _auditService.LogUpdate(User.Identity.Name, "Applicants", model.Id.ToString(), oldValues, newValues);
                
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _auditService.LogAction(User.Identity.Name, "UPDATE", "Applicants", model.Id.ToString(), 
                    wasSuccessful: false, errorMessage: ex.Message);
                
                ModelState.AddModelError("", "Error updating applicant: " + ex.Message);
                return View(model);
            }
        }

        public ActionResult Delete(int id)
        {
            var item = _uow.Applicants.Get(id);
            if (item == null)
            {
                return HttpNotFound();
            }
            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                // Do not delete if applicant still has applications (FK constraint)
                var hasApplications = _uow.Applications.GetAll().Any(a => a.ApplicantId == id);
                if (hasApplications)
                {
                    TempData["DeleteError"] = "Cannot delete applicant because applications still exist. Delete or reassign those applications first.";
                    
                    // Log failed deletion attempt
                    _auditService.LogAction(User.Identity.Name, "DELETE", "Applicants", id.ToString(), 
                        wasSuccessful: false, errorMessage: "Applicant has existing applications");
                    
                    return RedirectToAction("Details", new { id });
                }

                var item = _uow.Applicants.Get(id);
                if (item == null)
                {
                    return HttpNotFound();
                }
                
                // Store old values for audit
                var oldValues = new { 
                    FullName = item.FullName, 
                    Email = item.Email, 
                    Phone = item.Phone 
                };
                
                _uow.Applicants.Remove(item);
                _uow.Complete();
                
                // Log successful deletion
                _auditService.LogDelete(User.Identity.Name, "Applicants", id.ToString(), oldValues);
                
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _auditService.LogAction(User.Identity.Name, "DELETE", "Applicants", id.ToString(), 
                    wasSuccessful: false, errorMessage: ex.Message);
                
                TempData["DeleteError"] = "Error deleting applicant: " + ex.Message;
                return RedirectToAction("Details", new { id });
            }
        }
    }
}







