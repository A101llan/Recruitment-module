using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HR.Web.Services;
using HR.Web.Models;
using HR.Web.Data;
using Moq;
using System.Data.Entity;

namespace HR.Web.Tests.Services
{
    [TestClass]
    public class ScoringServiceTests
    {
        private ScoringService _scoringService;
        private Mock<UnitOfWork> _mockUnitOfWork;
        private Mock<HRContext> _mockContext;

        [TestInitialize]
        public void Setup()
        {
            _mockContext = new Mock<HRContext>();
            _mockUnitOfWork = new Mock<UnitOfWork>(_mockContext.Object);
            _scoringService = new ScoringService();
        }

        [TestMethod]
        public void RankCandidatesForPosition_ShouldRankByPercentageDescending()
        {
            // Arrange
            var positionId = 1;
            var applications = CreateTestApplications();
            var questions = CreateTestQuestions();
            var answers = CreateTestAnswers();
            var positionQuestions = CreatePositionQuestions();

            // Setup mocks
            var mockApplicationsSet = CreateMockDbSet(applications);
            var mockQuestionsSet = CreateMockDbSet(questions);
            var mockAnswersSet = CreateMockDbSet(answers);
            var mockPositionQuestionsSet = CreateMockDbSet(positionQuestions);
            var mockQuestionOptionsSet = CreateMockDbSet(CreateQuestionOptions());

            _mockContext.Setup(c => c.Set<Application>()).Returns(mockApplicationsSet.Object);
            _mockContext.Setup(c => c.Set<Question>()).Returns(mockQuestionsSet.Object);
            _mockContext.Setup(c => c.Set<ApplicationAnswer>()).Returns(mockAnswersSet.Object);
            _mockContext.Setup(c => c.Set<PositionQuestion>()).Returns(mockPositionQuestionsSet.Object);
            _mockContext.Setup(c => c.Set<QuestionOption>()).Returns(mockQuestionOptionsSet.Object);

            _mockUnitOfWork.Setup(uow => uow.Applications.GetAll(
                It.IsAny<System.Linq.Expressions.Expression<Func<Application, object>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Application, object>>>()))
                .Returns(applications);

            // Act
            var rankings = _scoringService.RankCandidatesForPosition(positionId);

            // Assert
            Assert.AreEqual(3, rankings.Count);
            
            // Verify ranking is in descending order by percentage
            Assert.IsTrue(rankings[0].Percentage >= rankings[1].Percentage);
            Assert.IsTrue(rankings[1].Percentage >= rankings[2].Percentage);

            // Verify specific ranking order based on our test data
            Assert.AreEqual("Alice Johnson", rankings[0].CandidateName); // Highest score
            Assert.AreEqual("Bob Smith", rankings[1].CandidateName);   // Medium score  
            Assert.AreEqual("Charlie Brown", rankings[2].CandidateName); // Lowest score
        }

        [TestMethod]
        public void CalculateApplicationScore_ShouldCalculateCorrectTotalScore()
        {
            // Arrange
            var application = CreateTestApplications().First(); // Alice Johnson (highest scorer)
            var questions = CreateTestQuestions();
            var answers = CreateTestAnswers().Where(a => a.ApplicationId == application.Id).ToList();
            var positionQuestions = CreatePositionQuestions();

            // Setup mocks
            var mockQuestionsSet = CreateMockDbSet(questions);
            var mockAnswersSet = CreateMockDbSet(answers);
            var mockPositionQuestionsSet = CreateMockDbSet(positionQuestions);
            var mockQuestionOptionsSet = CreateMockDbSet(CreateQuestionOptions());

            _mockContext.Setup(c => c.Set<Question>()).Returns(mockQuestionsSet.Object);
            _mockContext.Setup(c => c.Set<ApplicationAnswer>()).Returns(mockAnswersSet.Object);
            _mockContext.Setup(c => c.Set<PositionQuestion>()).Returns(mockPositionQuestionsSet.Object);
            _mockContext.Setup(c => c.Set<QuestionOption>()).Returns(mockQuestionOptionsSet.Object);

            // Act
            var score = _scoringService.CalculateApplicationScore(application);

            // Assert
            // Alice should have the highest score (based on our test data)
            // Expected: Choice(10) + Rating(8) + Number(10) + Text(9) = 37
            Assert.AreEqual(37m, score);
        }

