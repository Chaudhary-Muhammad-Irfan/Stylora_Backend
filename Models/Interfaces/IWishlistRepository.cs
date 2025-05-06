using Microsoft.Data.SqlClient;
namespace WebApplication1.Models.Interfaces
{
    public interface IWishlistRepository
    {
        public bool addToWishlist(Wishlist item);
        public List<Product> GetWishlistProductsOfCurrentUser(string userId);
    }
}
