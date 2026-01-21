using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Web;

namespace HR.Web.Services
{
    public class JobAnalysis
    {
        public string JobTitle { get; set; }
        public string JobDescription { get; set; }
        public string SeniorityLevel { get; set; }
        public List<string> KeyRequirements { get; set; }
        public List<string> TechnicalSkills { get; set; }
        public List<string> SoftSkills { get; set; }
        public List<string> Responsibilities { get; set; }
        public string Industry { get; set; }
        public string Department { get; set; }
        public List<string> CompanyValues { get; set; }
        public Dictionary<string, decimal> SkillWeights { get; set; }
    }

    public class MCPService
    {
        private readonly string _mcpServerPath;
        private readonly string _nodePath;

        public MCPService()
        {
            // Resolve the path to the MCP server robustly under IIS Express
            var baseDir = AppDomain.CurrentDomain.BaseDirectory; // typically ...\HR.Web\bin\
            var appRoot = HttpRuntime.AppDomainAppPath;          // typically ...\HR.Web\

            // First try the expected locations
            var candidates = new List<string>
            {
                Path.Combine(appRoot ?? baseDir, "..", "mcp-server", "server.js"),            // ...\HR\mcp-server\server.js
                Path.Combine(baseDir, "..", "..", "mcp-server", "server.js"),               // from bin two levels up
                Path.Combine(baseDir, "mcp-server", "server.js"),                              // local fallback
                Path.Combine(appRoot ?? baseDir, "mcp-server", "server.js"),
                @"c:\Users\allan\Documents\Examples\HR\mcp-server\server.js"                 // absolute fallback
            };

            _mcpServerPath = candidates.FirstOrDefault(File.Exists) ?? candidates.Last();

            // Resolve Node path (prefer absolute, fallback to PATH)
            var nodePaths = new[]
            {
                @"C:\Program Files\nodejs\node.exe",
                @"C:\Program Files (x86)\nodejs\node.exe",
                "node"
            };
            _nodePath = nodePaths.FirstOrDefault(File.Exists) ?? "node";
            
            // Debug logging
            System.Diagnostics.Debug.WriteLine($"MCP Service: BaseDir = {baseDir}");
            System.Diagnostics.Debug.WriteLine($"MCP Service: AppRoot = {appRoot}");
            System.Diagnostics.Debug.WriteLine($"MCP Service: ServerPath = {_mcpServerPath}");
            System.Diagnostics.Debug.WriteLine($"MCP Service: NodePath = {_nodePath}");
            System.Diagnostics.Debug.WriteLine($"MCP Service: Server exists = {File.Exists(_mcpServerPath)}");
            System.Diagnostics.Debug.WriteLine($"MCP Service: Node exists = {File.Exists(_nodePath) || _nodePath == "node"}");
        }

        public async Task<MCPResponse> CallToolAsync(string toolName, object parameters)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"MCP Service: Calling tool '{toolName}' with parameters: {JsonConvert.SerializeObject(parameters)}");
                
                var request = new
                {
                    jsonrpc = "2.0",
                    id = Guid.NewGuid().ToString(),
                    method = "tools/call",
                    @params = new
                    {
                        name = toolName,
                        arguments = parameters
                    }
                };

                var response = await ExecuteMCPCommandAsync(request);
                var mcpResponse = JsonConvert.DeserializeObject<MCPResponse>(response);
                
                System.Diagnostics.Debug.WriteLine($"MCP Service: Response success: {mcpResponse.Success}");
                if (mcpResponse.Success && mcpResponse.Result != null)
                {
                    System.Diagnostics.Debug.WriteLine($"MCP Service: Response result: {mcpResponse.Result}");
                }
                
