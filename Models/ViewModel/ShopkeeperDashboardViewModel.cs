namespace WebApplication1.Models.ViewModel
{
    public class ShopkeeperDashboardViewModel
    {
        public string ShopkeeperName { get; set; }
        public DashboardStats Stats { get; set; }
        public List<RecentOrder> RecentOrders { get; set; }
    }
}
