using System.Linq;
using System.Web.Mvc;
using HR.Web.Data;
using HR.Web.Models;

namespace HR.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ApplicantsController : Controller
    {
        private readonly UnitOfWork _uow = new UnitOfWork();

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
            _uow.Applicants.Add(model);
            _uow.Complete();
            return RedirectToAction("Index");
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
            _uow.Applicants.Update(model);
            _uow.Complete();
            return RedirectToAction("Index");
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
            var item = _uow.Applicants.Get(id);
            if (item == null)
            {
                return HttpNotFound();
            }
            _uow.Applicants.Remove(item);
            _uow.Complete();
            return RedirectToAction("Index");
        }
    }
}







