using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace GiftOfTheGiversApp.Models
{
    public class Donation
    {
        public int Id { get; set; }

        // Foreign keys
        public string DonorId { get; set; }
        public int ResourceId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Display(Name = "Donation Date")]
        public DateTime DonationDate { get; set; } = DateTime.Now;

        public string Status { get; set; } = "Pending"; // Pending, Received, Distributed

        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        // Navigation properties
        [ForeignKey("DonorId")]
        public virtual ApplicationUser Donor { get; set; }

        [ForeignKey("ResourceId")]
        public virtual Resource Resource { get; set; }
    }
}
