using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GiftOfTheGiversApp.ViewModels
{
    public class MissionViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please select a disaster")]
        [Display(Name = "Disaster")]
        public int DisasterId { get; set; }

        [Required(ErrorMessage = "Mission title is required")]
        [Display(Name = "Mission Title")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [DataType(DataType.MultilineText)]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; }

        [Display(Name = "Assigned To")]
        public int? AssignedToId { get; set; }

        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; } = "Open";

        [Required(ErrorMessage = "Priority is required")]
        public string Priority { get; set; } = "Medium";

        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        // For dropdowns
        public List<SelectListItem> Disasters { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Volunteers { get; set; } = new List<SelectListItem>();
    }
}