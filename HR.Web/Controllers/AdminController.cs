using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using HR.Web.Data;
using HR.Web.Models;
using HR.Web.ViewModels;
using Newtonsoft.Json;

namespace HR.Web.Controllers
{
    /// <summary>
    /// Admin controller for managing candidates, applications, and rankings
    /// Allows admins to view candidates ranked by position, filter, and manage applications
    /// </summary>
    [Authorize(Roles = "Admin,HR")]
    public partial class AdminController : Controller
    {
        private readonly UnitOfWork _uow = new UnitOfWork();

        /// <summary>
        /// Display candidates ranked by position with filtering capability
        /// 
        /// Query Parameters:
        /// - positionId (optional): Filter candidates by specific position ID
        /// 
        /// Features:
        /// - Groups candidates by position they applied for
        /// - Ranks candidates within each position by total score
        /// - Shows quick stats (applicant count per position)
        /// - Expandable details for each candidate
        /// - Action links to view details and schedule interviews
        /// </summary>
        public ActionResult CandidateRankings(int? positionId)
        {
            /* TODO: Implement the actual data retrieval logic:
             * 
             * var viewModel = new CandidateRankingsViewModel
             * {
             *     // Get all positions for the filter dropdown
             *     Positions = db.Positions
             *         .Where(p => p.IsOpen)
             *         .OrderBy(p => p.Title)
             *         .ToList(),
             *     
             *     // Get applications grouped by position
             *     CandidatesByPosition = db.Applications
             *         .Where(a => !positionId.HasValue || a.PositionId == positionId)
             *         .Include(a => a.Position)
             *         .Include(a => a.Position.Department)
             *         .Include(a => a.User)
             *         .GroupBy(a => a.Position)
             *         .ToDictionary(
             *             g => g.Key,
             *             g => g.Select(app => new CandidateApplicationScore
             *             {
             *                 ApplicationId = app.Id,
             *                 CandidateName = app.User.FullName,
             *                 CandidateEmail = app.User.Email,
             *                 TotalScore = CalculateTotalScore(app), // Implement scoring logic
             *                 QuestionnaireScore = app.QuestionnaireScore ?? 0,
             *                 MaxQuestionnaireScore = app.Position.MaxScore ?? 100,
             *                 AppliedDate = app.AppliedDate,
             *                 Status = app.Status,
             *                 PositionId = app.PositionId
             *             }).ToList()
             *         )
             * };
             * 
             * ViewBag.SelectedPositionId = positionId;
             * return View(viewModel);
             */

            // Get all applications with related data
            var applications = _uow.Applications.GetAll(
                a => a.Applicant,
                a => a.Position,
                a => a.Position.Department
            ).ToList();

            // Filter by position if specified
            if (positionId.HasValue)
            {
                applications = applications.Where(a => a.PositionId == positionId.Value).ToList();
            }

            // Group applications by position
            var candidatesByPosition = new Dictionary<Position, List<CandidateApplicationScore>>();
            
            foreach (var application in applications)
            {
                if (application.Position == null) continue;

                if (!candidatesByPosition.ContainsKey(application.Position))
                {
                    candidatesByPosition[application.Position] = new List<CandidateApplicationScore>();
                }

                var candidateScore = new CandidateApplicationScore
                {
                    ApplicationId = application.Id,
                    CandidateName = application.Applicant != null ? application.Applicant.FullName : "Unknown",
                    CandidateEmail = application.Applicant != null ? application.Applicant.Email : "",
                    TotalScore = application.Score ?? 0,
                    QuestionnaireScore = application.Score ?? 0,
                    MaxQuestionnaireScore = 100,
                    AppliedDate = application.AppliedOn,
                    Status = application.Status ?? "Pending",
                    PositionId = application.PositionId
                };

                candidatesByPosition[application.Position].Add(candidateScore);
            }

            // Sort candidates within each position by score (descending)
            foreach (var position in candidatesByPosition.Keys.ToList())
            {
                candidatesByPosition[position] = candidatesByPosition[position]
                    .OrderByDescending(c => c.TotalScore)
                    .ToList();
            }

            // Get all positions for the filter dropdown
            var allPositions = _uow.Positions.GetAll(p => p.Department).ToList();

            var viewModel = new CandidateRankingsViewModel
            {
                Positions = allPositions,
                CandidatesByPosition = candidatesByPosition
            };

            ViewBag.SelectedPositionId = positionId;
            return View(viewModel);
        }

