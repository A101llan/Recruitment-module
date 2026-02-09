using System.Linq;
using System.Web.Mvc;
using HR.Web.Data;
using HR.Web.Models;
using HR.Web.Services;

namespace HR.Web.Controllers
{
    public class DepartmentsController : Controller
    {
        private readonly UnitOfWork _uow = new UnitOfWork();
        private readonly TenantService _tenantService = new TenantService();

        public ActionResult Index()
        {
            var itemsQuery = _uow.Departments.GetAll().AsQueryable();
            itemsQuery = _tenantService.ApplyTenantFilter(itemsQuery);
            var items = itemsQuery.ToList();
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

        [Authorize(Roles = "Admin")]
        public ActionResult Create()
        {
            return View(new Department());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Create(Department model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            
            // Assign current user's company
            var companyId = _tenantService.GetCurrentUserCompanyId();
            if (!companyId.HasValue)
            {
                ModelState.AddModelError("", "No company assigned to your account.");
                return View(model);
            }
            model.CompanyId = companyId.Value;
            
            _uow.Departments.Add(model);
            _uow.Complete();
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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









































