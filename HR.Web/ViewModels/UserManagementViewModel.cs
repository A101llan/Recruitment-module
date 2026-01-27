using System;
using System.ComponentModel.DataAnnotations;
using HR.Web.Models;

namespace HR.Web.ViewModels
{
    public class UserManagementViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string UserName { get; set; }

        [Required, StringLength(100)]
        public string Email { get; set; }

        [Required, StringLength(50)]
        public string Role { get; set; }

        public string Phone { get; set; }

        public DateTime? LastLoginDate { get; set; }

        public string LastLoginIP { get; set; }

        public bool IsLocked { get; set; }

        public DateTime? LockoutEndTime { get; set; }

        public int FailedLoginAttempts { get; set; }

        public DateTime CreatedDate { get; set; }

        public string Status
        {
            get
            {
                if (IsLocked)
                    return "Locked";
                return "Active";
            }
        }

        public string StatusBadgeClass
        {
            get
            {
                if (IsLocked)
                    return "badge-danger";
                return "badge-success";
            }
        }

        public string RoleBadgeClass
        {
            get
            {
                switch (Role?.ToLower())
                {
                    case "admin":
                        return "badge-danger";
                    case "client":
                        return "badge-primary";
                    default:
                        return "badge-secondary";
                }
            }
        }
    }

    public class UserRoleUpdateViewModel
    {
        public int UserId { get; set; }

        [Required, StringLength(100)]
        public string UserName { get; set; }

        [Required, StringLength(100)]
        public string Email { get; set; }

        [Required, StringLength(50)]
        public string CurrentRole { get; set; }

        [Required, StringLength(50)]
        public string NewRole { get; set; }

        public string Reason { get; set; }
    }
}
