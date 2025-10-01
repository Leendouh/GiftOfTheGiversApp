using System.ComponentModel.DataAnnotations;

namespace GiftOfTheGiversApp.Models
{
    public class VolunteerViewModel
    {
        [Required(ErrorMessage = "Skills information is required")]
        [Display(Name = "Skills & Qualifications")]
        [DataType(DataType.MultilineText)]
        public string Skills { get; set; }

        [Required(ErrorMessage = "Availability status is required")]
        [Display(Name = "Availability Status")]
        public string AvailabilityStatus { get; set; } = "Available";

        [Display(Name = "Address")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Emergency contact is required")]
        [Display(Name = "Emergency Contact")]
        [Phone(ErrorMessage = "Please enter a valid phone number")]
        public string EmergencyContact { get; set; }
    }
}