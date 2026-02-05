using System;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using HR.Web.Data;
using HR.Web.Models;
using HR.Web.Services;

namespace HR.Web.Controllers
{
    public class PositionsController : Controller
    {
        private readonly UnitOfWork _uow = new UnitOfWork();
        private readonly AuditService _auditService = new AuditService();

        public ActionResult Index()
        {
            var positions = _uow.Positions.GetAll(p => p.Department);
            
            // Filter positions based on user role
            if (User == null || !User.IsInRole("Admin"))
            {
                // For non-admin users (clients), only show open positions
                positions = positions.Where(p => p.IsOpen);
            }
            
            var orderedPositions = positions.OrderByDescending(p => p.PostedOn);
            
            // Pass additional view data for filtering
            ViewBag.IsAdmin = User != null && User.IsInRole("Admin");
            ViewBag.AllPositions = ViewBag.IsAdmin ? 
                _uow.Positions.GetAll(p => p.Department).OrderByDescending(p => p.PostedOn) : 
                orderedPositions;
            
            return View(orderedPositions);
        }

        public ActionResult Details(int id)
        {
            var position = _uow.Positions.GetAll(p => p.Department)
                .FirstOrDefault(p => p.Id == id);
            if (position == null)
            {
                return HttpNotFound();
            }
            
            // Prevent non-admin users from accessing closed positions
            if (position != null && !position.IsOpen && (User == null || !User.IsInRole("Admin")))
            {
                return new HttpStatusCodeResult(403, "This position is not available for application.");
            }
            
            return View(position);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Create()
        {
            ViewBag.DepartmentId = new SelectList(_uow.Departments.GetAll(), "Id", "Name");
            // Load all questions (not just active ones) with their options so admin can see all available questions
            ViewBag.QuestionList = _uow.Questions.GetAll(q => q.QuestionOptions).ToList();
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
                        Debug.WriteLine($"[PositionsController.Create][ModelError] Key='{kvp.Key}', Error='{err.ErrorMessage}', Exception='{err.Exception?.Message}'");
                    }
                }
                ViewBag.DepartmentId = new SelectList(_uow.Departments.GetAll(), "Id", "Name", model.DepartmentId);
                ViewBag.QuestionList = _uow.Questions.GetAll(q => q.QuestionOptions).Where(q => q.IsActive).ToList();
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
                
                // Log position creation
                var newValues = new { 
                    Title = model.Title, 
                    Description = model.Description, 
                    Responsibilities = model.Responsibilities,
                    Qualifications = model.Qualifications,
                    DepartmentId = model.DepartmentId,
                    Location = model.Location,
                    IsOpen = model.IsOpen,
                    PostedOn = model.PostedOn
                };
                _auditService.LogCreate(User.Identity.Name, "Positions", model.Id.ToString(), newValues);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[PositionsController.Create][POST] Exception during save: " + ex);
                var msg = ex.GetBaseException()?.Message ?? ex.Message;
                
                // Log failed creation
                _auditService.LogAction(User.Identity.Name, "CREATE", "Positions", "new", 
                    wasSuccessful: false, errorMessage: msg);
                
                ModelState.AddModelError("", "Unable to save position: " + msg);
                ViewBag.DepartmentId = new SelectList(_uow.Departments.GetAll(), "Id", "Name", model.DepartmentId);
                ViewBag.QuestionList = _uow.Questions.GetAll(q => q.QuestionOptions).Where(q => q.IsActive).ToList();
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
                
                // Log question linking
                _auditService.LogAction(User.Identity.Name, "LINK_QUESTIONS", "Positions", model.Id.ToString(), 
                    new { QuestionIds = selectedQuestions, QuestionCount = selectedQuestions.Length });
            }