        [TestMethod]
        public void CalculateQuestionScore_ShouldReturnCorrectScoresForDifferentQuestionTypes()
        {
            // Test Choice question
            var choiceQuestion = CreateTestQuestions().First(q => q.Type == "Choice");
            var choiceScore = _scoringService.CalculateQuestionScore(choiceQuestion, "Expert", 1);
            Assert.AreEqual(10m, choiceScore);

            // Test Rating question  
            var ratingQuestion = CreateTestQuestions().First(q => q.Type == "Rating");
            var ratingScore = _scoringService.CalculateQuestionScore(ratingQuestion, "4", 1);
            Assert.AreEqual(8m, ratingScore); // 4 * 2 = 8

            // Test Number question
            var numberQuestion = CreateTestQuestions().First(q => q.Type == "Number");
            var numberScore = _scoringService.CalculateQuestionScore(numberQuestion, "5", 1);
            Assert.AreEqual(10m, numberScore); // 5 years * 2 = 10

            // Test Text question
            var textQuestion = CreateTestQuestions().First(q => q.Type == "Text");
            var textScore = _scoringService.CalculateQuestionScore(textQuestion, "I have extensive experience developing and implementing complex solutions that achieved significant results", 1);
            Assert.IsTrue(textScore > 0); // Should score based on length and keywords
        }

        [TestMethod]
        public void GetScoreBreakdown_ShouldProvideDetailedBreakdown()
        {
            // Arrange
            var applicationId = 1;
            var application = CreateTestApplications().First();
            var questions = CreateTestQuestions();
            var answers = CreateTestAnswers().Where(a => a.ApplicationId == applicationId).ToList();
            var positionQuestions = CreatePositionQuestions();

            // Setup mocks
            var mockApplicationsSet = CreateMockDbSet(new List<Application> { application });
            var mockQuestionsSet = CreateMockDbSet(questions);
            var mockAnswersSet = CreateMockDbSet(answers);
            var mockPositionQuestionsSet = CreateMockDbSet(positionQuestions);
            var mockQuestionOptionsSet = CreateMockDbSet(CreateQuestionOptions());

            _mockContext.Setup(c => c.Set<Application>()).Returns(mockApplicationsSet.Object);
            _mockContext.Setup(c => c.Set<Question>()).Returns(mockQuestionsSet.Object);
            _mockContext.Setup(c => c.Set<ApplicationAnswer>()).Returns(mockAnswersSet.Object);
            _mockContext.Setup(c => c.Set<PositionQuestion>()).Returns(mockPositionQuestionsSet.Object);
            _mockContext.Setup(c => c.Set<QuestionOption>()).Returns(mockQuestionOptionsSet.Object);

            _mockUnitOfWork.Setup(uow => uow.Applications.Get(applicationId)).Returns(application);

            // Act
            var breakdown = _scoringService.GetScoreBreakdown(applicationId);

            // Assert
            Assert.AreEqual(4, breakdown.Count); // 4 questions
            Assert.IsTrue(breakdown.All(b => b.Score >= 0 && b.Score <= b.MaxScore));
            Assert.IsTrue(breakdown.All(b => b.Percentage >= 0 && b.Percentage <= 100));
        }

        [TestMethod]
        public void RankCandidatesForPosition_ShouldHandleEmptyApplicationsList()
        {
            // Arrange
            var positionId = 999; // Non-existent position
            var emptyApplications = new List<Application>();

            var mockApplicationsSet = CreateMockDbSet(emptyApplications);
            _mockContext.Setup(c => c.Set<Application>()).Returns(mockApplicationsSet.Object);

            _mockUnitOfWork.Setup(uow => uow.Applications.GetAll(
                It.IsAny<System.Linq.Expressions.Expression<Func<Application, object>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Application, object>>>()))
                .Returns(emptyApplications);

            // Act
            var rankings = _scoringService.RankCandidatesForPosition(positionId);

            // Assert
            Assert.AreEqual(0, rankings.Count);
        }

