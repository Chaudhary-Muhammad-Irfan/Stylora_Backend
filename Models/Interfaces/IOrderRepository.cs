using Microsoft.Data.SqlClient;

namespace WebApplication1.Models.Interfaces
{
    public interface IOrderRepository
    {
        public int PlaceOrder(Order order);
        public Order GetOrderById(int orderId);
        public List<RecentOrder> GetRecentOrders(string shopkeeperId);
        public DashboardStats GetDashboardStats(string shopkeeperId);
        public List<RecentOrder> GetAllOrdersOfShopkeeper(string shopkeeperId);
        public List<OrderedProduct> GetOrderedProductsByOrderId(int orderId);
        public string GetShopkeeperName(string userId);
        public List<Order> GetDistinctCustomersByBrandOwnerId(string brandOwnerId);
        public void DecreaseProductQuantity(int productId, int quantity, string size);
        public List<Order> GetOrdersByUserId(string userId);
    }
}