            TempData["Message"] = "Position created successfully.";
            Debug.WriteLine("[PositionsController.Create][POST] Redirecting to Index.");
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Edit(int id)
        {
            var position = _uow.Positions.GetAll(p => p.PositionQuestions).FirstOrDefault(p => p.Id == id);
            if (position == null)
            {
                return HttpNotFound();
            }

            ViewBag.DepartmentId = new SelectList(_uow.Departments.GetAll(), "Id", "Name", position.DepartmentId);
            
            // Load all questions (include inactive ones too so admin can see everything) with their options
            var allQuestions = _uow.Questions.GetAll(q => q.QuestionOptions).ToList();
            ViewBag.QuestionList = allQuestions;
            Debug.WriteLine($"[PositionsController.Edit] Loaded {allQuestions.Count} questions from database.");
            
            // Debug: Check each question and its options
            foreach (var q in allQuestions)
            {
                Debug.WriteLine($"Question: {q.Text} (Type: {q.Type})");
                Debug.WriteLine($"Options count: {q.QuestionOptions?.Count() ?? 0}");
                if (q.QuestionOptions != null)
                {
                    foreach (var opt in q.QuestionOptions)
                    {
                        Debug.WriteLine($"  - Option: {opt.Text} (Points: {opt.Points})");
                    }
                }
            }
            
            // Also check if there are any QuestionOptions in the database at all
            var allOptions = _uow.Context.Set<QuestionOption>().ToList();
            Debug.WriteLine($"[PositionsController.Edit] Total QuestionOptions in database: {allOptions.Count}");
            foreach (var opt in allOptions.Take(5))
            {
                Debug.WriteLine($"  - Option ID {opt.Id}: {opt.Text} (QuestionId: {opt.QuestionId})");
            }
            
            // Get currently selected question IDs for pre-checking
            var selectedQuestionIds = position.PositionQuestions?.Select(pq => pq.QuestionId).ToList() ?? new System.Collections.Generic.List<int>();
            ViewBag.SelectedQuestionIds = selectedQuestionIds;
            
            return View(position);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(Position model, int[] selectedQuestions)
        {
            Debug.WriteLine("[PositionsController.Edit][POST] Entered at " + DateTime.UtcNow);
            Debug.WriteLine($"Title='{model?.Title}', DeptId={model?.DepartmentId}, IsOpen={model?.IsOpen}");
            Debug.WriteLine("ModelState.IsValid = " + ModelState.IsValid);

            if (!ModelState.IsValid)
            {
                ViewBag.DepartmentId = new SelectList(_uow.Departments.GetAll(), "Id", "Name", model.DepartmentId);
                ViewBag.QuestionList = _uow.Questions.GetAll(q => q.QuestionOptions).Where(q => q.IsActive).ToList();
                var selectedQuestionIds = selectedQuestions != null ? selectedQuestions.ToList() : new System.Collections.Generic.List<int>();
                ViewBag.SelectedQuestionIds = selectedQuestionIds;
                Debug.WriteLine("[PositionsController.Edit][POST] Returning view due to invalid ModelState.");
                return View(model);
            }

            // ensure a department was selected
            if (model.DepartmentId <= 0)
            {
                ModelState.AddModelError("DepartmentId", "Please select a department.");
                ViewBag.DepartmentId = new SelectList(_uow.Departments.GetAll(), "Id", "Name", model.DepartmentId);
                ViewBag.QuestionList = _uow.Questions.GetAll(q => q.QuestionOptions).Where(q => q.IsActive).ToList();
                var selectedQuestionIds = selectedQuestions != null ? selectedQuestions.ToList() : new System.Collections.Generic.List<int>();
                ViewBag.SelectedQuestionIds = selectedQuestionIds;
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

                Debug.WriteLine("[PositionsController.Edit][POST] Updating position and saving...");
                _uow.Positions.Update(existingPosition);
                _uow.Complete();
                Debug.WriteLine("[PositionsController.Edit][POST] Save succeeded.");

                // Update selected questions for this position
                // First, get existing PositionQuestions
                var existingPositionQuestions = _uow.PositionQuestions.GetAll()
                    .Where(pq => pq.PositionId == model.Id)
                    .ToList();

                // Get selected question IDs (empty array if none selected)
                var selectedQuestionIds = selectedQuestions != null ? selectedQuestions.ToList() : new System.Collections.Generic.List<int>();

                // Remove PositionQuestions that are no longer selected
                foreach (var existingPq in existingPositionQuestions)
                {
                    if (!selectedQuestionIds.Contains(existingPq.QuestionId))
                    {
                        _uow.PositionQuestions.Remove(existingPq);
                    }
                }

                // Get currently assigned question IDs
                var currentlyAssignedQuestionIds = existingPositionQuestions.Select(pq => pq.QuestionId).ToList();

                // Add new PositionQuestions for newly selected questions
                int maxOrder = existingPositionQuestions.Any() ? existingPositionQuestions.Max(pq => pq.Order) : 0;
                foreach (var questionId in selectedQuestionIds)
                {
                    if (!currentlyAssignedQuestionIds.Contains(questionId))
                    {
                        var newPq = new PositionQuestion
                        {
                            PositionId = model.Id,
                            QuestionId = questionId,
                            Order = ++maxOrder
                        };
                        _uow.PositionQuestions.Add(newPq);
                    }
                }

                _uow.Complete();
                Debug.WriteLine("[PositionsController.Edit][POST] Updated position questions.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[PositionsController.Edit][POST] Exception during save: " + ex);
                var msg = ex.GetBaseException()?.Message ?? ex.Message;
                ModelState.AddModelError("", "Unable to save position: " + msg);
                ViewBag.DepartmentId = new SelectList(_uow.Departments.GetAll(), "Id", "Name", model.DepartmentId);
                ViewBag.QuestionList = _uow.Questions.GetAll(q => q.QuestionOptions).Where(q => q.IsActive).ToList();
                var selectedQuestionIds = selectedQuestions != null ? selectedQuestions.ToList() : new System.Collections.Generic.List<int>();
                ViewBag.SelectedQuestionIds = selectedQuestionIds;
                Debug.WriteLine("[PositionsController.Edit][POST] Returning view due to exception.");
                return View(model);
            }

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        public ActionResult DatabaseTest()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        public ActionResult CreateTestData()
        {
            try
            {
                // Create a test choice question with options
                var testQuestion = new Question
                {
                    Text = "What is your preferred work environment?",
                    Type = "Choice",
                    IsActive = true
                };
                _uow.Questions.Add(testQuestion);
                _uow.Complete();
                
                // Add options for the question
                var options = new[]
                {
                    new QuestionOption { QuestionId = testQuestion.Id, Text = "Remote", Points = 5 },
                    new QuestionOption { QuestionId = testQuestion.Id, Text = "Office", Points = 3 },
                    new QuestionOption { QuestionId = testQuestion.Id, Text = "Hybrid", Points = 4 }
                };
                
                foreach (var option in options)
                {
                    _uow.Context.Set<QuestionOption>().Add(option);
                }
                _uow.Complete();
                
                return Json(new { success = true, message = "Test data created successfully", questionId = testQuestion.Id, optionsCount = options.Length });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        public ActionResult TestEagerLoading()
        {
            try
            {
                // Test eager loading - get questions with options
                var questionsWithOptions = _uow.Questions.GetAll(q => q.QuestionOptions).ToList();
                
                var testResult = new
                {
                    Success = true,
                    TotalQuestions = questionsWithOptions.Count,
                    QuestionsWithOptions = questionsWithOptions.Count(q => q.QuestionOptions != null && q.QuestionOptions.Any()),
                    Questions = questionsWithOptions.Select(q => new
                    {
                        q.Id,
                        q.Text,
                        q.Type,
                        HasOptions = q.QuestionOptions != null,
                        OptionsCount = q.QuestionOptions?.Count() ?? 0,
                        Options = q.QuestionOptions?.Select(o => new
                        {
                            o.Id,
                            o.Text,
                            o.Points
                        }).ToList()
                    }).ToList()
                };
                
                return Json(testResult, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [Authorize(Roles = "Admin")]
        public ActionResult TestQuestionOptions()
        {
            var allOptions = _uow.Context.Set<QuestionOption>().ToList();
            var allQuestions = _uow.Questions.GetAll(q => q.QuestionOptions).ToList();
            
            var result = new
            {
                TotalQuestions = allQuestions.Count,
                TotalOptions = allOptions.Count,
                Questions = allQuestions.Select(q => new
                {
                    q.Id,
                    q.Text,
                    q.Type,
                    OptionsCount = q.QuestionOptions?.Count() ?? 0
                }).ToList(),
                Options = allOptions.Select(o => new
                {
                    o.Id,
                    o.Text,
                    o.Points,
                    o.QuestionId
                }).ToList()
            };
            
            return Json(result, JsonRequestBehavior.AllowGet);
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

            try
            {
                // Use the context directly for more control over deletion order
                var context = _uow.Context;
                
                // Debug: Check what applications exist for this position
                var applications = context.Applications.Where(a => a.PositionId == id).ToList();
                var applicationIds = applications.Select(a => a.Id).ToList();
                
                // Debug: Log what we found
                System.Diagnostics.Debug.WriteLine($"Found {applications.Count} applications for position {id}");
                foreach (var app in applications)
                {
                    System.Diagnostics.Debug.WriteLine($"Application ID: {app.Id}, Applicant: {app.ApplicantId}");
                }
                
                // Step 1: Delete PositionQuestions first
                var positionQuestions = context.PositionQuestions.Where(pq => pq.PositionId == id).ToList();
                
                // Delete the PositionQuestions themselves
                context.PositionQuestions.RemoveRange(positionQuestions);
                
                // Save changes for PositionQuestions first
                _uow.Complete();
                
                // Step 2: Delete ALL application-related entities in the correct order
                
                // Delete ApplicationAnswers for these applications
                var applicationAnswers = context.ApplicationAnswers.Where(aa => applicationIds.Contains(aa.ApplicationId));
                context.ApplicationAnswers.RemoveRange(applicationAnswers);
                
                // Delete Interviews for these applications
                var interviews = context.Interviews.Where(i => applicationIds.Contains(i.ApplicationId));
                context.Interviews.RemoveRange(interviews);
                
                // Delete Onboardings for these applications
                var onboardings = context.Onboardings.Where(o => applicationIds.Contains(o.ApplicationId));
                context.Onboardings.RemoveRange(onboardings);
                
                // Save changes for application-related entities
                _uow.Complete();
                
                // Step 3: Delete the applications themselves
                context.Applications.RemoveRange(applications);
                _uow.Complete();
                
                // Debug: Verify applications are deleted
                var remainingApps = context.Applications.Where(a => a.PositionId == id).ToList();
                System.Diagnostics.Debug.WriteLine($"Remaining applications after deletion: {remainingApps.Count}");
                
                // Step 4: Finally delete the position
                context.Positions.Remove(position);
                _uow.Complete();

                // Log the deletion
                var username = User.Identity.Name;
                _auditService.LogAction(username, "DELETE_POSITION", "Position", id.ToString(), 
                    $"Position '{position.Title}' and {applications.Count} associated applications deleted");

                TempData["SuccessMessage"] = $"Position '{position.Title}' and {applications.Count} associated applications have been deleted successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Log the error
                var username = User.Identity.Name;
                _auditService.LogAction(username, "DELETE_POSITION_ERROR", "Position", id.ToString(), 
                    $"Error deleting position: {ex.Message}");

                ModelState.AddModelError("", "Unable to delete position. Please ensure there are no related records preventing deletion.");
                return View(position);
            }
        }
    }
}










