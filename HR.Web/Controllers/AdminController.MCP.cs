using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Data.Entity;
using HR.Web.Models;
using HR.Web.Services;
using HR.Web.ViewModels;
using Newtonsoft.Json;

namespace HR.Web.Controllers
{
    /// <summary>
    /// MCP-enhanced admin controller methods
    /// </summary>
    public partial class AdminController : Controller
    {
        private readonly MCPService _mcpService = new MCPService();
        private const bool DEV_STUB_MCP = false; // set false to use real MCP

        /// <summary>
        /// Generate questions using MCP based on job description
        /// </summary>
        // GET: Admin/GenerateQuestions
        [HttpGet]
        public ActionResult GenerateQuestions()
        {
            return View(new GenerateQuestionsViewModel());
        }

        // GET: Admin/RankingTest
        [HttpGet]
        public ActionResult RankingTest()
        {
            return View();
        }

        // POST: Admin/GenerateQuestions
        [HttpPost]
        [AllowAnonymous]
        [OverrideAuthorization]
        public async Task<ActionResult> GenerateQuestions(string jobTitle, string jobDescription, string keyResponsibilities, string requiredQualifications, int count, string experience = "mid", string[] questionTypes = null)
        {
            try
            {
                if (DEV_STUB_MCP)
                {
                    // Generate intelligent questions using AI analysis
                    var jobAnalysis = await AnalyzeJobRequirements(jobTitle, jobDescription, experience);
                    var contextualQuestions = await GenerateContextualQuestions(jobAnalysis, count, questionTypes);
                    
                    var stub = new GeneratedQuestionsResponse
                    {
                        success = true,
                        metadata = new Metadata { 
                            jobTitle = jobTitle, 
                            experience = experience, 
                            keywords = jobAnalysis.KeyRequirements.Take(5).ToList(), 
                            generatedAt = DateTime.UtcNow.ToString("o"),
                            analysis = jobAnalysis
                        },
                        questions = contextualQuestions
                    };
                    return Json(new { success = true, questions = stub.questions, metadata = stub.metadata }, JsonRequestBehavior.AllowGet);
                }

                if (string.IsNullOrEmpty(jobTitle) || string.IsNullOrEmpty(jobDescription))
                {
                    return Json(new { success = false, message = "Job title and description are required" });
                }

                if (count <= 0)
                {
                    return Json(new { success = false, message = "Number of questions must be greater than 0" });
                }

                if (count > 25)
                {
                    return Json(new { success = false, message = "Number of questions cannot exceed 25" });
                }

                questionTypes = questionTypes ?? new[] { "Text", "Choice", "Number", "Rating" };
                
                var parameters = new
                {
                    jobTitle,
                    jobDescription,
                    keyResponsibilities,
                    requiredQualifications,
                    experience,
                    questionTypes,
                    count
                };
                
                // Debug: Check MCP connection first
                var isConnected = await _mcpService.TestConnectionAsync();
                System.Diagnostics.Debug.WriteLine($"MCP Connected: {isConnected}");
                
                // Guard with timeout to avoid UI hangs
                var callTask = _mcpService.CallToolAsync("generate-questions", parameters);
                var completed = await Task.WhenAny(callTask, Task.Delay(30000)); // Increased timeout to 30 seconds
                MCPResponse response;
                if (completed == callTask)
                {
                    response = callTask.Result;
                    System.Diagnostics.Debug.WriteLine("MCP Response received successfully");
                    System.Diagnostics.Debug.WriteLine($"Response success: {response.Success}");
                    if (response.Success && response.Result != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Response result type: {response.Result.GetType()}");
                        System.Diagnostics.Debug.WriteLine($"Response result: {response.Result}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("MCP timeout - using fallback generation");
                    System.Diagnostics.Debug.WriteLine($"Timeout occurred after 30 seconds");
                    // Generate fallback questions based on job description
                    var stubQuestions = new List<GeneratedQuestion>();
                    var templates = new[]
                    {
                        new { Text = "Describe your experience with {0}.", Type = "Text", Category = "experience" },
                        new { Text = "How would you handle a situation involving {0}?", Type = "Text", Category = "situational" },
                        new { Text = "Rate your proficiency in {0} (1-5).", Type = "Rating", Category = "skill" },
                        new { Text = "Which of the following {0} approaches do you prefer?", Type = "Choice", Category = "preference" },
                        new { Text = "What steps would you take to {0}?", Type = "Text", Category = "process" },
                        new { Text = "Give an example of when you {0}.", Type = "Text", Category = "behavioral" },
                        new { Text = "How do you prioritize {0}?", Type = "Text", Category = "organizational" },
                        new { Text = "What is your approach to {0}?", Type = "Text", Category = "methodology" },
                        new { Text = "Select your experience level with {0}.", Type = "Choice", Category = "experience" },
                        new { Text = "Describe a time you dealt with {0}.", Type = "Text", Category = "behavioral" }
                    };
                    
                    // Extract relevant topics from job description
                    var topics = ExtractJobTopics(jobDescription, jobTitle);
                    var rand = new Random();
                    
                    for (int i = 0; i < count; i++)
                    {
                        var tpl = templates[rand.Next(templates.Length)];
                        var topic = topics[rand.Next(topics.Count)];
                        var questionText = string.Format(tpl.Text, topic);
                        
                        var options = new List<HR.Web.Services.MCPQuestionOption>();
                        if (tpl.Type == "Choice")
                        {
                            var choices = topic.Contains("tools") || topic.Contains("ORM") || topic.Contains("framework")
                                ? new[] { "Microsoft Office", "Google Workspace", "LibreOffice", "Other" }
                                : topic.Contains("communication") || topic.Contains("customer")
                                ? new[] { "Phone", "Email", "In-person", "Video conference" }
                                : topic.Contains("organization") || topic.Contains("planning")
                                ? new[] { "Trello", "Asana", "Jira", "Microsoft Planner" }
                                : topic.Contains("experience") || topic.Contains("skill")
                                ? new[] { "Entry-level", "Mid-level", "Senior-level", "Executive-level" }
                                : new[] { "Beginner", "Intermediate", "Advanced", "Expert" };
                            for (int c = 0; c < choices.Length; c++)
                            {
                                options.Add(new MCPQuestionOption { text = choices[c], points = (choices.Length - c) * 2 });
                            }
                        }
                        else if (tpl.Type == "Rating")
                        {
                            for (int r = 1; r <= 5; r++)
                            {
                                options.Add(new MCPQuestionOption { text = r.ToString(), points = r * 2 });
                            }
                        }
                        
                        stubQuestions.Add(new GeneratedQuestion
                        {
                            text = questionText,
                            type = tpl.Type,
                            category = tpl.Category,
                            suggestedOptions = options
                        });
                    }
                    
                    var fallbackResponse = new GeneratedQuestionsResponse
                    {
                        success = true,
                        metadata = new Metadata { jobTitle = jobTitle, experience = experience, keywords = topics.Take(3).ToList(), generatedAt = DateTime.UtcNow.ToString("o") },
                        questions = stubQuestions
                    };
                    response = new MCPResponse
                    {
                        Result = new MCPResult
                        {
                            contents = new List<MCPContent>{ new MCPContent{ type = "text", text = JsonConvert.SerializeObject(fallbackResponse) } }
                        }
                    };
                }
                
                if (response.Success)
                {
                    var content = response.Result.contents[0];
                    var generatedQuestions = JsonConvert.DeserializeObject<GeneratedQuestionsResponse>(content.text);
                    
                    return Json(new { 
                        success = true, 
                        questions = generatedQuestions.questions,
                        metadata = generatedQuestions.metadata
                    }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { success = false, message = response.ErrorMessage }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Validate a question for bias and quality
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,HR")]
        public async Task<ActionResult> ValidateQuestion(string question, string questionType, string optionsJson = null)
        {
            try
            {
                var parameters = new
                {
                    question,
                    questionType,
                    options = string.IsNullOrEmpty(optionsJson) ? null : JsonConvert.DeserializeObject(optionsJson)
                };

                var response = await _mcpService.CallToolAsync("validate-question", parameters);
                
                if (response.Success)
                {
                    var content = response.Result.contents[0];
                    var validationResult = JsonConvert.DeserializeObject<ValidationResponse>(content.text);
                    
                    return Json(new { 
                        success = true, 
                        validation = validationResult.validation,
                        recommendations = validationResult.recommendations
                    });
                }
                else
                {
                    return Json(new { success = false, message = response.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get point suggestions for question options
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,HR")]
        public async Task<ActionResult> SuggestPoints(string question, string[] options, string difficulty = "intermediate")
        {
            try
            {
                var parameters = new
                {
                    question,
                    options,
                    difficulty
                };

                var response = await _mcpService.CallToolAsync("suggest-points", parameters);
                
                if (response.Success)
                {
                    var content = response.Result.contents[0];
                    var pointsSuggestion = JsonConvert.DeserializeObject<PointsSuggestionResponse>(content.text);
                    
                    return Json(new { 
                        success = true, 
                        suggestions = pointsSuggestion.suggestions,
                        totalPoints = pointsSuggestion.totalPoints
                    });
                }
                else
                {
                    return Json(new { success = false, message = response.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Import a questionnaire template
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,HR")]
        public async Task<ActionResult> ImportTemplate(string templateType, bool customize = true)
        {
            try
            {
                var parameters = new
                {
                    templateType,
                    customize
                };

                var response = await _mcpService.CallToolAsync("import-template", parameters);
                
                if (response.Success)
                {
                    var content = response.Result.contents[0];
                    var templateResponse = JsonConvert.DeserializeObject<TemplateResponse>(content.text);
                    
                    return Json(new { 
                        success = true, 
                        template = templateResponse.template,
                        customizable = templateResponse.customizable
                    });
                }
                else
                {
                    return Json(new { success = false, message = response.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get available MCP resources
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,HR")]
        public async Task<ActionResult> GetMCPResources()
        {
            try
            {
                var resources = await _mcpService.ListResourcesAsync();
                return Json(new { success = true, resources }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Get specific MCP resource content
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,HR")]
        public async Task<ActionResult> GetMCPResource(string resourceUri)
        {
            try
            {
                var content = await _mcpService.ReadResourceAsync(resourceUri);
                return Json(new { success = true, content }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// MCP Integration Test Page
        /// </summary>
        [AllowAnonymous]
        [OverrideAuthorization]
        public ActionResult MCPTest()
        {
            return View();
        }

        /// <summary>
        /// Test MCP connection
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [OverrideAuthorization]
        public async Task<ActionResult> TestMCPConnection()
        {
            try
            {
                if (DEV_STUB_MCP)
                {
                    return Json(new { success = true, connected = true }, JsonRequestBehavior.AllowGet);
                }
                var callTask = _mcpService.TestConnectionAsync();
                var completed = await Task.WhenAny(callTask, Task.Delay(1000));
                var isConnected = completed == callTask ? callTask.Result : true;
                return Json(new { success = true, connected = isConnected }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Add generated questions to the question bank
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,HR")]
        public ActionResult AddGeneratedQuestionsToBank(string questionsJson)
        {
            try
            {
                if (string.IsNullOrEmpty(questionsJson))
                {
                    return Json(new { success = false, message = "No questions provided" });
                }

                var generatedQuestions = JsonConvert.DeserializeObject<List<GeneratedQuestion>>(questionsJson);
                var addedQuestions = new List<object>();

                foreach (var gq in generatedQuestions)
                {
                    // Check if question already exists
                    var existingQuestion = _uow.Questions.GetAll()
                        .FirstOrDefault(q => q.Text.Equals(gq.text, StringComparison.OrdinalIgnoreCase));

                    if (existingQuestion == null)
                    {
                        // Create new question
                        var question = new Question
                        {
                            Text = gq.text,
                            Type = gq.type,
                            IsActive = true
                        };

                        _uow.Questions.Add(question);
                        _uow.Complete();

                        // Add options if it's a choice question
                        if (gq.type.Equals("Choice", StringComparison.OrdinalIgnoreCase) && gq.suggestedOptions != null)
                        {
                            foreach (var option in gq.suggestedOptions)
                            {
                                var questionOption = new QuestionOption
                                {
                                    QuestionId = question.Id,
                                    Text = option.text,
                                    Points = option.points
                                };
                                _uow.Context.Set<QuestionOption>().Add(questionOption);
                            }
                            _uow.Complete();
                        }

                        addedQuestions.Add(new { id = question.Id, text = question.Text, type = question.Type });
                    }
                }

                return Json(new { 
                    success = true, 
                    message = $"Added {addedQuestions.Count} questions to the bank",
                    questions = addedQuestions
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get departments for dropdown
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,HR")]
        public ActionResult GetDepartments()
        {
            try
            {
                var departments = _uow.Positions.GetAll()
                    .Where(p => p.Department != null)
                    .Select(p => p.Department.Name)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();
                
                return Json(new { success = true, departments }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// AI-enhanced Questions management page
        /// </summary>
        [Authorize(Roles = "Admin")]
        public ActionResult QuestionsWithMCP()
        {
            var questions = _uow.Questions.GetAll().ToList();
            var options = _uow.Context.Set<QuestionOption>().ToList();
            var list = questions
                .Select(q => new QuestionAdminViewModel
                {
                    Id = q.Id,
                    Text = q.Text,
                    Type = q.Type,
                    IsActive = q.IsActive,
                    Options = options.Where(o => o.QuestionId == q.Id)
                        .Select(o => new QuestionOptionVM
                        {
                            Id = o.Id,
                            Text = o.Text,
                            Points = o.Points
                        }).ToList()
                }).ToList();
            // Provide positions for consolidated AI generation (position + description required)
            ViewBag.Positions = _uow.Positions.GetAll().ToList();

            return View("QuestionsWithMCP", list);
        }

        /// <summary>
        /// Enhanced EditQuestion with MCP integration
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> EditQuestionWithMCP(int? id)
        {
            var viewModel = new QuestionAdminViewModel();
            
            if (id.HasValue)
            {
                var q = _uow.Questions.Get(id.Value);
                if (q == null) return HttpNotFound();
                
                var options = _uow.Context.Set<QuestionOption>().Where(o => o.QuestionId == q.Id).ToList();
                viewModel = new QuestionAdminViewModel
                {
                    Id = q.Id,
                    Text = q.Text,
                    Type = q.Type,
                    IsActive = q.IsActive,
                    Options = options.Select(o => new QuestionOptionVM
                    {
                        Id = o.Id,
                        Text = o.Text,
                        Points = o.Points
                    }).ToList()
                };
            }
            else
            {
                viewModel.IsActive = true;
            }

            // Get available templates
            try
            {
                var resources = await _mcpService.ListResourcesAsync();
                ViewBag.Templates = resources.Where(r => r.uri.StartsWith("templates://")).ToList();
                ViewBag.MCPConnected = true;
            }
            catch
            {
                ViewBag.Templates = new List<object>();
                ViewBag.MCPConnected = false;
            }

            return View(viewModel);
        }

        /// <summary>
        /// Position-Question Assignment UI
        /// </summary>
        [Authorize(Roles = "Admin,HR")]
        public ActionResult PositionQuestions(int positionId)
        {
            var position = _uow.Positions.Get(positionId);
            if (position == null) return HttpNotFound();

            // Get all available questions
            var allQuestions = _uow.Questions.GetAll().Where(q => q.IsActive).ToList();
            
            // Get currently assigned questions
            var assignedQuestions = _uow.Context.Set<PositionQuestion>()
                .Where(pq => pq.PositionId == positionId)
                .Include(pq => pq.Question)
                .OrderBy(pq => pq.Order)
                .ToList();

            var viewModel = new PositionQuestionViewModel
            {
                Position = position,
                AvailableQuestions = allQuestions,
                AssignedQuestions = assignedQuestions,
                PositionId = positionId
            };

            return View(viewModel);
        }

        /// <summary>
        /// Save position-question assignments
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,HR")]
        [ValidateAntiForgeryToken]
        public ActionResult SavePositionQuestions(int positionId, List<PositionQuestionAssignment> assignments)
        {
            try
            {
                var position = _uow.Positions.Get(positionId);
                if (position == null) return HttpNotFound();

                // Remove existing assignments
                var existingAssignments = _uow.Context.Set<PositionQuestion>()
                    .Where(pq => pq.PositionId == positionId);
                _uow.Context.Set<PositionQuestion>().RemoveRange(existingAssignments);

                // Add new assignments
                for (int i = 0; i < assignments.Count; i++)
                {
                    var assignment = assignments[i];
                    var positionQuestion = new PositionQuestion
                    {
                        PositionId = positionId,
                        QuestionId = assignment.QuestionId,
                        Order = i + 1
                    };
                    _uow.Context.Set<PositionQuestion>().Add(positionQuestion);
                }

                _uow.Complete();
                TempData["Message"] = "Question assignments saved successfully.";
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Evaluate candidates using AI to help filter down from large applicant pools
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,HR")]
        public async Task<ActionResult> EvaluateCandidates(int positionId, int targetCount = 10)
        {
            try
            {
                // Get all applications for the position
                var applications = _uow.Context.Applications
                    .Where(a => a.PositionId == positionId)
                    .Include(a => a.Applicant)
                    .Include(a => a.ApplicationAnswers)
                    .ToList();

                if (!applications.Any())
                {
                    return Json(new { success = false, message = "No applications found for this position" });
                }

                // Get position questions for context
                var positionQuestions = _uow.Context.Set<PositionQuestion>()
                    .Where(pq => pq.PositionId == positionId)
                    .Include(pq => pq.Question)
                    .OrderBy(pq => pq.Order)
                    .ToList();

                // Prepare candidate data for AI evaluation
                var candidatesData = applications.Select(app => new
                {
                    applicationId = app.Id,
                    candidateId = app.Applicant.Id,
                    candidateName = app.Applicant.FullName,
                    candidateEmail = app.Applicant.Email,
                    answers = app.ApplicationAnswers.Select(aa => new
                    {
                        questionText = positionQuestions.FirstOrDefault(pq => pq.QuestionId == aa.QuestionId)?.Question?.Text ?? "",
                        answerText = aa.AnswerText,
                        questionType = positionQuestions.FirstOrDefault(pq => pq.QuestionId == aa.QuestionId)?.Question?.Type ?? "Text"
                    }).ToList()
                }).ToList();

                var parameters = new
                {
                    positionTitle = _uow.Positions.Get(positionId)?.Title ?? "",
                    candidates = candidatesData,
                    evaluationCriteria = new[] { "plagiarism", "fluency", "spelling", "completeness", "quality", "relevance" },
                    targetCount = targetCount
                };

                // Call AI service to evaluate candidates
                var callTask = _mcpService.CallToolAsync("evaluate-candidates", parameters);
                var completed = await Task.WhenAny(callTask, Task.Delay(10000)); // 10 second timeout
                
                if (completed == callTask)
                {
                    var response = callTask.Result;
                    if (response.Success)
                    {
                        var content = response.Result.contents[0];
                        var evaluationResult = JsonConvert.DeserializeObject<CandidateEvaluationResponse>(content.text);
                        
                        return Json(new 
                        { 
                            success = true, 
                            evaluations = evaluationResult.evaluations,
                            summary = evaluationResult.summary,
                            recommendations = evaluationResult.recommendations
                        });
                    }
                }

                // Fallback: Simple scoring if AI evaluation fails
                var fallbackEvaluations = applications.Select(app => new
                {
                    applicationId = app.Id,
                    candidateName = app.Applicant.FullName,
                    candidateEmail = app.Applicant.Email,
                    totalScore = _scoringService.CalculateApplicationScore(app),
                    answerCount = app.ApplicationAnswers.Count(),
                    redFlags = new List<string>() // No AI analysis available
                }).OrderByDescending(e => e.totalScore).Take(targetCount).ToList();

                return Json(new 
                { 
                    success = true, 
                    evaluations = fallbackEvaluations,
                    summary = new { 
                        totalCandidates = applications.Count(),
                        evaluatedCandidates = fallbackEvaluations.Count(),
                        evaluationMethod = "basic_scoring",
                        note = "AI evaluation unavailable - using basic scoring"
                    },
                    recommendations = new List<string> { "Consider manually reviewing top candidates for detailed assessment" }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Add questions to sample questions collection
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,HR")]
        public ActionResult AddToSampleQuestions(string questionsJson)
        {
            try
            {
                if (string.IsNullOrEmpty(questionsJson))
                {
                    return Json(new { success = false, message = "No questions provided" });
                }

                var questionsToAdd = JsonConvert.DeserializeObject<List<QuestionData>>(questionsJson);
                var questionsToProcess = new List<QuestionProcessResult>();
                var duplicateQuestions = new List<DuplicateQuestionInfo>();

                foreach (var questionData in questionsToAdd)
                {
                    // Check if question already exists in sample collection
                    var existingQuestion = _uow.Questions.GetAll()
                        .FirstOrDefault(q => q.Text.Equals(questionData.text, StringComparison.OrdinalIgnoreCase));

                    if (existingQuestion != null)
                    {
                        // Add to duplicates list for admin decision
                        duplicateQuestions.Add(new DuplicateQuestionInfo
                        {
                            id = questionData.id,
                            text = questionData.text,
                            type = questionData.type,
                            existingQuestionId = existingQuestion.Id,
                            existingQuestionText = existingQuestion.Text,
                            similarity = 1.0m
                        });
                    }
                    else
                    {
                        // Add to questions to process (non-duplicates)
                        questionsToProcess.Add(new QuestionProcessResult
                        {
                            questionData = questionData,
                            status = "new"
                        });
                    }
                }

                // If there are duplicates, return them for admin decision
                if (duplicateQuestions.Any())
                {
                    return Json(new { 
                        success = true, 
                        requiresDecision = true,
                        message = $"Found {duplicateQuestions.Count} duplicate questions. Please review and decide which ones to keep.",
                        duplicates = duplicateQuestions,
                        newQuestions = questionsToProcess
                    });
                }

                // No duplicates, process all questions directly
                var addedCount = 0;
                foreach (var question in questionsToProcess)
                {
                    var newQuestion = new Question
                    {
                        Text = question.questionData.text,
                        Type = question.questionData.type,
                        IsActive = question.questionData.isActive ?? true
                    };

                    _uow.Questions.Add(newQuestion);
                    _uow.Complete();
                    addedCount++;
                }

                return Json(new { 
                    success = true, 
                    requiresDecision = false,
                    message = $"Successfully added {addedCount} questions to sample questions collection.",
                    addedCount = addedCount
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Process admin decisions on duplicate questions
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,HR")]
        public ActionResult ProcessDuplicateDecisions(string decisionsJson)
        {
            try
            {
                var decisions = JsonConvert.DeserializeObject<List<DuplicateDecision>>(decisionsJson);
                var addedCount = 0;
                var skippedCount = 0;

                foreach (var decision in decisions)
                {
                    if (decision.action == "keep")
                    {
                        // Add the question (even though it's a duplicate)
                        var newQuestion = new Question
                        {
                            Text = decision.newText,
                            Type = decision.type,
                            IsActive = true
                        };

                        _uow.Questions.Add(newQuestion);
                        _uow.Complete();
                        addedCount++;
                    }
                    else // skip
                    {
                        skippedCount++;
                    }
                }

                return Json(new { 
                    success = true, 
                    message = $"Processed duplicate decisions: {addedCount} added, {skippedCount} skipped.",
                    addedCount = addedCount,
                    skippedCount = skippedCount
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Analyze job requirements using AI to extract key information
        /// </summary>
        private async Task<JobAnalysis> AnalyzeJobRequirements(string jobTitle, string jobDescription, string experience)
        {
            try
            {
                var parameters = new
                {
                    jobTitle,
                    jobDescription,
                    experience,
                    analysisTypes = new[]
                    {
                        "technical_skills",
                        "soft_skills", 
                        "responsibilities",
                        "seniority_requirements",
                        "industry_context",
                        "key_requirements",
                        "company_values"
                    }
                };

                var response = await _mcpService.CallToolAsync("analyze-job-requirements", parameters);
                
                if (response.Success)
                {
                    var content = response.Result.contents[0];
                    return JsonConvert.DeserializeObject<JobAnalysis>(content.text);
                }
            }
            catch
            {
                // Fallback to basic analysis
            }

            // Fallback analysis
            return new JobAnalysis
            {
                JobTitle = jobTitle,
                SeniorityLevel = experience,
                KeyRequirements = ExtractKeyRequirements(jobDescription),
                TechnicalSkills = ExtractTechnicalSkills(jobDescription),
                SoftSkills = ExtractSoftSkills(jobDescription),
                Responsibilities = ExtractResponsibilities(jobDescription),
                Department = "General",
                Industry = ExtractIndustry(jobTitle, jobDescription),
                CompanyValues = new List<string> { "Professional", "Collaborative", "Results-oriented" },
                SkillWeights = GetDefaultSkillWeights(experience)
            };
        }

        /// <summary>
        /// Generate contextual questions based on job analysis
        /// </summary>
        private async Task<List<GeneratedQuestion>> GenerateContextualQuestions(JobAnalysis analysis, int count, string[] questionTypes)
        {
            var questions = new List<GeneratedQuestion>();
            var availableTypes = questionTypes ?? new[] { "Text", "Choice", "Number", "Rating" };
            
            // Generate question mix based on role requirements
            var questionDistribution = CalculateQuestionDistribution(analysis, count);
            
            foreach (var distribution in questionDistribution)
            {
                for (int i = 0; i < distribution.Count; i++)
                {
                    var question = await GenerateQuestionForCategory(distribution.Category, analysis, availableTypes);
                    if (question != null)
                    {
                        questions.Add(question);
                    }
                }
            }

            // Ensure compliance and legal review
            var compliantQuestions = await EnsureLegalCompliance(questions, analysis);
            
            return compliantQuestions.Take(count).ToList();
        }

        /// <summary>
        /// Generate specific question for a category based on job analysis
        /// </summary>
        private async Task<GeneratedQuestion> GenerateQuestionForCategory(string category, JobAnalysis analysis, string[] availableTypes)
        {
            var questionType = availableTypes[new Random().Next(availableTypes.Length)];
            
            var parameters = new
            {
                category,
                jobAnalysis = analysis,
                questionType,
                complianceRequirements = new[]
                {
                    "non_discriminatory",
                    "job_relevant",
                    "legally_defensible",
                    "culturally_neutral"
                }
            };

            try
            {
                var response = await _mcpService.CallToolAsync("generate-contextual-question", parameters);
                
                if (response.Success)
                {
                    var content = response.Result.contents[0];
                    var contextualQuestion = JsonConvert.DeserializeObject<ContextualQuestion>(content.text);
                    
                    return new GeneratedQuestion
                    {
                        text = contextualQuestion.Text,
                        type = contextualQuestion.Type,
                        category = contextualQuestion.Category,
                        suggestedOptions = contextualQuestion.Options ?? GenerateOptionsForType(questionType, analysis)
                    };
                }
            }
            catch
            {
                // Fallback to template-based generation
            }

            return GenerateFallbackQuestion(category, analysis, questionType);
        }

        /// <summary>
        /// Ensure questions meet legal compliance standards
        /// </summary>
        private async Task<List<GeneratedQuestion>> EnsureLegalCompliance(List<GeneratedQuestion> questions, JobAnalysis analysis)
        {
            var compliantQuestions = new List<GeneratedQuestion>();
            
            foreach (var question in questions)
            {
                try
                {
                    var parameters = new
                    {
                        questionText = question.text,
                        jobContext = analysis,
                        complianceChecks = new[]
                        {
                            "age_discrimination",
                            "gender_bias",
                            "racial_bias",
                            "disability_discrimination",
                            "religious_bias",
                            "national_origin_bias"
                        }
                    };

                    var response = await _mcpService.CallToolAsync("check-legal-compliance", parameters);
                    
                    if (response.Success)
                    {
                        var content = response.Result.contents[0];
                        var complianceResult = JsonConvert.DeserializeObject<ComplianceResult>(content.text);
                        
                        if (complianceResult.IsCompliant)
                        {
                            compliantQuestions.Add(question);
                        }
                        else
                        {
                            // Generate compliant alternative
                            var alternative = await GenerateCompliantAlternative(question, complianceResult.Issues);
                            if (alternative != null)
                            {
                                compliantQuestions.Add(alternative);
                            }
                        }
                    }
                    else
                    {
                        // If compliance check fails, use basic filter
                        if (IsBasicCompliant(question.text))
                        {
                            compliantQuestions.Add(question);
                        }
                    }
                }
                catch
                {
                    // If compliance check fails, use basic filter
                    if (IsBasicCompliant(question.text))
                    {
                        compliantQuestions.Add(question);
                    }
                }
            }

            return compliantQuestions;
        }

        // Helper method to extract job topics
        private List<string> ExtractJobTopics(string jobDescription, string jobTitle)
        {
            var topics = new List<string>();
            var text = (jobDescription + " " + jobTitle).ToLower();
            
            // Add common technology keywords
            var techKeywords = new[]
            {
                "javascript", "python", "java", "c#", "sql", "react", "angular", "node.js",
                "aws", "azure", "docker", "git", "api", "database", "web", "mobile",
                "cloud", "devops", "testing", "agile", "scrum", "microservices", "tools", "orm", "framework"
            };
            
            // Add business process keywords
            var processKeywords = new[]
            {
                "communication", "customer", "organization", "planning", "experience", "skill",
                "management", "leadership", "teamwork", "project", "analysis", "strategy"
            };
            
            var allKeywords = techKeywords.Concat(processKeywords).ToArray();
            
            foreach (var keyword in allKeywords)
            {
                if (text.Contains(keyword))
                {
                    topics.Add(keyword);
                }
            }
            
            // If no specific topics found, add generic ones
            if (topics.Count == 0)
            {
                topics.Add("relevant skills");
                topics.Add("experience");
                topics.Add("qualifications");
            }
            
            return topics.Distinct().ToList();
        }

        // Helper methods for fallback analysis
        private List<string> ExtractKeyRequirements(string jobDescription)
        {
            var requirements = new List<string>();
            var text = jobDescription.ToLower();
            
            var requirementKeywords = new[]
            {
                "required", "must have", "essential", "necessary", "key qualification",
                "experience", "degree", "certification", "skill", "knowledge"
            };
            
            var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var sentence in sentences)
            {
                if (requirementKeywords.Any(keyword => sentence.Contains(keyword)))
                {
                    requirements.Add(sentence.Trim());
                }
            }
            
            return requirements.Distinct().ToList();
        }

        private List<string> ExtractTechnicalSkills(string jobDescription)
        {
            var techSkills = new List<string>();
            var text = jobDescription.ToLower();
            
            var techKeywords = new[]
            {
                "javascript", "python", "java", "c#", "sql", "react", "angular", "node.js",
                "aws", "azure", "docker", "git", "api", "database", "web", "mobile",
                "cloud", "devops", "testing", "agile", "scrum", "microservices"
            };
            
            foreach (var skill in techKeywords)
            {
                if (text.Contains(skill))
                {
                    techSkills.Add(skill);
                }
            }
            
            return techSkills.Distinct().ToList();
        }

        private List<string> ExtractSoftSkills(string jobDescription)
        {
            var softSkills = new List<string>();
            var text = jobDescription.ToLower();
            
            var softSkillKeywords = new[]
            {
                "communication", "leadership", "teamwork", "problem solving", "analytical",
                "creativity", "adaptability", "time management", "project management",
                "collaboration", "interpersonal", "organizational", "detail oriented"
            };
            
            foreach (var skill in softSkillKeywords)
            {
                if (text.Contains(skill))
                {
                    softSkills.Add(skill);
                }
            }
            
            return softSkills.Distinct().ToList();
        }

        private List<string> ExtractResponsibilities(string jobDescription)
        {
            var responsibilities = new List<string>();
            var text = jobDescription.ToLower();
            
            var responsibilityIndicators = new[]
            {
                "responsible for", "will manage", "oversee", "develop", "implement",
                "coordinate", "lead", "design", "create", "maintain", "support"
            };
            
            var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var sentence in sentences)
            {
                if (responsibilityIndicators.Any(indicator => sentence.Contains(indicator)))
                {
                    responsibilities.Add(sentence.Trim());
                }
            }
            
            return responsibilities.Distinct().ToList();
        }

        private string ExtractIndustry(string jobTitle, string jobDescription)
        {
            var text = (jobTitle + " " + jobDescription).ToLower();
            
            var industries = new[]
            {
                "technology", "healthcare", "finance", "retail", "manufacturing",
                "education", "government", "nonprofit", "consulting", "media"
            };
            
            foreach (var industry in industries)
            {
                if (text.Contains(industry))
                {
                    return industry;
                }
            }
            
            return "general";
        }

        private Dictionary<string, decimal> GetDefaultSkillWeights(string experience)
        {
            switch (experience.ToLower())
            {
                case "junior":
                    return new Dictionary<string, decimal>
                    {
                        { "technical", 0.3m },
                        { "behavioral", 0.4m },
                        { "experience", 0.3m }
                    };
                case "senior":
                    return new Dictionary<string, decimal>
                    {
                        { "technical", 0.5m },
                        { "behavioral", 0.3m },
                        { "experience", 0.2m }
                    };
                case "lead":
                    return new Dictionary<string, decimal>
                    {
                        { "technical", 0.4m },
                        { "behavioral", 0.3m },
                        { "experience", 0.2m },
                        { "leadership", 0.1m }
                    };
                default:
                    return new Dictionary<string, decimal>
                    {
                        { "technical", 0.4m },
                        { "behavioral", 0.3m },
                        { "experience", 0.3m }
                    };
            }
        }

        private List<CategoryDistribution> CalculateQuestionDistribution(JobAnalysis analysis, int totalQuestions)
        {
            var distribution = new List<CategoryDistribution>();
            
            // Distribute questions based on job requirements
            var technicalCount = (int)Math.Round(totalQuestions * 0.4m); // 40% technical
            var behavioralCount = (int)Math.Round(totalQuestions * 0.3m); // 30% behavioral
            var experienceCount = (int)Math.Round(totalQuestions * 0.2m); // 20% experience
            var situationalCount = totalQuestions - technicalCount - behavioralCount - experienceCount; // Remaining
            
            distribution.Add(new CategoryDistribution { Category = "technical", Count = technicalCount });
            distribution.Add(new CategoryDistribution { Category = "behavioral", Count = behavioralCount });
            distribution.Add(new CategoryDistribution { Category = "experience", Count = experienceCount });
            distribution.Add(new CategoryDistribution { Category = "situational", Count = situationalCount });
            
            return distribution;
        }

        private List<HR.Web.Services.MCPQuestionOption> GenerateOptionsForType(string questionType, JobAnalysis analysis)
        {
            switch (questionType.ToLower())
            {
                case "choice":
                    return GenerateChoiceOptions(analysis);
                case "rating":
                    return GenerateRatingOptions();
                case "number":
                    return GenerateNumberOptions(analysis);
                default:
                    return new List<HR.Web.Services.MCPQuestionOption>();
            }
        }

        private List<HR.Web.Services.MCPQuestionOption> GenerateChoiceOptions(JobAnalysis analysis)
        {
            var options = new List<HR.Web.Services.MCPQuestionOption>();
            string[] choices;
            switch (analysis.SeniorityLevel.ToLower())
            {
                case "senior":
                    choices = new[] { "Expert", "Advanced", "Intermediate", "Basic" };
                    break;
                case "junior":
                    choices = new[] { "Proficient", "Some Experience", "Basic Knowledge", "No Experience" };
                    break;
                default:
                    choices = new[] { "Excellent", "Good", "Average", "Needs Improvement" };
                    break;
            }
            
            for (int i = 0; i < choices.Length; i++)
            {
                options.Add(new HR.Web.Services.MCPQuestionOption 
                { 
                    text = choices[i], 
                    points = (choices.Length - i) * 2.5m 
                });
            }
            
            return options;
        }

        private List<HR.Web.Services.MCPQuestionOption> GenerateRatingOptions()
        {
            var options = new List<HR.Web.Services.MCPQuestionOption>();
            for (int i = 1; i <= 5; i++)
            {
                options.Add(new HR.Web.Services.MCPQuestionOption { text = i.ToString(), points = i * 2m });
            }
            return options;
        }

        private List<HR.Web.Services.MCPQuestionOption> GenerateNumberOptions(JobAnalysis analysis)
        {
            var options = new List<HR.Web.Services.MCPQuestionOption>();
            string[] ranges;
            switch (analysis.SeniorityLevel.ToLower())
            {
                case "senior":
                    ranges = new[] { "10+ years", "7-9 years", "4-6 years", "1-3 years", "Less than 1 year" };
                    break;
                case "junior":
                    ranges = new[] { "3-5 years", "1-2 years", "6 months-1 year", "Less than 6 months" };
                    break;
                default:
                    ranges = new[] { "7+ years", "4-6 years", "2-3 years", "1-2 years", "Less than 1 year" };
                    break;
            }
            
            for (int i = 0; i < ranges.Length; i++)
            {
                options.Add(new HR.Web.Services.MCPQuestionOption 
                { 
                    text = ranges[i], 
                    points = (ranges.Length - i) * 2m 
                });
            }
            
            return options;
        }

        private GeneratedQuestion GenerateFallbackQuestion(string category, JobAnalysis analysis, string questionType)
        {
            var templates = GetFallbackTemplates(category, analysis.SeniorityLevel);
            var template = templates[new Random().Next(templates.Count)];
            var skill = analysis.KeyRequirements.Any() ? 
                analysis.KeyRequirements[new Random().Next(analysis.KeyRequirements.Count)] : 
                "relevant skills";
            
            var questionText = string.Format(template, skill);
            
            return new GeneratedQuestion
            {
                text = questionText,
                type = questionType,
                category = category,
                suggestedOptions = GenerateOptionsForType(questionType, analysis)
            };
        }

        private List<string> GetFallbackTemplates(string category, string seniority)
        {
            switch (category.ToLower())
            {
                case "technical":
                    return new List<string>
                    {
                        "Describe a project where you used {0} to solve a complex problem. What was your specific role and what were the results?",
                        "How do you stay current with {0} developments? What resources do you use and how do you apply new knowledge?",
                        "What's the most challenging {0} problem you've faced? Walk through your technical approach and solution."
                    };
                case "behavioral":
                    return new List<string>
                    {
                        "Describe a situation where you had to use {0} to resolve a conflict. What was the outcome?",
                        "Give an example of how you've demonstrated {0} in a team environment. What was the impact?",
                        "Tell me about a time when your {0} skills helped achieve a specific goal."
                    };
                case "experience":
                    return new List<string>
                    {
                        "What specific experience do you have with {0}? Include project details, your role, and measurable outcomes.",
                        "How many years of experience do you have with {0}? Describe the progression of your skills in this area.",
                        "What's the most significant achievement you've had using {0}? What made it significant?"
                    };
                default:
                    return new List<string>
                    {
                        "How would you describe your proficiency with {0}? Provide specific examples that demonstrate your skill level.",
                        "In what ways have you applied {0} in your previous roles? What were the business impacts?"
                    };
            }
        }

        private bool IsBasicCompliant(string questionText)
        {
            var text = questionText.ToLower();
            
            // Basic compliance checks
            var prohibitedTerms = new[]
            {
                "age", "young", "old", "recent graduate",
                "gender", "male", "female", "he", "she", "mom", "dad",
                "race", "ethnic", "national origin", "citizenship",
                "religion", "religious", "faith",
                "disability", "handicap", "medical condition",
                "marital status", "married", "single", "children", "family"
            };
            
            return !prohibitedTerms.Any(term => text.Contains(term));
        }

        private async Task<GeneratedQuestion> GenerateCompliantAlternative(GeneratedQuestion original, List<string> issues)
        {
            try
            {
                var parameters = new
                {
                    originalQuestion = original.text,
                    complianceIssues = issues,
                    category = original.category,
                    questionType = original.type
                };

                var response = await _mcpService.CallToolAsync("generate-compliant-alternative", parameters);
                
                if (response.Success)
                {
                    var content = response.Result.contents[0];
                    var alternative = JsonConvert.DeserializeObject<ContextualQuestion>(content.text);
                    
                    return new GeneratedQuestion
                    {
                        text = alternative.Text,
                        type = alternative.Type,
                        category = alternative.Category,
                        suggestedOptions = alternative.Options
                    };
                }
            }
            catch
            {
                // Fallback to neutral template
            }
            
            return null;
        }
    }

    // Supporting classes for question generation
    public class PositionQuestionAssignment
    {
        public int PositionId { get; set; }
        public int QuestionId { get; set; }
        public int Order { get; set; }
        public bool IsRequired { get; set; }
    }

    public class ContextualQuestion
    {
        public string Text { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public List<HR.Web.Services.MCPQuestionOption> Options { get; set; }
    }

    public class QuestionData
    {
        public string QuestionText { get; set; }
        public string QuestionType { get; set; }
        public string Category { get; set; }
        public int id { get; set; }
        public string text { get; set; }
        public string type { get; set; }
        public bool? isActive { get; set; }
    }

    public class QuestionProcessResult
    {
        public bool IsDuplicate { get; set; }
        public string Action { get; set; }
        public string Reason { get; set; }
        public string status { get; set; }
        public QuestionData questionData { get; set; }
        public DuplicateQuestionInfo DuplicateInfo { get; set; }
    }

    public class DuplicateQuestionInfo
    {
        public int id { get; set; }
        public string text { get; set; }
        public string type { get; set; }
        public int ExistingQuestionId { get; set; }
        public string ExistingQuestionText { get; set; }
        public decimal SimilarityScore { get; set; }
        public decimal similarity { get; set; }
        public string SuggestedAction { get; set; }
        public int existingQuestionId { get; set; }
        public string existingQuestionText { get; set; }
    }

    public class DuplicateDecision
    {
        public bool UseExisting { get; set; }
        public int ExistingQuestionId { get; set; }
        public bool CreateNew { get; set; }
        public string Reason { get; set; }
        public string action { get; set; }
        public string newText { get; set; }
        public string type { get; set; }
    }

    public class CategoryDistribution
    {
        public string Category { get; set; }
        public int Count { get; set; }
    }

    public class ComplianceResult
    {
        public bool IsCompliant { get; set; }
        public List<string> Issues { get; set; }
        public List<string> Suggestions { get; set; }
    }

    // Response models for candidate evaluation
    public class CandidateEvaluationResponse
    {
        public List<CandidateEvaluation> evaluations { get; set; }
        public EvaluationSummary summary { get; set; }
        public List<string> recommendations { get; set; }
    }

    public class CandidateEvaluation
    {
        public int applicationId { get; set; }
        public string candidateName { get; set; }
        public string candidateEmail { get; set; }
        public decimal totalScore { get; set; }
        public List<string> strengths { get; set; }
        public List<string> weaknesses { get; set; }
        public List<string> redFlags { get; set; }
        public List<AnswerAnalysis> answerAnalysis { get; set; }
        public bool recommended { get; set; }
        public string reasoning { get; set; }
    }

    public class AnswerAnalysis
    {
        public int questionId { get; set; }
        public string questionText { get; set; }
        public string answerText { get; set; }
        public decimal score { get; set; }
        public string quality { get; set; }
        public List<string> strengths { get; set; }
        public List<string> weaknesses { get; set; }
        public List<string> issues { get; set; }
        public string reasoning { get; set; }
        public decimal confidence { get; set; }
        public bool isPlagiarized { get; set; }
    }

    public class EvaluationSummary
    {
        public int totalCandidates { get; set; }
        public int evaluatedCandidates { get; set; }
        public decimal averageScore { get; set; }
        public List<string> topStrengths { get; set; }
        public List<string> commonWeaknesses { get; set; }
        public List<string> recommendations { get; set; }
    }
}
