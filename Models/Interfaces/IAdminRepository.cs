using Microsoft.Data.SqlClient;
using System.Data;

namespace WebApplication1.Models.Interfaces
{
    public interface IAdminRepository
    {
        public decimal GetTotalSales();
        public int GetTotalOrders();
        public int GetTotalCustomers();
        public int GetApprovedBrands();
        public Tuple<int, int> GetReviewStats();
        public DataTable GetRecentOrders();
        public decimal GetLast24HoursSales();
        public int GetLast24HoursApprovedBrands();
        public int GetLast24HoursOrders();
        public int GetLast24HoursCustomers();
        public List<RecentOrder> GetAllOrdersForAdmin();
        public List<Order> GetAllDistinctCustomers();
    }
}