namespace WebApplication1.Models.ViewModel
{
    public class ShopkeeperOrdersViewModel
    {
        public List<RecentOrder> Orders { get; set; }
        public decimal Last24hSales { get; set; }
        public int Last24hCount { get; set; }
        public decimal Last7dSales { get; set; }
        public int Last7dCount { get; set; }
        public decimal Last30dSales { get; set; }
        public int Last30dCount { get; set; }
        public string CurrentFilter { get; set; }
    }
}
