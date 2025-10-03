using System.ComponentModel.DataAnnotations;

namespace GiftOfTheGiversApp.ViewModels
{
    public class DisasterViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Disaster name is required")]
        [Display(Name = "Disaster Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Location is required")]
        public string Location { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Required(ErrorMessage = "Disaster type is required")]
        [Display(Name = "Disaster Type")]
        public string DisasterType { get; set; }

        [Required(ErrorMessage = "Severity level is required")]
        [Display(Name = "Severity Level")]
        public string SeverityLevel { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } = "Active";

        [Display(Name = "Estimated People Affected")]
        [Range(0, int.MaxValue, ErrorMessage = "Estimated affected must be a positive number")]
        public int? EstimatedAffected { get; set; }
    }
}