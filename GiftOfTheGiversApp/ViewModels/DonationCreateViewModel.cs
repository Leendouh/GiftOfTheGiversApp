using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GiftOfTheGiversApp.ViewModels
{
    public class DonationCreateViewModel
    {
        [Required(ErrorMessage = "Please select a resource to donate")]
        [Display(Name = "Resource")]
        public int ResourceId { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Additional Notes")]
        public string Notes { get; set; }

        // For dropdown
        public List<SelectListItem> Resources { get; set; } = new List<SelectListItem>();
    }
}