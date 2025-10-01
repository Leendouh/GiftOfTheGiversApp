using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace GiftOfTheGiversApp.Models
{
    public class ResourceRequest
    {
        public int Id { get; set; }

        // Foreign keys
        public int DisasterId { get; set; }
        public int ResourceId { get; set; }

        [Required]
        [Display(Name = "Quantity Requested")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int QuantityRequested { get; set; }

        [Display(Name = "Urgency Level")]
        public string UrgencyLevel { get; set; } = "Normal"; // Low, Normal, High, Critical

        public string Status { get; set; } = "Pending"; // Pending, Approved, Fulfilled

        [Display(Name = "Requested By")]
        public string RequestedById { get; set; }

        [Display(Name = "Date Requested")]
        public DateTime DateRequested { get; set; } = DateTime.Now;

        [Display(Name = "Date Required")]
        [DataType(DataType.DateTime)]
        public DateTime? DateRequired { get; set; }

        // Navigation properties
        [ForeignKey("DisasterId")]
        public virtual Disaster Disaster { get; set; }

        [ForeignKey("ResourceId")]
        public virtual Resource Resource { get; set; }

        [ForeignKey("RequestedById")]
        public virtual ApplicationUser RequestedBy { get; set; }
    }
}
