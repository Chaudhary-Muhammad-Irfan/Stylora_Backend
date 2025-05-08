using Microsoft.Data.SqlClient;
namespace WebApplication1.Models.Interfaces
{
    public interface ICartRepository
    {
        public bool AddToCart(Cart cart);
        public (int count, List<Cart> carts) GetCartProducts(string userId);
        public bool UpdateQuantity(int cartId, int newQuantity);
    }
}
