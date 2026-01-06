using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HR.Web.Models
{
    public class Question
    {
        public int Id { get; set; }

        [Required, StringLength(255)]
        public string Text { get; set; }

        // e.g. Text, Choice, etc. For now, all free text.
        [StringLength(50)]
        public string Type { get; set; }

        public bool IsActive { get; set; }

        public virtual ICollection<PositionQuestion> PositionQuestions { get; set; }
        public virtual ICollection<ApplicationAnswer> ApplicationAnswers { get; set; }
    }
}






















