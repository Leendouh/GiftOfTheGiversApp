using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GiftOfTheGiversApp.Models
{
    public class Disaster
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Disaster Name")]
        public string Name { get; set; }

        [Required]
        public string Location { get; set; }

        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Display(Name = "Disaster Type")]
        public string DisasterType { get; set; } // Flood, Earthquake, Fire, etc.

        [Display(Name = "Severity Level")]
        public string SeverityLevel { get; set; } // Low, Medium, High, Critical

        public string Status { get; set; } = "Active"; // Active, Resolved

        [Display(Name = "Start Date")]
        [DataType(DataType.DateTime)]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [Display(Name = "Estimated Affected")]
        public int? EstimatedAffected { get; set; }

        // Foreign key
        public string ReportedById { get; set; }

        // Navigation properties
        [ForeignKey("ReportedById")]
        public virtual ApplicationUser ReportedBy { get; set; }

        public virtual ICollection<Assignment> Assignments { get; set; }
        public virtual ICollection<Mission> Missions { get; set; }
        public virtual ICollection<ResourceRequest> ResourceRequests { get; set; }
    }
}
