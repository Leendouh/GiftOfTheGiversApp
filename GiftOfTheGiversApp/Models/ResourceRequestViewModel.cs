using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GiftOfTheGiversApp.Models
{
    public class ResourceRequestViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please select a disaster")]
        [Display(Name = "Disaster")]
        public int DisasterId { get; set; }

        [Required(ErrorMessage = "Please select a resource")]
        [Display(Name = "Resource Needed")]
        public int ResourceId { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Display(Name = "Quantity Needed")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int QuantityRequested { get; set; }

        [Required(ErrorMessage = "Please select urgency level")]
        [Display(Name = "Urgency Level")]
        public string UrgencyLevel { get; set; } = "Normal";

        [Display(Name = "Date Required By")]
        [DataType(DataType.Date)]
        public DateTime? DateRequired { get; set; }

        // For dropdowns
        public List<SelectListItem> Disasters { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Resources { get; set; } = new List<SelectListItem>();
    }
}