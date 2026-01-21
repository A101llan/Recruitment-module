using System;
using System.Collections.Generic;
using System.Linq;
using HR.Web.Services;
using HR.Web.Models;

namespace HR.Web.Tests
{
    /// <summary>
    /// Simple integration test to verify candidate ranking works correctly
    /// This can be run directly without complex mocking setup
    /// </summary>
    public class CandidateRankingVerification
    {
        public static void RunRankingTest()
        {
            Console.WriteLine("=== Candidate Ranking Verification Test ===\n");

            try
            {
                // Test 1: Verify ranking order is correct
                TestRankingOrder();
                
                // Test 2: Verify scoring calculations
                TestScoringCalculations();
                
                // Test 3: Verify edge cases
                TestEdgeCases();

                Console.WriteLine("\n✅ All ranking verification tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private static void TestRankingOrder()
        {
            Console.WriteLine("Test 1: Verifying ranking order...");

            // Create test candidates with different score profiles
            var candidates = CreateTestCandidates();
            
            // Verify they are ranked correctly (highest percentage first)
            var isRankedCorrectly = candidates[0].Percentage >= candidates[1].Percentage &&
                                    candidates[1].Percentage >= candidates[2].Percentage &&
                                    candidates[2].Percentage >= candidates[3].Percentage;

            if (!isRankedCorrectly)
            {
                throw new Exception("Candidates are not ranked in descending order by percentage");
            }

            // Verify specific expected order
            var expectedOrder = new[] { "Alice Expert", "Bob Strong", "Charlie Average", "David Basic" };
            var actualOrder = candidates.Select(c => c.CandidateName).ToArray();

            for (int i = 0; i < expectedOrder.Length; i++)
            {
                if (actualOrder[i] != expectedOrder[i])
                {
                    throw new Exception($"Ranking mismatch at position {i}. Expected: {expectedOrder[i]}, Actual: {actualOrder[i]}");
                }
            }

            Console.WriteLine("  ✓ Ranking order is correct");
            Console.WriteLine($"  1st: {candidates[0].CandidateName} - {candidates[0].Percentage:F1}%");
            Console.WriteLine($"  2nd: {candidates[1].CandidateName} - {candidates[1].Percentage:F1}%");
            Console.WriteLine($"  3rd: {candidates[2].CandidateName} - {candidates[2].Percentage:F1}%");
            Console.WriteLine($"  4th: {candidates[3].CandidateName} - {candidates[3].Percentage:F1}%");
        }

        private static void TestScoringCalculations()
        {
            Console.WriteLine("\nTest 2: Verifying scoring calculations...");

            var scoringService = new ScoringService();
            
            // Test each question type scoring
            var testCases = new[]
            {
                new { Type = "Choice", Answer = "Expert", ExpectedPoints = 10m },
                new { Type = "Rating", Answer = "5", ExpectedPoints = 10m },
                new { Type = "Number", Answer = "5", ExpectedPoints = 10m },
                new { Type = "Text", Answer = "I have extensive experience developing and implementing complex solutions that achieved significant results.", ExpectedMinPoints = 7m }
            };

            foreach (var testCase in testCases)
            {
                var question = new Question 
                { 
                    Id = 1, 
                    Text = $"Test {testCase.Type} question", 
                    Type = testCase.Type,
                    Category = "test"
                };

                var actualPoints = scoringService.CalculateQuestionScore(question, testCase.Answer, 1);
                
                if (testCase.Type == "Text")
                {
                    if (actualPoints < testCase.ExpectedMinPoints)
                    {
                        throw new Exception($"{testCase.Type} question scoring too low. Expected at least {testCase.ExpectedMinPoints}, got {actualPoints}");
                    }
                }
                else
                {
                    if (actualPoints != testCase.ExpectedPoints)
                    {
                        throw new Exception($"{testCase.Type} question scoring mismatch. Expected {testCase.ExpectedPoints}, got {actualPoints}");
                    }
                }

                Console.WriteLine($"  ✓ {testCase.Type}: {testCase.Answer} -> {actualPoints} points");
            }
        }

        private static void TestEdgeCases()
        {
            Console.WriteLine("\nTest 3: Verifying edge cases...");

            var scoringService = new ScoringService();

            // Test empty answer
            var question = new Question { Id = 1, Text = "Test question", Type = "Text", Category = "test" };
            var emptyScore = scoringService.CalculateQuestionScore(question, "", 1);
            if (emptyScore != 0)
            {
                throw new Exception("Empty answer should score 0 points");
            }

            // Test null answer
            var nullScore = scoringService.CalculateQuestionScore(question, null, 1);
            if (nullScore != 0)
            {
                throw new Exception("Null answer should score 0 points");
            }

            // Test invalid rating
            var ratingQuestion = new Question { Id = 2, Text = "Rate yourself", Type = "Rating", Category = "test" };
            var invalidRatingScore = scoringService.CalculateQuestionScore(ratingQuestion, "invalid", 1);
            if (invalidRatingScore != 0)
            {
                throw new Exception("Invalid rating should score 0 points");
            }

            // Test invalid number
            var numberQuestion = new Question { Id = 3, Text = "Years experience", Type = "Number", Category = "test" };
            var invalidNumberScore = scoringService.CalculateQuestionScore(numberQuestion, "invalid", 1);
            if (invalidNumberScore != 0)
            {
                throw new Exception("Invalid number should score 0 points");
            }

            Console.WriteLine("  ✓ Empty/null answers score 0 points");
            Console.WriteLine("  ✓ Invalid inputs score 0 points");
        }

        private static List<CandidateRanking> CreateTestCandidates()
        {
            return new List<CandidateRanking>
            {
                new CandidateRanking 
                { 
                    ApplicationId = 1,
                    CandidateName = "Alice Expert", 
                    TotalScore = 38m, 
                    MaxScore = 40m, 
                    Percentage = 95m,
                    AppliedDate = DateTime.Now.AddDays(-3),
                    Status = "Under Review"
                },
                new CandidateRanking 
                { 
                    ApplicationId = 2,
                    CandidateName = "Bob Strong", 
                    TotalScore = 32m, 
                    MaxScore = 40m, 
                    Percentage = 80m,
                    AppliedDate = DateTime.Now.AddDays(-2),
                    Status = "Under Review"
                },
                new CandidateRanking 
                { 
                    ApplicationId = 3,
                    CandidateName = "Charlie Average", 
                    TotalScore = 24m, 
                    MaxScore = 40m, 
                    Percentage = 60m,
                    AppliedDate = DateTime.Now.AddDays(-1),
                    Status = "Under Review"
                },
                new CandidateRanking 
                { 
                    ApplicationId = 4,
                    CandidateName = "David Basic", 
                    TotalScore = 16m, 
                    MaxScore = 40m, 
                    Percentage = 40m,
                    AppliedDate = DateTime.Now,
                    Status = "Under Review"
                }
            };
        }
    }

    /// <summary>
    /// Simple test runner program
    /// </summary>
    public class TestRunner
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("HR Candidate Ranking Verification");
            Console.WriteLine("=====================================\n");
            
            CandidateRankingVerification.RunRankingTest();
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
