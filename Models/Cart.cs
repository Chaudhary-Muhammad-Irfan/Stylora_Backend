using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Cart
    {
        [Key]
        public int cardId { get; set; } 
        public string userId { get; set; }
        public int productId { get; set; }
        public string brandName { get; set; }
        public string productName { get; set; }
        public string size { get; set; }
        public string productThumbnailURL { get; set; }
        public int price { get; set; }
        public int quantity { get; set; }
        public int availableStock { get; set; }
        public int subTotal { get; set; }
    }
}
