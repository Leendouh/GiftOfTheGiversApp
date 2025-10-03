using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GiftOfTheGiversApp.ViewModels
{
    public class ResourceViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Resource name is required")]
        [Display(Name = "Resource Name")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Please select a category")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [DataType(DataType.MultilineText)]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Unit of measure is required")]
        [Display(Name = "Unit of Measure")]
        [StringLength(50, ErrorMessage = "Unit cannot exceed 50 characters")]
        public string UnitOfMeasure { get; set; }

        [Required(ErrorMessage = "Current quantity is required")]
        [Display(Name = "Current Quantity")]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be 0 or greater")]
        public int CurrentQuantity { get; set; } = 0;

        [Required(ErrorMessage = "Low stock alert is required")]
        [Display(Name = "Low Stock Alert")]
        [Range(1, int.MaxValue, ErrorMessage = "Alert level must be at least 1")]
        public int ThresholdQuantity { get; set; } = 5;

        // For dropdown
        public List<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
    }
}