        [TestMethod]
        public void CalculateApplicationScore_ShouldHandleMissingAnswers()
        {
            // Arrange
            var application = new Application 
            { 
                Id = 999, 
                PositionId = 1, 
                ApplicantId = 1,
                AppliedOn = DateTime.Now
            };
            var questions = CreateTestQuestions();
            var positionQuestions = CreatePositionQuestions();
            // No answers for this application

            // Setup mocks
            var mockQuestionsSet = CreateMockDbSet(questions);
            var mockAnswersSet = CreateMockDbSet(new List<ApplicationAnswer>());
            var mockPositionQuestionsSet = CreateMockDbSet(positionQuestions);

            _mockContext.Setup(c => c.Set<Question>()).Returns(mockQuestionsSet.Object);
            _mockContext.Setup(c => c.Set<ApplicationAnswer>()).Returns(mockAnswersSet.Object);
            _mockContext.Setup(c => c.Set<PositionQuestion>()).Returns(mockPositionQuestionsSet.Object);

            // Act
            var score = _scoringService.CalculateApplicationScore(application);

            // Assert
            Assert.AreEqual(0m, score); // No answers should result in 0 score
        }

        #region Test Data Helpers

        private List<Application> CreateTestApplications()
        {
            return new List<Application>
            {
                new Application 
                { 
                    Id = 1, 
                    PositionId = 1, 
                    ApplicantId = 1,
                    AppliedOn = DateTime.Now.AddDays(-3),
                    Status = "Under Review",
                    Applicant = new Applicant { Id = 1, FullName = "Alice Johnson", Email = "alice@test.com" }
                },
                new Application 
                { 
                    Id = 2, 
                    PositionId = 1, 
                    ApplicantId = 2,
                    AppliedOn = DateTime.Now.AddDays(-2),
                    Status = "Under Review", 
                    Applicant = new Applicant { Id = 2, FullName = "Bob Smith", Email = "bob@test.com" }
                },
                new Application 
                { 
                    Id = 3, 
                    PositionId = 1, 
                    ApplicantId = 3,
                    AppliedOn = DateTime.Now.AddDays(-1),
                    Status = "Under Review",
                    Applicant = new Applicant { Id = 3, FullName = "Charlie Brown", Email = "charlie@test.com" }
                }
            };
        }

        private List<Question> CreateTestQuestions()
        {
            return new List<Question>
            {
                new Question { Id = 1, Text = "What is your experience level?", Type = "Choice", Category = "experience" },
                new Question { Id = 2, Text = "Rate your communication skills", Type = "Rating", Category = "soft-skills" },
                new Question { Id = 3, Text = "How many years of experience do you have?", Type = "Number", Category = "experience" },
                new Question { Id = 4, Text = "Describe your most challenging project", Type = "Text", Category = "technical" }
            };
        }

        private List<PositionQuestion> CreatePositionQuestions()
        {
            return new List<PositionQuestion>
            {
                new PositionQuestion { Id = 1, PositionId = 1, QuestionId = 1, Order = 1, IsRequired = true },
                new PositionQuestion { Id = 2, PositionId = 1, QuestionId = 2, Order = 2, IsRequired = true },
                new PositionQuestion { Id = 3, PositionId = 1, QuestionId = 3, Order = 3, IsRequired = true },
                new PositionQuestion { Id = 4, PositionId = 1, QuestionId = 4, Order = 4, IsRequired = true }
            };
        }

        private List<QuestionOption> CreateQuestionOptions()
        {
            return new List<QuestionOption>
            {
                new QuestionOption { Id = 1, QuestionId = 1, Text = "Beginner", Points = 2 },
                new QuestionOption { Id = 2, QuestionId = 1, Text = "Intermediate", Points = 5 },
                new QuestionOption { Id = 3, QuestionId = 1, Text = "Advanced", Points = 8 },
                new QuestionOption { Id = 4, QuestionId = 1, Text = "Expert", Points = 10 }
            };
        }

