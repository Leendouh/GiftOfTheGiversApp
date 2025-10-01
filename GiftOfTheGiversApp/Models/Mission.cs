using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace GiftOfTheGiversApp.Models
{
    public class Mission
    {
        public int Id { get; set; }

        // Foreign key
        public int DisasterId { get; set; }

        [Required]
        public string Title { get; set; }

        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Display(Name = "Assigned To")]
        public int? AssignedToId { get; set; }

        public string Status { get; set; } = "Open"; // Open, In Progress, Completed
        public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical

        [Display(Name = "Due Date")]
        [DataType(DataType.DateTime)]
        public DateTime? DueDate { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Created By")]
        public string CreatedById { get; set; }

        // Navigation properties
        [ForeignKey("DisasterId")]
        public virtual Disaster Disaster { get; set; }

        [ForeignKey("AssignedToId")]
        public virtual Volunteer AssignedTo { get; set; }

        [ForeignKey("CreatedById")]
        public virtual ApplicationUser CreatedBy { get; set; }
    }
}
