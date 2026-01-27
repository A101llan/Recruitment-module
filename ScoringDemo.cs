using System;
using System.Collections.Generic;
using System.Linq;
using HR.Web.Services;
using HR.Web.Models;

namespace HR.Web.Demo
{
    public class ScoringDemo
    {
        public static void DemonstrateScoring()
        {
            var scoringService = new ScoringService();
            
            // Sample question for Full-Stack Developer position
            var question = new Question
            {
                Id = 1,
                Text = "Describe your experience with software development and any specific projects you've worked on.",
                Type = "text"
            };

            Console.WriteLine("=== ENHANCED SCORING ALGORITHM DEMONSTRATION ===\n");
            
            // Sample answers with different quality levels
            var sampleAnswers = new[]
            {
                new 
                { 
                    Answer = "I have 5 years of experience in software development. I worked on various projects using React and Node.js. I developed a customer management system that increased efficiency by 30%. I led a team of 3 developers and we successfully delivered the project on time and within budget.",
                    Description = "Strong Answer - Specific, quantifiable, professional"
                },
                new 
                { 
                    Answer = "I do software development. I have worked on some projects. I know JavaScript and React. I like coding and building things.",
                    Description = "Weak Answer - Vague, no specifics, minimal detail"
                },
                new 
                { 
                    Answer = "With over 7 years of full-stack development experience, I have architected and implemented scalable web applications using modern technology stacks. For example, I recently led the development of a microservices-based e-commerce platform that processed over 100,000 transactions daily, resulting in a 45% increase in revenue. I implemented CI/CD pipelines using Docker and Kubernetes, reducing deployment time by 60%. Additionally, I mentored junior developers and established coding standards that improved team productivity by 25%. My technical expertise includes React, Angular, Node.js, .NET Core, PostgreSQL, and cloud services like AWS and Azure.",
                    Description = "Excellent Answer - Highly detailed, quantifiable, technical depth"
                }
            };

            for (int i = 0; i < sampleAnswers.Length; i++)
            {
                var sample = sampleAnswers[i];
                Console.WriteLine($"--- {sample.Description} ---");
                Console.WriteLine($"Answer: \"{sample.Answer}\"\n");
                
                // Calculate score using enhanced algorithm
                var score = scoringService.CalculateQuestionScore(question, sample.Answer, 1);
                
                Console.WriteLine($"Total Score: {score:F1}/10.0\n");
                Console.WriteLine(new string('-', 80));
            }
        }
    }
}
