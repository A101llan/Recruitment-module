using System;

namespace HR.Web.Models
{
    /// <summary>
    /// View model used to review an application questionnaire before final submission.
    /// </summary>
    public class ApplicationReviewViewModel
    {
        public int PositionId { get; set; }
        public string PositionTitle { get; set; }

        public string ApplicantName { get; set; }
        public string ApplicantEmail { get; set; }

        // Questionnaire answers
        public string WhyInterested { get; set; }
        public string YearsInField { get; set; }
        public string YearsInRole { get; set; }
        public string ExpectedSalary { get; set; }
        public string EducationLevel { get; set; }
        public string WorkAvailability { get; set; }
        public string WorkMode { get; set; }
        public string AvailabilityToStart { get; set; }

        // Saved resume file path
        public string ResumePath { get; set; }
    }
}










