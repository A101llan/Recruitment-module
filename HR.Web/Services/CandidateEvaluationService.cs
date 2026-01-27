using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HR.Web.Data;
using HR.Web.Models;

namespace HR.Web.Services
{
    public interface ICandidateEvaluationService
    {
        CandidateScore EvaluateApplication(int applicationId, ApplicationReviewViewModel review, List<ApplicationAnswer> answers);
    }

    public class CandidateScore
    {
        public CandidateScore()
        {
            CategoryScores = new Dictionary<string, decimal>();
        }

        public decimal Score { get; set; } // 0-100
        public string Reason { get; set; }
        public Dictionary<string, decimal> CategoryScores { get; set; }
    }

    public class CandidateEvaluationService : ICandidateEvaluationService
    {
        private readonly UnitOfWork _uow = new UnitOfWork();

        public CandidateScore EvaluateApplication(int applicationId, ApplicationReviewViewModel review, List<ApplicationAnswer> answers)
        {
            var score = new CandidateScore();
            var categoryScores = new Dictionary<string, decimal>();

            // 1. Answer Completeness (20 points)
            decimal completenessScore = EvaluateCompleteness(review, answers);
            categoryScores["Completeness"] = completenessScore;

            // 2. Answer Quality & Relevance (30 points)
            decimal qualityScore = EvaluateQuality(review, answers);
            categoryScores["Quality"] = qualityScore;

            // 3. Experience Level (25 points)
            decimal experienceScore = EvaluateExperience(review);
            categoryScores["Experience"] = experienceScore;

            // 4. Motivation & Fit (15 points)
            decimal motivationScore = EvaluateMotivation(review);
            categoryScores["Motivation"] = motivationScore;

            // 5. Professionalism (10 points)
            decimal professionalismScore = EvaluateProfessionalism(review, answers);
            categoryScores["Professionalism"] = professionalismScore;

            // Calculate total score
            score.Score = Math.Round(
                completenessScore + qualityScore + experienceScore + motivationScore + professionalismScore,
                2
            );

            // Generate reason
            score.Reason = GenerateScoreReason(score.Score, categoryScores);
            score.CategoryScores = categoryScores;

            return score;
        }

        private decimal EvaluateCompleteness(ApplicationReviewViewModel review, List<ApplicationAnswer> answers)
        {
            decimal score = 0;
            int totalFields = 0;
            int completedFields = 0;

            // Check standard fields (now 12 fields including new skill ratings)
            totalFields += 12; // WhyInterested, InterestLevel, YearsInField, YearsInRole, ExpectedSalary, EducationLevel, WorkAvailability, WorkMode, AvailabilityToStart, CommunicationSkills, ProblemSolvingSkills, TeamworkSkills
            if (!string.IsNullOrWhiteSpace(review.WhyInterested)) completedFields++;
            if (!string.IsNullOrWhiteSpace(review.InterestLevel)) completedFields++;
            if (!string.IsNullOrWhiteSpace(review.YearsInField)) completedFields++;
            if (!string.IsNullOrWhiteSpace(review.YearsInRole)) completedFields++;
            if (!string.IsNullOrWhiteSpace(review.ExpectedSalary)) completedFields++;
            if (!string.IsNullOrWhiteSpace(review.EducationLevel)) completedFields++;
            if (!string.IsNullOrWhiteSpace(review.WorkAvailability)) completedFields++;
            if (!string.IsNullOrWhiteSpace(review.WorkMode)) completedFields++;
            if (!string.IsNullOrWhiteSpace(review.AvailabilityToStart)) completedFields++;
            if (!string.IsNullOrWhiteSpace(review.CommunicationSkills)) completedFields++;
            if (!string.IsNullOrWhiteSpace(review.ProblemSolvingSkills)) completedFields++;
            if (!string.IsNullOrWhiteSpace(review.TeamworkSkills)) completedFields++;

            // Check dynamic answers
            if (answers != null && answers.Any())
            {
                totalFields += answers.Count;
                completedFields += answers.Count(a => !string.IsNullOrWhiteSpace(a.AnswerText));
            }

            // Check resume
            if (!string.IsNullOrWhiteSpace(review.ResumePath)) completedFields++;
            totalFields++;

            if (totalFields > 0)
            {
                score = (completedFields / (decimal)totalFields) * 20m;
            }

            return Math.Round(score, 2);
        }

