namespace WebApplication1.Models
{
    public class OrderedProduct
    {
        public string ProductThumbnailURL { get; set; }
        public string ProductName { get; set; }
        public string Size { get; set; }
        public decimal Price { get; set; }       
        public int Quantity { get; set; }
    }
}
