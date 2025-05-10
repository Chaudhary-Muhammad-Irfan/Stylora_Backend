using Microsoft.Data.SqlClient;
namespace WebApplication1.Models.Interfaces
{
    public interface ICartRepository
    {
        public bool AddToCart(Cart item);
        public (int count, List<int> availableQuantities, List<Cart> carts) GetCartProducts(string userId);
        public bool UpdateQuantity(int cartId, int newQuantity);
        public bool RemoveFromCart(string userId, int productId, string size);
        public void ClearCart(string userId);
    }
}
 