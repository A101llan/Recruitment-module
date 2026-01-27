using System.ComponentModel.DataAnnotations;

namespace HR.Web.Models
{
    public class RegisterViewModel
    {
        [Required, StringLength(100)]
        public string UserName { get; set; }

        [Required, EmailAddress, StringLength(100)]
        public string Email { get; set; }

        [StringLength(50)]
        public string Role { get; set; } // Admin or Client

        [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", 
            ErrorMessage = "Password must be at least 8 characters long and contain at least one uppercase letter, one lowercase letter, one number, and one special character.")]
        public string Password { get; set; }

        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; }
    }
}
