using System.ComponentModel.DataAnnotations;

namespace GiftOfTheGiversApp.Models
{
    public class RoleManagementViewModel
    {
        public string UserId { get; set; }

        [Display(Name = "User Name")]
        public string UserName { get; set; }

        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Current Roles")]
        public List<string> CurrentRoles { get; set; } = new List<string>();

        [Display(Name = "Available Roles")]
        public List<string> AvailableRoles { get; set; } = new List<string>();

        [Display(Name = "Select Roles")]
        public List<string> SelectedRoles { get; set; } = new List<string>();
    }

    public class UserRoleViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public List<UserRole> Roles { get; set; } = new List<UserRole>();
    }

    public class UserRole
    {
        public string RoleName { get; set; }
        public bool IsSelected { get; set; }
    }
}