        private decimal EvaluateQuality(ApplicationReviewViewModel review, List<ApplicationAnswer> answers)
        {
            decimal score = 0;

            // Score based on number of interest reasons selected (structured)
            if (!string.IsNullOrWhiteSpace(review.WhyInterested))
            {
                var reasons = review.WhyInterested.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var reasonCount = reasons.Length;
                // More reasons = higher score (shows genuine interest)
                if (reasonCount >= 4) score += 10;
                else if (reasonCount >= 3) score += 7;
                else if (reasonCount >= 2) score += 5;
                else if (reasonCount >= 1) score += 3;
            }

            // Score interest level (1-5 scale, directly convert to points)
            if (!string.IsNullOrWhiteSpace(review.InterestLevel) && int.TryParse(review.InterestLevel, out int interestLevel))
            {
                score += interestLevel * 2; // 1=2pts, 2=4pts, 3=6pts, 4=8pts, 5=10pts (max 10)
            }

            // Score skill self-assessments (average them)
            decimal skillAvg = 0;
            int skillCount = 0;
            if (!string.IsNullOrWhiteSpace(review.CommunicationSkills) && int.TryParse(review.CommunicationSkills, out int comm))
            {
                skillAvg += comm;
                skillCount++;
            }
            if (!string.IsNullOrWhiteSpace(review.ProblemSolvingSkills) && int.TryParse(review.ProblemSolvingSkills, out int problem))
            {
                skillAvg += problem;
                skillCount++;
            }
            if (!string.IsNullOrWhiteSpace(review.TeamworkSkills) && int.TryParse(review.TeamworkSkills, out int team))
            {
                skillAvg += team;
                skillCount++;
            }
            if (skillCount > 0)
            {
                skillAvg = skillAvg / skillCount;
                score += skillAvg * 2; // Convert 1-5 scale to 2-10 points
            }

            // Evaluate dynamic answers quality (now 1-5 ratings)
            decimal dynamicScore = 0;
            if (answers != null && answers.Any())
            {
                foreach (var answer in answers)
                {
                    if (!string.IsNullOrWhiteSpace(answer.AnswerText) && int.TryParse(answer.AnswerText, out int rating))
                    {
                        dynamicScore += rating; // Direct 1-5 points per question
                    }
                }
                // Cap dynamic answers contribution at 5 points total
                dynamicScore = Math.Min(dynamicScore, 5m);
            }
            score += dynamicScore;

            // Cap total quality score at 30
            return Math.Round(Math.Min(score, 30m), 2);
        }

        private decimal EvaluateExperience(ApplicationReviewViewModel review)
        {
            decimal score = 0;

            // Years in field
            if (!string.IsNullOrWhiteSpace(review.YearsInField))
            {
                var yearsInField = ParseYears(review.YearsInField);
                if (yearsInField >= 5) score += 10;
                else if (yearsInField >= 3) score += 7;
                else if (yearsInField >= 1) score += 4;
                else score += 1;
            }

            // Years in role
            if (!string.IsNullOrWhiteSpace(review.YearsInRole))
            {
                var yearsInRole = ParseYears(review.YearsInRole);
                if (yearsInRole >= 3) score += 8;
                else if (yearsInRole >= 1) score += 5;
                else score += 2;
            }

            // Education level
            if (!string.IsNullOrWhiteSpace(review.EducationLevel))
            {
                var edu = review.EducationLevel.ToLower();
                if (edu.Contains("master") || edu.Contains("phd") || edu.Contains("doctorate"))
                    score += 7;
                else if (edu.Contains("bachelor") || edu.Contains("degree"))
                    score += 5;
                else if (edu.Contains("diploma") || edu.Contains("certificate"))
                    score += 3;
                else
                    score += 1;
            }

            // Cap at 25 points
            return Math.Round(Math.Min(score, 25m), 2);
        }

