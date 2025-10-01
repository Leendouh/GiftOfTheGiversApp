using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace GiftOfTheGiversApp.Models
{
    public class Assignment
    {
        public int Id { get; set; }

        // Foreign keys
        public int VolunteerId { get; set; }
        public int DisasterId { get; set; }

        [Display(Name = "Assignment Date")]
        public DateTime AssignmentDate { get; set; } = DateTime.Now;

        [Display(Name = "Role in Assignment")]
        public string RoleInAssignment { get; set; } // Team Lead, Medic, Distributor, etc.

        public string Status { get; set; } = "Assigned"; // Assigned, Completed, Cancelled

        [Display(Name = "Assigned By")]
        public string AssignedById { get; set; }

        // Navigation properties
        [ForeignKey("VolunteerId")]
        public virtual Volunteer Volunteer { get; set; }

        [ForeignKey("DisasterId")]
        public virtual Disaster Disaster { get; set; }

        [ForeignKey("AssignedById")]
        public virtual ApplicationUser AssignedBy { get; set; }
    }
}
