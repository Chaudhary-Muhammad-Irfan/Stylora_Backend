namespace WebApplication1.Models.ViewModel
{
    public class ProductDetailsViewModel
    {
        public Product Product { get; set; }
        public List<Review> Reviews { get; set; }
        public List<Product> RelatedProducts { get; set; }
    }
}