        /// <summary>
        /// Helper method to calculate total score for a candidate
        /// 
        /// Scoring Logic:
        /// - Questionnaire responses weighted by importance
        /// - Any additional scoring factors (e.g., resume score, experience match)
        /// - Returns normalized score (typically 0-100)
        /// </summary>
        private decimal CalculateTotalScore(dynamic application)
        {
            /* TODO: Implement your scoring algorithm
             * Example:
             * var questionnaireScore = (application.QuestionnaireScore ?? 0) * 0.7m;
             * var resumeScore = (application.ResumeScore ?? 0) * 0.3m;
             * return questionnaireScore + resumeScore;
             */
            return 0;
        }

        /// <summary>
        /// Display detailed information about a candidate's application
        /// Used when admin clicks "View Details" from rankings view
        /// </summary>
        public ActionResult ViewApplicationDetails(int applicationId)
        {
            var application = _uow.Applications.Get(applicationId);
            if (application == null)
            {
                return HttpNotFound();
            }
            return View(application);
        }

        /// <summary>
        /// Initiate interview scheduling workflow for a candidate
        /// </summary>
        public ActionResult ScheduleInterview(int applicationId)
        {
            /* TODO: Implement interview scheduling
             * var application = db.Applications.Find(applicationId);
             * if (application == null)
             *     return HttpNotFound();
             * 
             * return View(new InterviewScheduleViewModel { ApplicationId = applicationId });
             */
            return HttpNotFound();
        }

