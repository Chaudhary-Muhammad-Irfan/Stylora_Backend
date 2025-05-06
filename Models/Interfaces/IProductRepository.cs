using Microsoft.Data.SqlClient;

namespace WebApplication1.Models.Interfaces
{ 
    public interface IProductRepository
    {
        void addProduct(Product product);
        public int GetBrandIdOfCurrentShopkeeper(string userId);
        public (int BrandId, string BrandName) GetBrandInfoOfCurrentShopkeeper(string userId);
        List<Product> GetAllProducts(string userId);
       // Product getProductByID(int productId);
    }
}