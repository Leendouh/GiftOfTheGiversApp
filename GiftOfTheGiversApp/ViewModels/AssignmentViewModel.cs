using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GiftOfTheGiversApp.ViewModels
{
    public class AssignmentViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please select a volunteer")]
        [Display(Name = "Volunteer")]
        public int VolunteerId { get; set; }

        [Required(ErrorMessage = "Please select a disaster")]
        [Display(Name = "Disaster")]
        public int DisasterId { get; set; }

        [Required(ErrorMessage = "Role is required")]
        [Display(Name = "Role")]
        public string RoleInAssignment { get; set; }

        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; } = "Assigned";

        // For dropdowns
        public List<SelectListItem> Volunteers { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Disasters { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Roles { get; set; } = new List<SelectListItem>();
    }
}