        /* 
         * FUTURE ENHANCEMENTS:
         * 
         * 1. Bulk Actions:
         *    - Select multiple candidates for rejection/advancement
         *    - Send bulk emails to shortlisted candidates
         * 
         * 2. Advanced Filtering:
         *    - Filter by score range
         *    - Filter by application date
         *    - Filter by status (Pending, Shortlisted, Rejected)
         * 
         * 3. Sorting Options:
         *    - Sort by score ascending/descending
         *    - Sort by application date
         *    - Sort by last name
         * 
         * 4. Export:
         *    - Export rankings to Excel/CSV
         *    - Generate reports by position
         * 
         * 5. Analytics:
         *    - Average score by position
         *    - Application trends
         *    - Candidate pool quality metrics
         */
        // Questions management (CRUD)
        [Authorize(Roles = "Admin")]
        public ActionResult Questions()
        {
            // Use eager loading to get questions with their options in one query
            var questions = _uow.Questions.GetAll(q => q.QuestionOptions).ToList();
            var list = questions
                .Select(q => new QuestionAdminViewModel
                {
                    Id = q.Id,
                    Text = q.Text,
                    Type = q.Type,
                    IsActive = q.IsActive,
                    Options = q.QuestionOptions.Select(o => new QuestionOptionVM
                    {
                        Id = o.Id,
                        Text = o.Text,
                        Points = o.Points
                    }).ToList()
                }).ToList();
            // Ensure positions are available for consolidated AI generation modal
            ViewBag.Positions = _uow.Positions.GetAll().ToList();
            // Use the combined AI-enhanced questions view
            return View("QuestionsWithMCP", list);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult EditQuestion(int? id)
        {
            if (id == null)
            {
                return View(new QuestionAdminViewModel { IsActive = true });
            }
            var question = _uow.Questions.GetAll(q => q.QuestionOptions).FirstOrDefault(x => x.Id == id.Value);
            if (question == null)
                return HttpNotFound();
            var vm = new QuestionAdminViewModel
            {
                Id = question.Id,
                Text = question.Text,
                Type = question.Type,
                IsActive = question.IsActive,
                Options = question.QuestionOptions.Select(o => new QuestionOptionVM
                {
                    Id = o.Id,
                    Text = o.Text,
                    Points = o.Points
                }).ToList()
            };
            return View(vm);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public ActionResult EditQuestion(QuestionAdminViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            Question q;
            if (model.Id.HasValue)
            {
                q = _uow.Questions.Get(model.Id.Value);
                if (q == null) return HttpNotFound();
                q.Text = model.Text;
                q.Type = model.Type;
                q.IsActive = model.IsActive;
                _uow.Questions.Update(q);
                var oldOptions = _uow.Context.Set<QuestionOption>().Where(o => o.QuestionId == q.Id);
                _uow.Context.Set<QuestionOption>().RemoveRange(oldOptions);
            }
            else
            {
                // create new
                q = new Question
                {
                    Text = model.Text,
                    Type = model.Type,
                    IsActive = model.IsActive
                };
                _uow.Questions.Add(q);
                _uow.Complete();
            }
            _uow.Complete(); // Save question so it exists for option linking

            // Add options (allowed for any question type)
            if (model.Options != null)
            {
                foreach (var opt in model.Options)
                {
                    if (!string.IsNullOrWhiteSpace(opt.Text))
                    {
                        var newOpt = new QuestionOption
                        {
                            QuestionId = q.Id,
                            Text = opt.Text,
                            Points = opt.Points
                        };
                        _uow.Context.Set<QuestionOption>().Add(newOpt);
                    }
                }
            }
            _uow.Complete();
            TempData["Message"] = "Question saved.";
            return RedirectToAction("Questions");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteQuestion(int id)
        {
            var q = _uow.Questions.Get(id);
            if (q == null) return HttpNotFound();
            
            try
            {
                // Delete related records in proper order due to foreign key constraints
                
                // 1. Delete ApplicationAnswer records that reference this question
                var applicationAnswers = _uow.Context.Set<ApplicationAnswer>().Where(aa => aa.QuestionId == id);
                _uow.Context.Set<ApplicationAnswer>().RemoveRange(applicationAnswers);
                
                // 2. Delete PositionQuestionOption records (through QuestionOptions)
                var questionOptions = _uow.Context.Set<QuestionOption>().Where(qo => qo.QuestionId == id).ToList();
                foreach (var option in questionOptions)
                {
                    // Delete PositionQuestionOption records that reference this QuestionOption
                    var positionQuestionOptions = _uow.Context.Set<PositionQuestionOption>().Where(pqo => pqo.QuestionOptionId == option.Id);
                    _uow.Context.Set<PositionQuestionOption>().RemoveRange(positionQuestionOptions);
                }
                
                // 3. Delete QuestionOption records
                _uow.Context.Set<QuestionOption>().RemoveRange(questionOptions);
                
                // 4. Delete PositionQuestion records that reference this question
                var positionQuestions = _uow.Context.Set<PositionQuestion>().Where(pq => pq.QuestionId == id);
                _uow.Context.Set<PositionQuestion>().RemoveRange(positionQuestions);
                
                // 5. Finally delete the question itself
                _uow.Questions.Remove(q);
                
                _uow.Complete();
                TempData["Message"] = "Question deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error deleting question: " + ex.Message;
                // Log the full exception for debugging
                System.Diagnostics.Debug.WriteLine("DeleteQuestion Error: " + ex.ToString());
            }
            
            return RedirectToAction("Questions");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public ActionResult BatchDeleteQuestions(int[] questionIds)
        {
            if (questionIds == null || questionIds.Length == 0)
            {
                return Json(new { success = false, message = "No questions selected for deletion." });
            }

            try
            {
                int deletedCount = 0;
                
                foreach (var id in questionIds)
                {
                    var q = _uow.Questions.Get(id);
                    if (q == null) continue;
                    
                    // Delete related records in proper order due to foreign key constraints
                    
                    // 1. Delete ApplicationAnswer records that reference this question
                    var applicationAnswers = _uow.Context.Set<ApplicationAnswer>().Where(aa => aa.QuestionId == id);
                    _uow.Context.Set<ApplicationAnswer>().RemoveRange(applicationAnswers);
                    
                    // 2. Delete PositionQuestionOption records (through QuestionOptions)
                    var questionOptions = _uow.Context.Set<QuestionOption>().Where(qo => qo.QuestionId == id).ToList();
                    foreach (var option in questionOptions)
                    {
                        // Delete PositionQuestionOption records that reference this QuestionOption
                        var positionQuestionOptions = _uow.Context.Set<PositionQuestionOption>().Where(pqo => pqo.QuestionOptionId == option.Id);
                        _uow.Context.Set<PositionQuestionOption>().RemoveRange(positionQuestionOptions);
                    }
                    
                    // 3. Delete QuestionOption records
                    _uow.Context.Set<QuestionOption>().RemoveRange(questionOptions);
                    
                    // 4. Delete PositionQuestion records that reference this question
                    var positionQuestions = _uow.Context.Set<PositionQuestion>().Where(pq => pq.QuestionId == id);
                    _uow.Context.Set<PositionQuestion>().RemoveRange(positionQuestions);
                    
                    // 5. Finally delete the question itself
                    _uow.Questions.Remove(q);
                    
                    deletedCount++;
                }
                
                _uow.Complete();
                TempData["Message"] = $"Successfully deleted {deletedCount} question(s).";
                return Json(new { success = true, message = $"Successfully deleted {deletedCount} question(s)." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting questions: " + ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public ActionResult AddToSampleQuestions(string questionsJson)
        {
            try
            {
                // This method is similar to AddGeneratedQuestionsToSample but works with existing questions
                var questions = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(questionsJson);
                
                // Check for duplicates in the existing sample questions
                var existingQuestions = _uow.Questions.GetAll().ToList();
                var duplicates = new List<object>();
                var newQuestions = new List<object>();

                foreach (var question in questions)
                {
                    var questionText = question["text"].ToString();
                    var questionType = question["type"].ToString();

                    // Check for similar questions
                    var similarQuestion = existingQuestions.FirstOrDefault(eq => 
                        eq.Text.ToLower().Contains(questionText.ToLower().Substring(0, Math.Min(50, questionText.Length))) ||
                        questionText.ToLower().Contains(eq.Text.ToLower().Substring(0, Math.Min(50, eq.Text.Length))));

                    if (similarQuestion != null)
                    {
                        duplicates.Add(new
                        {
                            id = question["id"],
                            text = questionText,
                            type = questionType,
                            existingQuestionId = similarQuestion.Id,
                            existingQuestionText = similarQuestion.Text,
                            existingQuestionType = similarQuestion.Type
                        });
                    }
                    else
                    {
                        newQuestions.Add(new
                        {
                            questionData = question
                        });
                    }
                }

                if (duplicates.Any())
                {
                    return Json(new { 
                        success = true, 
                        requiresDecision = true,
                        duplicates = duplicates, 
                        newQuestions = newQuestions,
                        message = $"Found {duplicates.Count} potential duplicates. Please review before adding."
                    });
                }
                else
                {
                    // No duplicates, just add them to sample (they're already in the main question bank)
                    return Json(new { 
                        success = true, 
                        requiresDecision = false,
                        message = $"All {questions.Count} questions are already in the question bank."
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error adding questions to sample: " + ex.Message });
            }
        }

        public ActionResult TestQuestions()
        {
            // Return a simple response to verify routing works without a view dependency
            return Content("Test Works");
        }
    }
}
