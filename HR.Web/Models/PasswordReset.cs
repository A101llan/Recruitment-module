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

        public bool IsUsed { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public virtual User User { get; set; }
    }
}