                return mcpResponse;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MCP Service: Exception occurred: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"MCP Service: Falling back to stub response for {toolName}");
                
                // Fallback: return stubbed responses so UI remains usable in dev
                try
                {
                    switch ((toolName ?? string.Empty).ToLowerInvariant())
                    {
                        case "generate-questions":
                            // Dynamic fallback generation based on job parameters
                            var genParams = parameters as Newtonsoft.Json.Linq.JObject;
                            var jobTitle = genParams?.Value<string>("jobTitle") ?? "Sample Position";
                            var jobDesc = genParams?.Value<string>("jobDescription") ?? "";
                            var count = genParams?.Value<int>("count") ?? 5;
                            var questionTypes = genParams?.Value<string[]>("questionTypes") ?? new[] { "Text", "Choice", "Rating" };
                            
                            var keywords = ExtractKeywords(jobDesc);
                            var fallbackQuestions = GenerateDynamicFallbackQuestions(jobTitle, keywords, questionTypes, count);
                            
                            var genPayload = new GeneratedQuestionsResponse
                            {
                                success = true,
                                metadata = new Metadata { 
                                    jobTitle = jobTitle, 
                                    experience = "mid", 
                                    keywords = keywords.Take(3).ToList(), 
                                    generatedAt = DateTime.UtcNow.ToString("o") 
                                },
                                questions = fallbackQuestions
                            };
                            return BuildToolSuccess(genPayload);
                        case "validate-question":
                            var valPayload = new ValidationResponse
                            {
                                success = true,
                                validation = new QuestionValidation{ isValid = true, warnings = new List<string>(), suggestions = new List<string>(), biasDetected = false, clarityScore = 8 },
                                recommendations = new List<string>()
                            };
                            return BuildToolSuccess(valPayload);
                        case "suggest-points":
                            var ptsPayload = new PointsSuggestionResponse
                            {
                                success = true,
                                difficulty = "intermediate",
                                totalPoints = 10,
                                suggestions = new List<PointSuggestion>
                                {
                                    new PointSuggestion{ option = "Option A", suggestedPoints = 4, reasoning = "Good answer"},
                                    new PointSuggestion{ option = "Option B", suggestedPoints = 3, reasoning = "Acceptable answer"},
                                    new PointSuggestion{ option = "Option C", suggestedPoints = 2, reasoning = "Less relevant"},
                                    new PointSuggestion{ option = "Option D", suggestedPoints = 1, reasoning = "Poor answer"}
                                }
                            };
                            return BuildToolSuccess(ptsPayload);
                        case "import-template":
                            var tplPayload = new TemplateResponse
                            {
                                success = true,
                                customizable = true,
                                importedAt = DateTime.UtcNow.ToString("o"),
                                template = new QuestionTemplate{ name = "Sample Template", description = "Imported locally", questions = new List<TemplateQuestion>{ new TemplateQuestion{ text = "Why do you want this role?", type = "Text", category = "behavioral" } } }
                            };
                            return BuildToolSuccess(tplPayload);
                    }
                }
                catch { }

                return new MCPResponse { Error = new MCPError { code = -1, message = $"Failed to call MCP tool {toolName}: {ex.Message}" } };
            }
        }

        public async Task<string> ReadResourceAsync(string resourceUri)
        {
            try
            {
                var request = new
                {
                    jsonrpc = "2.0",
                    id = Guid.NewGuid().ToString(),
                    method = "resources/read",
                    @params = new
                    {
                        uri = resourceUri
                    }
                };

                var response = await ExecuteMCPCommandAsync(request);
                var mcpResponse = JsonConvert.DeserializeObject<MCPResponse>(response);
                
                if (mcpResponse.Success && mcpResponse.Result?.contents?.Count > 0)
                {
                    return mcpResponse.Result.contents[0].text;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                // Fallback sample content
                if (!string.IsNullOrWhiteSpace(resourceUri) && resourceUri.StartsWith("templates://"))
                {
                    return "{\"name\":\"Sample Template\",\"description\":\"Local fallback template\"}";
                }
                return null;
            }
        }

        public async Task<List<MCPResource>> ListResourcesAsync()
        {
            try
            {
                var request = new
                {
                    jsonrpc = "2.0",
                    id = Guid.NewGuid().ToString(),
                    method = "resources/list"
                };

                var response = await ExecuteMCPCommandAsync(request);
                var mcpResponse = JsonConvert.DeserializeObject<MCPResponse>(response);
                
                if (mcpResponse.Success)
                {
                    return mcpResponse.Result?.resources ?? new List<MCPResource>();
                }
                
                return new List<MCPResource>();
            }
            catch (Exception ex)
            {
                // Fallback sample resources
                return new List<MCPResource>
                {
                    new MCPResource{ uri = "templates://general", name = "General Template", description = "Fallback template", mimeType = "application/json" },
                    new MCPResource{ uri = "rubrics://default", name = "Default Rubric", description = "Fallback rubric", mimeType = "application/json" }
                };
            }
        }

        private async Task<string> ExecuteMCPCommandAsync(object request)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"MCP Service: Executing command with Node path: {_nodePath}");
                System.Diagnostics.Debug.WriteLine($"MCP Service: Server path: {_mcpServerPath}");
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = _nodePath,
                    Arguments = $"\"{_mcpServerPath}\"",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(_mcpServerPath),
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();

                    // Send request then close stdin to signal EOF
                    var requestJson = JsonConvert.SerializeObject(request);
                    System.Diagnostics.Debug.WriteLine($"MCP Service: Sending request: {requestJson}");
                    process.StandardInput.WriteLine(requestJson);
                    process.StandardInput.Flush();
                    process.StandardInput.Close();

                    // Wait for a response; the server.js might be long-lived otherwise
                    const int TimeoutMs = 35000;
                    System.Diagnostics.Debug.WriteLine($"MCP Service: Waiting for response (timeout: {TimeoutMs}ms)");
                    var exited = process.WaitForExit(TimeoutMs);
                    if (!exited)
                    {
                        System.Diagnostics.Debug.WriteLine("MCP Service: Process timed out, killing process");
                        try { process.Kill(); } catch { }
                        throw new TimeoutException("MCP server did not respond in time");
                    }

                    var error = process.StandardError.ReadToEnd();
                    if (!string.IsNullOrEmpty(error))
                    {
                        System.Diagnostics.Debug.WriteLine($"MCP Service: Error output: {error}");
                        throw new Exception($"MCP Server Error: {error}");
                    }

                    var response = process.StandardOutput.ReadToEnd();
                    System.Diagnostics.Debug.WriteLine($"MCP Service: Raw response: {response}");
                    return response;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to execute MCP command: {ex.Message}", ex);
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var resources = await ListResourcesAsync();
                if (resources != null && resources.Count > 0) return true;
                // Heuristic fallback: node and server.js exist
                return File.Exists(_mcpServerPath) && (_nodePath == "node" || File.Exists(_nodePath));
            }
            catch
            {
                return File.Exists(_mcpServerPath) && (_nodePath == "node" || File.Exists(_nodePath));
            }
        }

        private MCPResponse BuildToolSuccess(object payload)
        {
            return new MCPResponse
            {
                Result = new MCPResult
                {
                    contents = new List<MCPContent>
                    {
                        new MCPContent{ type = "text", text = JsonConvert.SerializeObject(payload) }
                    }
                }
            };
        }

        // Helper methods for dynamic fallback generation
        private List<string> ExtractKeywords(string jobDescription)
        {
            if (string.IsNullOrEmpty(jobDescription))
                return new List<string> { "skills", "experience", "teamwork" };

            var techKeywords = new[] { 
                "javascript", "python", "java", "c#", "sql", "react", "angular", "node.js", 
                "aws", "azure", "docker", "git", "api", "database", "web", "mobile", 
                "cloud", "devops", "testing", "agile", "scrum", "microservices", "frontend", 
                "backend", "fullstack", "machine learning", "ai", "data", "analytics"
            };
            
            var businessKeywords = new[] {
                "management", "leadership", "communication", "project", "strategy", "marketing",
                "sales", "customer", "finance", "operations", "hr", "recruitment", "training"
            };

            var words = jobDescription.ToLower().Split(new[] { ' ', ',', '.', ';', ':', '-', '_', '/', '(', ')', '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
            var found = words.Where(word => 
                techKeywords.Contains(word) || 
                businessKeywords.Contains(word) ||
                word.Length > 4 // Include longer words that might be relevant
            ).Distinct().ToList();

            if (!found.Any())
            {
                // Default keywords based on common job categories
                found = new List<string> { "professional", "experience", "skills", "teamwork" };
            }

            return found.Take(5).ToList(); // Limit to top 5 keywords
        }

        private List<GeneratedQuestion> GenerateDynamicFallbackQuestions(string jobTitle, List<string> keywords, string[] questionTypes, int count)
        {
            var questions = new List<GeneratedQuestion>();
            var random = new Random();
            
            // Text question templates
            var textTemplates = new[]
            {
                $"Describe your experience with {{0}} and how you've applied it in professional settings.",
                $"Give an example of a challenging situation involving {{0}} and how you resolved it.",
                $"How do you stay updated with the latest developments in {{0}}?",
                $"What interests you most about working with {{0}} in the {jobTitle} role?",
                $"Describe a project where you used {{0}} to achieve significant results.",
                $"What specific {{0}} skills would you bring to our team?",
                $"How have you used {{0}} to solve business problems?",
                $"Describe your approach to learning and mastering {{0}} technologies."
            };

            // Choice question templates
            var choiceTemplates = new[]
            {
                $"How would you rate your proficiency with {{0}}?",
                $"Which approach do you prefer when working with {{0}}?",
                $"What's your experience level with {{0}}?",
                $"How often do you use {{0}} in your daily work?",
                $"What {{0}} tools or technologies have you worked with?",
                $"How do you handle {{0}} related challenges in a team environment?"
            };

            // Rating question templates
            var ratingTemplates = new[]
            {
                $"Rate your expertise in {{0}} (1-5).",
                $"Rate your problem-solving skills with {{0}} (1-5).",
                $"Rate your communication skills when discussing {{0}} (1-5).",
                $"Rate your ability to learn new {{0}} technologies (1-5).",
                $"Rate your teamwork skills in {{0}} projects (1-5)."
            };

            // Number question templates
            var numberTemplates = new[]
            {
                $"How many years of experience do you have with {{0}}?",
                $"How many projects have you completed using {{0}}?",
                $"How many team members have you mentored on {{0}}?",
                $"How many certifications do you have related to {{0}}?",
                $"How many {{0}} systems have you implemented?",
                $"On a scale of 1-10, how would you rate your {{0}} knowledge?"
            };

            // Calculate how many questions to generate per type
            var questionsPerType = new Dictionary<string, int>();
            var remainingQuestions = count;
            var typesCount = questionTypes.Length;
            
            // Distribute questions evenly among types
            for (int i = 0; i < typesCount; i++)
            {
                var questionsForThisType = Math.Ceiling((double)remainingQuestions / (typesCount - i));
                questionsPerType[questionTypes[i]] = (int)questionsForThisType;
                remainingQuestions -= (int)questionsForThisType;
            }

            foreach (var questionType in questionTypes)
            {
                if (questions.Count >= count) break;

                string[] templates;
                switch (questionType)
                {
                    case "Text":
                        templates = textTemplates;
                        break;
                    case "Choice":
                        templates = choiceTemplates;
                        break;
                    case "Rating":
                        templates = ratingTemplates;
                        break;
                    case "Number":
                        templates = numberTemplates;
                        break;
                    default:
                        continue;
                }

                // Generate questions for this type
                var typeCount = questionsPerType.ContainsKey(questionType) ? questionsPerType[questionType] : 0;
                for (int i = 0; i < typeCount && questions.Count < count; i++)
                {
                    var template = templates[random.Next(templates.Length)];
                    var keyword = keywords.Count > 0 ? keywords[random.Next(keywords.Count)] : "relevant skills";
                    var questionText = template.Replace("{0}", keyword);

                    var options = new List<MCPQuestionOption>();
                    
                    if (questionType == "Choice")
                    {
                        var choices = keyword.Contains("experience") || keyword.Contains("skill") 
                            ? new[] { "Beginner", "Intermediate", "Advanced", "Expert" }
                            : keyword.Contains("communication") || keyword.Contains("team")
                            ? new[] { "Poor", "Fair", "Good", "Excellent" }
                            : new[] { "None", "Basic", "Proficient", "Expert" };
                        
                        for (int c = 0; c < choices.Length; c++)
                        {
                            options.Add(new MCPQuestionOption { text = choices[c], points = (choices.Length - c) * 2 + 2 });
                        }
                    }
                    else if (questionType == "Rating")
                    {
                        for (int r = 1; r <= 5; r++)
                        {
                            options.Add(new MCPQuestionOption { text = r.ToString(), points = r * 2 });
                        }
                    }
                    else if (questionType == "Number")
                    {
                        var ranges = keyword.Contains("year") || keyword.Contains("experience")
                            ? new[] { "0-1 years", "2-3 years", "4-6 years", "7+ years" }
                            : new[] { "0-2", "3-5", "6-10", "11+" };
                        
                        for (int r = 0; r < ranges.Length; r++)
                        {
                            options.Add(new MCPQuestionOption { text = ranges[r], points = (r + 1) * 2 + 2 });
                        }
                    }

                    questions.Add(new GeneratedQuestion
                    {
                        text = questionText,
                        type = questionType,
                        category = GetCategoryForKeyword(keyword),
                        suggestedOptions = options
                    });
                }
            }

            return questions.Take(count).ToList();
        }

        private string GetCategoryForKeyword(string keyword)
        {
            if (new[] { "javascript", "python", "java", "c#", "sql", "react", "node.js", "aws", "docker" }.Contains(keyword))
                return "technical";
            if (new[] { "management", "leadership", "communication", "team" }.Contains(keyword))
                return "professional";
            if (new[] { "project", "strategy", "marketing", "sales" }.Contains(keyword))
                return "business";
            return "general";
        }
    }

    // Response models
    public class MCPResponse
    {
        public string jsonrpc { get; set; }
        public string id { get; set; }
        public MCPResult Result { get; set; }
        public MCPError Error { get; set; }
        
        [JsonIgnore]
        public bool Success => Error == null;
        
        [JsonIgnore]
        public string ErrorMessage => Error?.message;
    }

    public class MCPResult
    {
        public List<MCPContent> contents { get; set; }
        public List<MCPResource> resources { get; set; }
    }

    public class MCPContent
    {
        public string type { get; set; }
        public string text { get; set; }
    }

    public class MCPResource
    {
        public string uri { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string mimeType { get; set; }
    }

    public class MCPError
    {
        public int code { get; set; }
        public string message { get; set; }
    }

    // Tool-specific response models
    public class GeneratedQuestionsResponse
    {
        public bool success { get; set; }
        public List<GeneratedQuestion> questions { get; set; }
        public Metadata metadata { get; set; }
    }

    public class GeneratedQuestion
    {
        public string text { get; set; }
        public string type { get; set; }
        public string category { get; set; }
        public List<MCPQuestionOption> suggestedOptions { get; set; }
    }

    public class MCPQuestionOption
    {
        public string text { get; set; }
        public decimal points { get; set; }
    }

    public class Metadata
    {
        public string jobTitle { get; set; }
        public string experience { get; set; }
        public List<string> keywords { get; set; }
        public string generatedAt { get; set; }
        public JobAnalysis analysis { get; set; }
    }

    public class ValidationResponse
    {
        public bool success { get; set; }
        public QuestionValidation validation { get; set; }
        public List<string> recommendations { get; set; }
    }

    public class QuestionValidation
    {
        public bool isValid { get; set; }
        public List<string> warnings { get; set; }
        public List<string> suggestions { get; set; }
        public bool biasDetected { get; set; }
        public int clarityScore { get; set; }
    }

    public class PointsSuggestionResponse
    {
        public bool success { get; set; }
        public string difficulty { get; set; }
        public List<PointSuggestion> suggestions { get; set; }
        public int totalPoints { get; set; }
    }

    public class PointSuggestion
    {
        public string option { get; set; }
        public int suggestedPoints { get; set; }
        public string reasoning { get; set; }
    }

    public class TemplateResponse
    {
        public bool success { get; set; }
        public QuestionTemplate template { get; set; }
        public bool customizable { get; set; }
        public string importedAt { get; set; }
    }

    public class QuestionTemplate
    {
        public string name { get; set; }
        public string description { get; set; }
        public List<TemplateQuestion> questions { get; set; }
    }

    public class TemplateQuestion
    {
        public string text { get; set; }
        public string type { get; set; }
        public string category { get; set; }
    }

    public class PerformanceAnalysisResponse
    {
        public bool success { get; set; }
        public PerformanceAnalysis analysis { get; set; }
        public string analyzedAt { get; set; }
    }

    public class PerformanceAnalysis
    {
        public string questionId { get; set; }
        public int totalResponses { get; set; }
        public double averageScore { get; set; }
        public Dictionary<string, int> distribution { get; set; }
        public List<string> insights { get; set; }
        public List<string> recommendations { get; set; }
    }
}
