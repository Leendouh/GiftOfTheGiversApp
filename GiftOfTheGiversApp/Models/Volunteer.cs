using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace GiftOfTheGiversApp.Models
{
    public class Volunteer
    {
        public int Id { get; set; }

        // Link to Identity User
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        [DataType(DataType.MultilineText)]
        public string Skills { get; set; } // Comma-separated skills

        [Display(Name = "Availability Status")]
        public string AvailabilityStatus { get; set; } = "Available"; // Available, Busy, Unavailable

        public string Address { get; set; }

        [Display(Name = "Emergency Contact")]
        public string EmergencyContact { get; set; }

        [Display(Name = "Date Registered")]
        public DateTime DateRegistered { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<Assignment> Assignments { get; set; }
        public virtual ICollection<Mission> AssignedMissions { get; set; }
    }
}
