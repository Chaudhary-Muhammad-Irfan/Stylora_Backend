using Microsoft.Data.SqlClient;
namespace WebApplication1.Models.Interfaces
{
    public interface ICartRepository
    {
        public bool AddToCart(Cart cart);
        public List<Cart> GetCartProducts(string userId);
        public bool UpdateQuantity(int cartId, int newQuantity);
    }
}
