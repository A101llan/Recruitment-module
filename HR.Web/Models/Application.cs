using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HR.Web.Models
{
    public class Application
    {
        public int Id { get; set; }

        [ForeignKey("Applicant")]
        public int ApplicantId { get; set; }

        [ForeignKey("Position")]
        public int PositionId { get; set; }

        [Required, StringLength(30)]
        public string Status { get; set; } // Submitted, Screening, Interviewing, Offer, Hired, Rejected

        public DateTime AppliedOn { get; set; }

        [StringLength(255)]
        public string ResumePath { get; set; }

        [StringLength(30)]
        public string WorkExperienceLevel { get; set; } // No experience, Less than 1 year, etc.

        public virtual Applicant Applicant { get; set; }
        public virtual Position Position { get; set; }
    }
}



















