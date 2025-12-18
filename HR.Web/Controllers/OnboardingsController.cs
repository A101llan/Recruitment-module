using System;
using System.Linq;
using System.Web.Mvc;
using HR.Web.Data;
using HR.Web.Models;

namespace HR.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class OnboardingsController : Controller
    {
        private readonly UnitOfWork _uow = new UnitOfWork();

        public ActionResult Index()
        {
            var items = _uow.Onboardings.GetAll(o => o.Application.Applicant, o => o.Application.Position);
            return View(items);
        }

        public ActionResult Details(int id)
        {
            var item = _uow.Onboardings.GetAll(o => o.Application.Applicant, o => o.Application.Position)
                .FirstOrDefault(o => o.Id == id);
            if (item == null)
            {
                return HttpNotFound();
            }
            return View(item);
        }

        public ActionResult Create()
        {
            ViewBag.ApplicationId = new SelectList(_uow.Applications.GetAll(a => a.Applicant, a => a.Position), "Id", "Id");
            return View(new Onboarding { Status = "Pending", StartDate = DateTime.UtcNow.AddDays(7) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Onboarding model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ApplicationId = new SelectList(_uow.Applications.GetAll(a => a.Applicant, a => a.Position), "Id", "Id", model.ApplicationId);
                return View(model);
            }
            _uow.Onboardings.Add(model);
            _uow.Complete();
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            var item = _uow.Onboardings.Get(id);
            if (item == null)
            {
                return HttpNotFound();
            }
            ViewBag.ApplicationId = new SelectList(_uow.Applications.GetAll(a => a.Applicant, a => a.Position), "Id", "Id", item.ApplicationId);
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Onboarding model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ApplicationId = new SelectList(_uow.Applications.GetAll(a => a.Applicant, a => a.Position), "Id", "Id", model.ApplicationId);
                return View(model);
            }

            _uow.Onboardings.Update(model);
            _uow.Complete();
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int id)
        {
            var item = _uow.Onboardings.Get(id);
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
            var item = _uow.Onboardings.Get(id);
            if (item == null)
            {
                return HttpNotFound();
            }
            _uow.Onboardings.Remove(item);
            _uow.Complete();
            return RedirectToAction("Index");
        }
    }
}



















