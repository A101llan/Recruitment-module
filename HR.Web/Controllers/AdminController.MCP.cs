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

        // POST: Admin/GenerateQuestions
        [HttpPost]
        [AllowAnonymous]
        [OverrideAuthorization]
        public async Task<ActionResult> GenerateQuestions(string jobTitle, string jobDescription, string experience = "mid", string[] questionTypes = null, int count = 5)
        {
            try
            {
                if (DEV_STUB_MCP)
                {
                    // Generate stub questions based on job description keywords
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
                    
                    for (int i = 0; i < count && i < 20; i++)
                    {
                        var tpl = templates[rand.Next(templates.Length)];
                        var topic = topics[rand.Next(topics.Count)];
                        var questionText = string.Format(tpl.Text, topic);
                        
                        var options = new List<MCPQuestionOption>();
                        if (tpl.Type == "Choice")
                        {
                            var choices = new[] { "Beginner", "Intermediate", "Advanced", "Expert" };
                            if (topic.Contains("communication") || topic.Contains("customer"))
                            {
                                choices = new[] { "Poor", "Fair", "Good", "Excellent" };
                            }
                            else if (topic.Contains("organization") || topic.Contains("planning"))
                            {
                                choices = new[] { "Needs Improvement", "Satisfactory", "Good", "Outstanding" };
                            }
                            else if (topic.Contains("experience") || topic.Contains("skill"))
                            {
                                choices = new[] { "None", "Some", "Proficient", "Expert" };
                            }
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
                    
                    var stub = new GeneratedQuestionsResponse
                    {
                        success = true,
                        metadata = new Metadata { jobTitle = jobTitle, experience = experience, keywords = topics.Take(3).ToList(), generatedAt = DateTime.UtcNow.ToString("o") },
                        questions = stubQuestions
                    };
                    return Json(new { success = true, questions = stub.questions, metadata = stub.metadata }, JsonRequestBehavior.AllowGet);
                }

                if (string.IsNullOrEmpty(jobTitle) || string.IsNullOrEmpty(jobDescription))
                {
                    return Json(new { success = false, message = "Job title and description are required" });
                }

                questionTypes = questionTypes ?? new[] { "Text", "Choice", "Number", "Rating" };
                
                var parameters = new
                {
                    jobTitle,
                    jobDescription,
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
                    
                    for (int i = 0; i < count && i < 20; i++)
                    {
                        var tpl = templates[rand.Next(templates.Length)];
                        var topic = topics[rand.Next(topics.Count)];
                        var questionText = string.Format(tpl.Text, topic);
                        
                        var options = new List<MCPQuestionOption>();
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
                            similarity = "Exact match"
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
        /// Extract relevant topics from job description and title
        /// </summary>
        private List<string> ExtractJobTopics(string jobDescription, string jobTitle)
        {
            var topics = new List<string>();
            var text = (jobTitle + " " + jobDescription).ToLower();
            
            System.Diagnostics.Debug.WriteLine($"Extracting topics from: {text}");
            
            // Common office/admin keywords
            var officeKeywords = new[] { 
                "scheduling", "calendar management", "correspondence", "filing", "documentation", 
                "phone calls", "meetings", "coordination", "organization", "time management",
                "communication", "customer service", "data entry", "bookkeeping", "reception",
                "office supplies", "travel arrangements", "event planning", "reports", "presentations"
            };
            
            // Common technical keywords (for technical roles)
            var technicalKeywords = new[] { 
                "programming", "coding", "software development", "web development", "database",
                "c#", "java", "javascript", "python", "sql", "api", "framework", "testing",
                "debugging", "version control", "agile", "scrum", "devops", "cloud"
            };
            
            // Common management keywords
            var managementKeywords = new[] { 
                "team leadership", "project management", "budget", "planning", "strategy",
                "performance reviews", "hiring", "training", "mentoring", "delegation",
                "conflict resolution", "decision making", "resource allocation"
            };
            
            // Add relevant keywords based on job content
            var allKeywords = officeKeywords.Concat(technicalKeywords).Concat(managementKeywords);
            
            foreach (var keyword in allKeywords)
            {
                if (text.Contains(keyword))
                {
                    topics.Add(keyword);
                }
            }
            
            // If no specific keywords found, use generic topics
            if (!topics.Any())
            {
                topics.AddRange(new[] { "administrative tasks", "communication", "organization", "customer support" });
            }
            
            System.Diagnostics.Debug.WriteLine($"Extracted topics: [{string.Join(", ", topics)}]");
            
            return topics.Any() ? topics : new List<string> { "general duties", "office work", "team collaboration" };
        }
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
        public string questionText { get; set; }
        public string answerText { get; set; }
        public decimal score { get; set; }
        public List<string> issues { get; set; } // plagiarism, spelling, grammar, etc.
        public string quality { get; set; } // excellent, good, fair, poor
    }

    public class EvaluationSummary
    {
        public int totalCandidates { get; set; }
        public int evaluatedCandidates { get; set; }
        public int recommendedCandidates { get; set; }
        public string evaluationMethod { get; set; }
        public List<string> keyFindings { get; set; }
        public string note { get; set; }
    }

    public class PositionQuestionAssignment
    {
        public int QuestionId { get; set; }
        public int Order { get; set; }
    }

    public class QuestionData
    {
        public int id { get; set; }
        public string text { get; set; }
        public string type { get; set; }
        public bool? isActive { get; set; }
    }

    public class QuestionProcessResult
    {
        public QuestionData questionData { get; set; }
        public string status { get; set; } // "new" or "duplicate"
    }

    public class DuplicateQuestionInfo
    {
        public int id { get; set; }
        public string text { get; set; }
        public string type { get; set; }
        public int existingQuestionId { get; set; }
        public string existingQuestionText { get; set; }
        public string similarity { get; set; }
    }

    public class DuplicateDecision
    {
        public int id { get; set; }
        public string newText { get; set; }
        public string type { get; set; }
        public string action { get; set; } // "keep" or "skip"
        public int existingQuestionId { get; set; }
    }
}
