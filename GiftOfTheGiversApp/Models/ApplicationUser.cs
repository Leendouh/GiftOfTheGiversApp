using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace GiftOfTheGiversApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Display(Name = "Full Name")]
        public string FullName => $"{FirstName} {LastName}";

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<Volunteer> VolunteerProfiles { get; set; }
        public virtual ICollection<Donation> Donations { get; set; }
        public virtual ICollection<Disaster> ReportedDisasters { get; set; }
    }
}
