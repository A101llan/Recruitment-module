using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HR.Web.Models
{
    public class User : ITenantEntity
    {
        public int Id { get; set; }

        public int? CompanyId { get; set; }
        public virtual Company Company { get; set; }

        [Required, StringLength(100)]
        public string UserName { get; set; }

        [Required, StringLength(100)]
        public string Email { get; set; }

        [Required, StringLength(50)]
        public string Role { get; set; } // Admin, HR

        [Required, StringLength(256)]
        public string PasswordHash { get; set; }

        public User()
        {
            RequirePasswordChange = false;
        }

        public bool RequirePasswordChange { get; set; }
        
        public System.DateTime? LastPasswordChange { get; set; }
        
        public System.DateTime? PasswordChangeExpiry { get; set; }

        public virtual ICollection<Interview> Interviews { get; set; }
    }
}










































