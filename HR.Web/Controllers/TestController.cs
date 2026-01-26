using System;
using System.Collections.Generic;
using System.Linq;
using HR.Web.Data;
using HR.Web.Models;
using HR.Web.Services;
using System.Web.Mvc;

namespace HR.Web.Controllers
{
    public class TestController : Controller
    {
        private readonly UnitOfWork _uow = new UnitOfWork();
        private readonly ICandidateEvaluationService _evaluationService = new CandidateEvaluationService();

        public ActionResult Index()
        {
            return Content("HR Application is running! Basic test successful.");
        }
        
        public ActionResult Status()
        {
            var status = new
            {
                Application = "HR Questionnaire System",
                Status = "Running",
                Framework = "ASP.NET MVC",
                Message = "Basic functionality working"
            };
            
            return Json(status, JsonRequestBehavior.AllowGet);
        }

        public ActionResult TestScoring()
        {
            // Get the test application we just created
            var application = _uow.Applications.GetAll().FirstOrDefault(a => a.Id == 5);
            if (application == null)
            {
                return Content("Application not found");
            }

            // Get the answers
            var answers = _uow.ApplicationAnswers.GetAll().Where(a => a.ApplicationId == 5).ToList();
            
            // Create a review model
            var review = new ApplicationReviewViewModel
            {
                PositionId = application.PositionId,
                PositionTitle = "Software Developer",
                ApplicantName = "Test User",
                YearsInRole = "3-5 years",
                WhyInterested = "I am interested in this position",
                InterestLevel = "High",
                ExpectedSalary = "80000",
                EducationLevel = "Bachelor",
                WorkAvailability = "Full-time",
                WorkMode = "Remote",
                AvailabilityToStart = "2 weeks",
                CommunicationSkills = "Good",
                ProblemSolvingSkills = "Good",
                TeamworkSkills = "Good"
            };

            // Test the evaluation
            try
            {
                var score = _evaluationService.EvaluateApplication(application.Id, review, answers);
                
                var result = $@"
                <h3>Scoring Test Results</h3>
                <p><strong>Application ID:</strong> {application.Id}</p>
                <p><strong>Total Score:</strong> {score.Score}/100</p>
                <p><strong>Reason:</strong> {score.Reason}</p>
                <h4>Category Scores:</h4>
                <ul>";
                
                foreach (var category in score.CategoryScores)
                {
                    result += $"<li><strong>{category.Key}:</strong> {category.Value}</li>";
                }
                
                result += $@"</ul>
                <h4>Answers Used:</h4>
                <ul>";
                
                foreach (var answer in answers)
                {
                    result += $"<li>Question {answer.QuestionId}: {answer.AnswerText}</li>";
                }
                
                result += "</ul>";
                
                return Content(result, "text/html");
            }
            catch (Exception ex)
            {
                return Content($"Error: {ex.Message}");
            }
        }
    }
}
