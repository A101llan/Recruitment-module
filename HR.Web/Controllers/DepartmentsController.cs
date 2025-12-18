using System.Linq;
using System.Web.Mvc;
using HR.Web.Data;
using HR.Web.Models;

namespace HR.Web.Controllers
{
    [Authorize]
    public class DepartmentsController : Controller
    {
        private readonly UnitOfWork _uow = new UnitOfWork();

        public ActionResult Index()
        {
            var items = _uow.Departments.GetAll();
            return View(items);
        }

        public ActionResult Details(int id)
        {
            var item = _uow.Departments.Get(id);
            if (item == null)
            {
                return HttpNotFound();
            }
            return View(item);
        }

        public ActionResult Create()
        {
            return View(new Department());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Department model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            _uow.Departments.Add(model);
            _uow.Complete();
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            var item = _uow.Departments.Get(id);
            if (item == null)
            {
                return HttpNotFound();
            }
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Department model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            _uow.Departments.Update(model);
            _uow.Complete();
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int id)
        {
            var item = _uow.Departments.Get(id);
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
            var item = _uow.Departments.Get(id);
            if (item == null)
            {
                return HttpNotFound();
            }
            _uow.Departments.Remove(item);
            _uow.Complete();
            return RedirectToAction("Index");
        }
    }
}


















