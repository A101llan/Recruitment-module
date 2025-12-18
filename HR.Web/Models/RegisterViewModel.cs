using System.ComponentModel.DataAnnotations;

namespace HR.Web.Models
{
    public class RegisterViewModel
    {
        [Required, StringLength(100)]
        public string UserName { get; set; }

        [Required, EmailAddress, StringLength(100)]
        public string Email { get; set; }

        [Required, StringLength(50)]
        public string Role { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
