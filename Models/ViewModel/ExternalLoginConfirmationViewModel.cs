using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.ViewModel
{
    public class ExternalLoginConfirmationViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Full Name")]
        public string Name { get; set; }
    }
}
