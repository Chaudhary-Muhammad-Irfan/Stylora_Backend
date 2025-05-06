using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Wishlist
    {
        [Key]
        public int wishlistId { get; set; }
        public string userId { get; set; }
        public int brandId { get; set; }
        public string brandName { get; set; }
        public int productId { get; set; }
        public string productName { get; set; }
        public string productThumbnailURL { get; set; }
        public int price { get; set; }
        public int stock { get; set; }
    }
}
