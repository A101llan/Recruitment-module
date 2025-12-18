using System;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using HR.Web.Data;
using HR.Web.Models;

namespace HR.Web.Controllers
{
    [Authorize]
    public class PositionsController : Controller
    {
        private readonly UnitOfWork _uow = new UnitOfWork();

        public ActionResult Index()
        {
            var positions = _uow.Positions.GetAll(p => p.Department)
                .OrderByDescending(p => p.PostedOn);
            return View(positions);
        }

        public ActionResult Details(int id)
        {
            var position = _uow.Positions.GetAll(p => p.Department)
                .FirstOrDefault(p => p.Id == id);
            if (position == null)
            {
                return HttpNotFound();
            }
            return View(position);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Create()
        {
            ViewBag.DepartmentId = new SelectList(_uow.Departments.GetAll(), "Id", "Name");
            ViewBag.QuestionList = _uow.Questions.GetAll().Where(q => q.IsActive).ToList();
            return View(new Position
            {
                IsOpen = true,
                PostedOn = DateTime.UtcNow
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Create(Position model, int[] selectedQuestions)
        {
            Debug.WriteLine("[PositionsController.Create][POST] Entered at " + DateTime.UtcNow);
            Debug.WriteLine($"Title='{model?.Title}', DeptId={model?.DepartmentId}, IsOpen={model?.IsOpen}");
            Debug.WriteLine("ModelState.IsValid = " + ModelState.IsValid);

            if (!ModelState.IsValid)
            {
                foreach (var kvp in ModelState)
                {
                    foreach (var err in kvp.Value.Errors)
                    {
                        Debug.WriteLine($"[PositionsController.Create][ModelError] Key='{kvp.Key}', Error='{err.ErrorMessage}', Exception='{err.Exception?.Message}'");
                    }
                }
                ViewBag.DepartmentId = new SelectList(_uow.Departments.GetAll(), "Id", "Name", model.DepartmentId);
                ViewBag.QuestionList = _uow.Questions.GetAll().Where(q => q.IsActive).ToList();
                Debug.WriteLine("[PositionsController.Create][POST] Returning view due to invalid ModelState.");
                return View(model);
            }

            model.PostedOn = DateTime.UtcNow;
            try
            {
                Debug.WriteLine("[PositionsController.Create][POST] Adding position to UoW and saving...");
                _uow.Positions.Add(model);
                _uow.Complete();
                Debug.WriteLine("[PositionsController.Create][POST] Save succeeded. New Id=" + model.Id);
            }
            catch (Exception ex)
            {
                // Surface any database/validation issues back to the user instead of silently failing
                Debug.WriteLine("[PositionsController.Create][POST] Exception during save: " + ex);
                ModelState.AddModelError("", "Unable to save position: " + ex.Message);
                ViewBag.DepartmentId = new SelectList(_uow.Departments.GetAll(), "Id", "Name", model.DepartmentId);
                ViewBag.QuestionList = _uow.Questions.GetAll().Where(q => q.IsActive).ToList();
                Debug.WriteLine("[PositionsController.Create][POST] Returning view due to exception.");
                return View(model);
            }

            // Link selected questions to this position
            if (selectedQuestions != null && selectedQuestions.Length > 0)
            {
                int order = 1;
                foreach (var qid in selectedQuestions)
                {
                    var pq = new PositionQuestion
                    {
                        PositionId = model.Id,
                        QuestionId = qid,
                        Order = order++
                    };
                    _uow.PositionQuestions.Add(pq);
                }
                _uow.Complete();
                Debug.WriteLine("[PositionsController.Create][POST] Linked " + selectedQuestions.Length + " questions.");
            }

            TempData["Message"] = "Position created successfully.";
            Debug.WriteLine("[PositionsController.Create][POST] Redirecting to Index.");
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Edit(int id)
        {
            var position = _uow.Positions.Get(id);
            if (position == null)
            {
                return HttpNotFound();
            }

            ViewBag.DepartmentId = new SelectList(_uow.Departments.GetAll(), "Id", "Name", position.DepartmentId);
            return View(position);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(Position model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.DepartmentId = new SelectList(_uow.Departments.GetAll(), "Id", "Name", model.DepartmentId);
                return View(model);
            }

            _uow.Positions.Update(model);
            _uow.Complete();
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int id)
        {
            var position = _uow.Positions.Get(id);
            if (position == null)
            {
                return HttpNotFound();
            }

            return View(position);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult DeleteConfirmed(int id)
        {
            var position = _uow.Positions.Get(id);
            if (position == null)
            {
                return HttpNotFound();
            }

            _uow.Positions.Remove(position);
            _uow.Complete();
            return RedirectToAction("Index");
        }
    }
}










