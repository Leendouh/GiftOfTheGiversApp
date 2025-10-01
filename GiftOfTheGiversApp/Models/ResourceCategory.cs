using System.ComponentModel.DataAnnotations;

namespace GiftOfTheGiversApp.Models
{
    public class ResourceCategory
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; }

        public string Description { get; set; }

        // Navigation properties
        public virtual ICollection<Resource> Resources { get; set; }
    }
}