        private decimal EvaluateMotivation(ApplicationReviewViewModel review)
        {
            decimal score = 0;

            // Score interest level directly (1-5 scale = 1-5 points)
            if (!string.IsNullOrWhiteSpace(review.InterestLevel) && int.TryParse(review.InterestLevel, out int interestLevel))
            {
                score += interestLevel; // 1-5 points
            }

            // Score number of interest reasons (more = more motivated)
            if (!string.IsNullOrWhiteSpace(review.WhyInterested))
            {
                var reasons = review.WhyInterested.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var reasonCount = reasons.Length;
                if (reasonCount >= 4) score += 5;
                else if (reasonCount >= 3) score += 4;
                else if (reasonCount >= 2) score += 3;
                else if (reasonCount >= 1) score += 2;
            }

            // Availability and commitment (structured)
            if (!string.IsNullOrWhiteSpace(review.AvailabilityToStart))
            {
                var availability = review.AvailabilityToStart.ToLower();
                if (availability.Contains("immediate") || availability.Contains("immediately"))
                    score += 4;
                else if (availability.Contains("2 weeks") || availability.Contains("within 2"))
                    score += 3;
                else if (availability.Contains("month") || availability.Contains("1 month"))
                    score += 2;
                else
                    score += 1;
            }

            // Cap at 15 points
            return Math.Round(Math.Min(score, 15m), 2);
        }

        private decimal EvaluateProfessionalism(ApplicationReviewViewModel review, List<ApplicationAnswer> answers)
        {
            decimal score = 0;

            // Resume provided
            if (!string.IsNullOrWhiteSpace(review.ResumePath))
                score += 5;

            // Score skill self-assessments (higher self-ratings show confidence/professionalism)
            decimal skillAvg = 0;
            int skillCount = 0;
            if (!string.IsNullOrWhiteSpace(review.CommunicationSkills) && int.TryParse(review.CommunicationSkills, out int comm))
            {
                skillAvg += comm;
                skillCount++;
            }
            if (!string.IsNullOrWhiteSpace(review.ProblemSolvingSkills) && int.TryParse(review.ProblemSolvingSkills, out int problem))
            {
                skillAvg += problem;
                skillCount++;
            }
            if (!string.IsNullOrWhiteSpace(review.TeamworkSkills) && int.TryParse(review.TeamworkSkills, out int team))
            {
                skillAvg += team;
                skillCount++;
            }
            if (skillCount > 0)
            {
                skillAvg = skillAvg / skillCount;
                // Average of 4-5 = 3 points, 3 = 2 points, 1-2 = 1 point
                if (skillAvg >= 4) score += 3;
                else if (skillAvg >= 3) score += 2;
                else score += 1;
            }

            // Work mode preference (shows thoughtfulness)
            if (!string.IsNullOrWhiteSpace(review.WorkMode))
                score += 1;

            // All fields completed shows professionalism
            if (!string.IsNullOrWhiteSpace(review.InterestLevel) && 
                !string.IsNullOrWhiteSpace(review.CommunicationSkills) &&
                !string.IsNullOrWhiteSpace(review.ProblemSolvingSkills) &&
                !string.IsNullOrWhiteSpace(review.TeamworkSkills))
                score += 1;

            // Cap at 10 points
            return Math.Round(Math.Min(score, 10m), 2);
        }

        private int ParseYears(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return 0;

            input = input.ToLower().Trim();
            
            // Try to extract number
            var parts = input.Split(new[] { ' ', '-', '+' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (int.TryParse(part, out int years))
                {
                    return years;
                }
            }

            // Fallback: check for keywords
            if (input.Contains("5") || input.Contains("five")) return 5;
            if (input.Contains("3") || input.Contains("three")) return 3;
            if (input.Contains("1") || input.Contains("one")) return 1;
            if (input.Contains("less than 1") || input.Contains("no experience")) return 0;

            return 0;
        }

        private string GenerateScoreReason(decimal totalScore, Dictionary<string, decimal> categoryScores)
        {
            return string.Empty;
        }
    }
}

