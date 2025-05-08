using Microsoft.Data.SqlClient;
namespace WebApplication1.Models.Interfaces
{
    public interface IWishlistRepository
    {
        public bool addToWishlist(Wishlist item);
        public (int Count, List<Product> Products) GetWishlistProductsOfCurrentUser(string userId);
    }
}
