using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Brand
    {
        // Brand-related fields 
        public int brandId { get; set; }
        public string brandName { get; set; }
        public string niche { get; set; }
        public string description { get; set; }
        public string address { get; set; }
        [NotMapped]
        public IFormFile brandLogo { get; set; }
        public string brandLogoURL { get; set; }
        public string socialLink { get; set; }
        public string tagLine { get; set; }

        // Personal details
        public string brandOwnerId { get; set; }
        public string brandOwnerName { get; set; }
        public string contact { get; set; }
        public string cnic { get; set; }
        public string bankName { get; set; }
        public string accountNumber { get; set; }
        public string accountHolderName { get; set; } 

        // Brand Registration status

        public string brandRegistrationStatus { get; set; }= "Pending";
        public DateTime brandRegistrationDate { get; set; } = DateTime.Now;
    }
}