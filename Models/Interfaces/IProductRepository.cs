using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using System.Diagnostics;

namespace WebApplication1.Models.Interfaces
{ 
    public interface IProductRepository
    {
        public void addProduct(Product product);
        public void deleteProduct(int productId);
        public (int BrandId, string BrandName) GetBrandInfoOfCurrentShopkeeper(string userId);
        public int GetBrandIdOfCurrentShopkeeper(string userId);
        // Getting Prodcuts of currently login shopkeeper
        public List<Product> GetAllProducts(string userId);
        // Getting products of current Brand
        public List<Product> GetAllProductsOfCurrentBrand(int brandId);
        public Product getProductByID(int productId);
        public void AddReview(Review review);
        public List<Review> GetReviewsByProductId(int productId);
        public DataTable GetReviewsOfShopkeeperProducts(string shopkeeperId);
        public int CountUnreadReviews(string shopkeeperId);
        public void MarkReviewsAsRead(string shopkeeperId, List<int> reviewIds = null);
        public List<Product> GetProductsByBrandAndCategory(int brandId, string productCategory, int productId);
        public List<Product> GetDistinctCategoriesWithThumbnails();
        public List<Product> GetNewArrivals();
        public List<Brand> GetNewlyAddedBrands();
        public (int ReviewCount, decimal AverageRating) GetShopkeeperReviewStats(string brandOwnerId);
    }
}