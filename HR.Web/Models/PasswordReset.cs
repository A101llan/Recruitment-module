using System;
using System.ComponentModel.DataAnnotations;

namespace HR.Web.Models
{
    public class PasswordReset
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(255)]
        public string Token { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        public PasswordReset()
        {
            IsUsed = false;
            CreatedDate = DateTime.UtcNow;
        }

        public bool IsUsed { get; set; }

        public DateTime CreatedDate { get; set; }

        public virtual User User { get; set; }
    }
}
