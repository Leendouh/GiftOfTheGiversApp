using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace GiftOfTheGiversApp.Models
{
    public class Resource
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual ResourceCategory Category { get; set; }

        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Display(Name = "Unit of Measure")]
        public string UnitOfMeasure { get; set; } // kg, liters, boxes, pieces

        [Display(Name = "Current Quantity")]
        public int CurrentQuantity { get; set; } = 0;

        [Display(Name = "Minimum Quantity")]
        public int ThresholdQuantity { get; set; } = 5;

        [Display(Name = "Is Low Stock")]
        public bool IsLowStock => CurrentQuantity <= ThresholdQuantity;

        // Navigation properties
        public virtual ICollection<Donation> Donations { get; set; }
        public virtual ICollection<ResourceRequest> ResourceRequests { get; set; }
    }
}