        private List<ApplicationAnswer> CreateTestAnswers()
        {
            return new List<ApplicationAnswer>
            {
                // Alice Johnson - Highest scorer (37 points total)
                new ApplicationAnswer { Id = 1, ApplicationId = 1, QuestionId = 1, AnswerText = "Expert" },           // 10 points
                new ApplicationAnswer { Id = 2, ApplicationId = 1, QuestionId = 2, AnswerText = "4" },                  // 8 points (4*2)
                new ApplicationAnswer { Id = 3, ApplicationId = 1, QuestionId = 3, AnswerText = "5" },                  // 10 points (5*2)
                new ApplicationAnswer { Id = 4, ApplicationId = 1, QuestionId = 4, AnswerText = "I have extensive experience developing and implementing complex solutions that achieved significant results for my organization." }, // 9 points

                // Bob Smith - Medium scorer (28 points total)
                new ApplicationAnswer { Id = 5, ApplicationId = 2, QuestionId = 1, AnswerText = "Advanced" },          // 8 points
                new ApplicationAnswer { Id = 6, ApplicationId = 2, QuestionId = 2, AnswerText = "3" },                  // 6 points (3*2)
                new ApplicationAnswer { Id = 7, ApplicationId = 2, QuestionId = 3, AnswerText = "3" },                  // 6 points (3*2)
                new ApplicationAnswer { Id = 8, ApplicationId = 2, QuestionId = 4, AnswerText = "I have worked on several projects and handled various tasks." }, // 8 points

                // Charlie Brown - Lowest scorer (19 points total)
                new ApplicationAnswer { Id = 9, ApplicationId = 3, QuestionId = 1, AnswerText = "Intermediate" },       // 5 points
                new ApplicationAnswer { Id = 10, ApplicationId = 3, QuestionId = 2, AnswerText = "2" },                 // 4 points (2*2)
                new ApplicationAnswer { Id = 11, ApplicationId = 3, QuestionId = 3, AnswerText = "2" },                 // 4 points (2*2)
                new ApplicationAnswer { Id = 12, ApplicationId = 3, QuestionId = 4, AnswerText = "I did some work." }   // 6 points
            };
        }

        #endregion

        #region Mock Helpers

        private static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
        {
            var queryable = data.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();

            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

            mockSet.Setup(m => m.Add(It.IsAny<T>())).Callback<T>(data.Add);
            mockSet.Setup(m => m.Remove(It.IsAny<T>())).Callback<T>(item => data.Remove(item));
            mockSet.Setup(m => m.Contains(It.IsAny<T>())).Returns<T>(item => data.Contains(item));

            return mockSet;
        }

        #endregion
    }

    #region Additional Test Classes for Comprehensive Testing

    [TestClass]
    public class ScoringServiceIntegrationTests
    {
        [TestMethod]
        public void ScoringConsistency_TestAllQuestionTypes()
        {
            // This test ensures scoring is consistent across all question types
            var scoringService = new ScoringService();
            
            // Test data with known expected scores
            var testCases = new[]
            {
                new { QuestionType = "Choice", Answer = "Expert", ExpectedScore = 10m },
                new { QuestionType = "Choice", Answer = "Advanced", ExpectedScore = 8m },
                new { QuestionType = "Choice", Answer = "Intermediate", ExpectedScore = 5m },
                new { QuestionType = "Choice", Answer = "Beginner", ExpectedScore = 2m },
                new { QuestionType = "Rating", Answer = "5", ExpectedScore = 10m },
                new { QuestionType = "Rating", Answer = "4", ExpectedScore = 8m },
                new { QuestionType = "Rating", Answer = "3", ExpectedScore = 6m },
                new { QuestionType = "Rating", Answer = "2", ExpectedScore = 4m },
                new { QuestionType = "Rating", Answer = "1", ExpectedScore = 2m },
                new { QuestionType = "Number", Answer = "10", ExpectedScore = 10m },
                new { QuestionType = "Number", Answer = "5", ExpectedScore = 10m },
                new { QuestionType = "Number", Answer = "3", ExpectedScore = 6m },
                new { QuestionType = "Number", Answer = "1", ExpectedScore = 2m }
            };

            foreach (var testCase in testCases)
            {
                var question = new Question 
                { 
                    Id = 1, 
                    Text = $"Test {testCase.QuestionType} question", 
                    Type = testCase.QuestionType,
                    Category = "test"
                };

                var actualScore = scoringService.CalculateQuestionScore(question, testCase.Answer, 1);
                
                Assert.AreEqual(testCase.ExpectedScore, actualScore, 
                    $"Scoring mismatch for {testCase.QuestionType} question with answer '{testCase.Answer}'. Expected: {testCase.ExpectedScore}, Actual: {actualScore}");
            }
        }
    }

    #endregion
}
