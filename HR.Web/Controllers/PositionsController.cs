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
            
            // Check which positions the current user has already applied for
            var appliedPositionIds = new System.Collections.Generic.HashSet<int>();
            if (User != null && User.Identity != null && User.Identity.IsAuthenticated)
            {
                var user = _uow.Users.GetAll().FirstOrDefault(u => u.UserName == User.Identity.Name);
                if (user != null)
                {
                    var applicant = _uow.Applicants.GetAll().FirstOrDefault(a => a.Email == user.Email);
                    if (applicant != null)
                    {
                        appliedPositionIds = new System.Collections.Generic.HashSet<int>(
                            _uow.Applications.GetAll()
                                .Where(a => a.ApplicantId == applicant.Id)
                                .Select(a => a.PositionId)
                                .ToList()
                        );
                    }
                }
            }
            ViewBag.AppliedPositionIds = appliedPositionIds;
            
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
                PostedOn = DateTime.UtcNow,
                Currency = "KES"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Create(Position model, int[] selectedQuestions)
        {
            Debug.WriteLine("[PositionsController.Create][POST] Entered at " + DateTime.UtcNow);
            Debug.WriteLine(string.Format("Title='{0}', DeptId={1}, IsOpen={2}", model != null ? model.Title : null, model != null ? (object)model.DepartmentId : null, model != null ? (object)model.IsOpen : null));
            Debug.WriteLine("ModelState.IsValid = " + ModelState.IsValid);

            // ensure a department was selected (DropDownList optionLabel posts empty -> 0)
            if (model.DepartmentId <= 0)
            {
                ModelState.AddModelError("DepartmentId", "Please select a department.");
            }

            if (!ModelState.IsValid)
            {
                foreach (var kvp in ModelState)
                {
                    foreach (var err in kvp.Value.Errors)
                    {
                    Debug.WriteLine(string.Format("[PositionsController.Create][ModelError] Key='{0}', Error='{1}', Exception='{2}'", kvp.Key, err.ErrorMessage, err.Exception != null ? err.Exception.Message : null));
                    }
                }
                ViewBag.DepartmentId = new SelectList(_uow.Departments.GetAll(), "Id", "Name", model.DepartmentId);
                ViewBag.QuestionList = _uow.Questions.GetAll().Where(q => q.IsActive).ToList();
                Debug.WriteLine("[PositionsController.Create][POST] Returning view due to invalid ModelState.");
                return View(model);
            }

            model.PostedOn = DateTime.UtcNow;
            // Set default currency to KES if not provided
            if (string.IsNullOrEmpty(model.Currency))
            {
                model.Currency = "KES";
            }
            try
            {
                Debug.WriteLine("[PositionsController.Create][POST] Adding position to UoW and saving...");
                _uow.Positions.Add(model);
                _uow.Complete();
                Debug.WriteLine("[PositionsController.Create][POST] Save succeeded. New Id=" + model.Id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[PositionsController.Create][POST] Exception during save: " + ex);
                var msg = ex.GetBaseException()?.Message ?? ex.Message;
                ModelState.AddModelError("", "Unable to save position: " + msg);
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
            Debug.WriteLine("[PositionsController.Edit][POST] Entered at " + DateTime.UtcNow);
            Debug.WriteLine(string.Format("Title='{0}', DeptId={1}, IsOpen={2}", model != null ? model.Title : null, model != null ? (object)model.DepartmentId : null, model != null ? (object)model.IsOpen : null));
            Debug.WriteLine("ModelState.IsValid = " + ModelState.IsValid);

            if (!ModelState.IsValid)
            {
                ViewBag.DepartmentId = new SelectList(_uow.Departments.GetAll(), "Id", "Name", model.DepartmentId);
                Debug.WriteLine("[PositionsController.Edit][POST] Returning view due to invalid ModelState.");
                return View(model);
            }

            // ensure a department was selected
            if (model.DepartmentId <= 0)
            {
                ModelState.AddModelError("DepartmentId", "Please select a department.");
                ViewBag.DepartmentId = new SelectList(_uow.Departments.GetAll(), "Id", "Name", model.DepartmentId);
                return View(model);
            }

            try
            {
                // Get the existing position to preserve PostedOn and other fields not in the form
                var existingPosition = _uow.Positions.Get(model.Id);
                if (existingPosition == null)
                {
                    return HttpNotFound();
                }

                // Preserve the original PostedOn value (SQL Server datetime range: 1753-01-01 to 9999-12-31)
                // Ensure PostedOn is always within valid SQL Server datetime range
                var originalPostedOn = existingPosition.PostedOn;
                var minDateTime = new DateTime(1753, 1, 1);
                var maxDateTime = new DateTime(9999, 12, 31, 23, 59, 59);
                
                if (originalPostedOn < minDateTime || originalPostedOn > maxDateTime)
                {
                    // If PostedOn is invalid, set it to current UTC time
                    originalPostedOn = DateTime.UtcNow;
                    Debug.WriteLine(string.Format("[PositionsController.Edit][POST] PostedOn was invalid ({0}), setting to {1}", existingPosition.PostedOn, originalPostedOn));
                }

                // Update the fields from the model
                existingPosition.Title = model.Title;
                existingPosition.Description = model.Description;
                existingPosition.Responsibilities = model.Responsibilities;
                existingPosition.Qualifications = model.Qualifications;
                existingPosition.SalaryMin = model.SalaryMin;
                existingPosition.SalaryMax = model.SalaryMax;
                existingPosition.DepartmentId = model.DepartmentId;
                existingPosition.IsOpen = model.IsOpen;
                // Update currency, default to KES if not provided
                if (!string.IsNullOrEmpty(model.Currency))
                {
                    existingPosition.Currency = model.Currency;
                }
                else if (string.IsNullOrEmpty(existingPosition.Currency))
                {
                    existingPosition.Currency = "KES";
                }
                
                // Ensure PostedOn is set to a valid value before saving
                existingPosition.PostedOn = originalPostedOn;
                
                // Double-check PostedOn is valid before saving
                if (existingPosition.PostedOn < minDateTime || existingPosition.PostedOn > maxDateTime)
                {
                    existingPosition.PostedOn = DateTime.UtcNow;
                    Debug.WriteLine($"[PositionsController.Edit][POST] PostedOn validation failed, resetting to {existingPosition.PostedOn}");
                }

                Debug.WriteLine(string.Format("[PositionsController.Edit][POST] Updating position with PostedOn={0}", existingPosition.PostedOn));
                _uow.Positions.Update(existingPosition);
                _uow.Complete();
                Debug.WriteLine("[PositionsController.Edit][POST] Save succeeded.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[PositionsController.Edit][POST] Exception during save: " + ex);
                var msg = ex.GetBaseException()?.Message ?? ex.Message;
                ModelState.AddModelError("", "Unable to save position: " + msg);
                ViewBag.DepartmentId = new SelectList(_uow.Departments.GetAll(), "Id", "Name", model.DepartmentId);
                Debug.WriteLine("[PositionsController.Edit][POST] Returning view due to exception.");
                return View(model);
            }

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










