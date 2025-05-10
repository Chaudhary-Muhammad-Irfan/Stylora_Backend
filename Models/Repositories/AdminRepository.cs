using Microsoft.Data.SqlClient;
using System.Data;
using WebApplication1.Models.Interfaces;
namespace WebApplication1.Models.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Stylora;Integrated Security=True";
        public decimal GetTotalSales()
        {
            decimal totalSales = 0;
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT SUM(Total) FROM Orders";
                SqlCommand command = new SqlCommand(query, connection);

                connection.Open();
                var result = command.ExecuteScalar();
                if (result != DBNull.Value)
                {
                    totalSales = Convert.ToDecimal(result);
                }
            }
            return totalSales;
        }
        public int GetTotalOrders()
        {
            int totalOrders = 0;
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT COUNT(*) FROM Orders";
                SqlCommand command = new SqlCommand(query, connection);

                connection.Open();
                totalOrders = Convert.ToInt32(command.ExecuteScalar());
            }
            return totalOrders;
        }
        public int GetTotalCustomers()
        {
            int totalCustomers = 0;
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT COUNT(DISTINCT Email) FROM Orders WHERE Email IS NOT NULL";
                SqlCommand command = new SqlCommand(query, connection);

                connection.Open();
                totalCustomers = Convert.ToInt32(command.ExecuteScalar());
            }
            return totalCustomers;
        }
        public int GetApprovedBrands()
        {
            int approvedBrands = 0;
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT COUNT(*) FROM Brand WHERE brandRegistrationStatus = 'Approved'";
                SqlCommand command = new SqlCommand(query, connection);

                connection.Open();
                approvedBrands = Convert.ToInt32(command.ExecuteScalar());
            }
            return approvedBrands;
        }
        public Tuple<int, int> GetReviewStats()
        {
            int reviewCount = 0;
            double averageRating = 0;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT COUNT(*), AVG(CAST(Rating AS FLOAT)) FROM Review";
                SqlCommand command = new SqlCommand(query, connection);

                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        reviewCount = reader.GetInt32(0);
                        if (!reader.IsDBNull(1))
                        {
                            averageRating = reader.GetDouble(1);
                        }
                    }
                }
            }

            // Round to nearest integer for star rating
            int roundedRating = (int)Math.Round(averageRating, MidpointRounding.AwayFromZero);
            return Tuple.Create(reviewCount, roundedRating);
        }
        public DataTable GetRecentOrders()
        {
            DataTable recentOrders = new DataTable();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"SELECT TOP 3 o.OrderId, o.CustomerName, o.Total, o.OrderDate, o.Status, 
                           STRING_AGG(op.ProductName, ', ') AS Products
                           FROM Orders o
                           JOIN OrderProducts op ON o.OrderId = op.OrderId
                           GROUP BY o.OrderId, o.CustomerName, o.Total, o.OrderDate, o.Status
                           ORDER BY o.OrderDate DESC";

                SqlCommand command = new SqlCommand(query, connection);
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                connection.Open();
                adapter.Fill(recentOrders);
            }
            return recentOrders;
        }
        public decimal GetLast24HoursSales()
        {
            decimal totalSales = 0;
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"SELECT SUM(Total) 
                           FROM Orders 
                           WHERE OrderDate >= DATEADD(HOUR, -24, GETDATE())";
                SqlCommand command = new SqlCommand(query, connection);

                connection.Open();
                var result = command.ExecuteScalar();
                if (result != DBNull.Value)
                {
                    totalSales = Convert.ToDecimal(result);
                }
            }
            return totalSales;
        }
        public int GetLast24HoursApprovedBrands()
        {
            int approvedBrands = 0;
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"SELECT COUNT(*) 
                            FROM Brand 
                            WHERE brandRegistrationStatus = 'Approved' 
                            AND brandRegistrationDate >= DATEADD(HOUR, -24, GETDATE())";
                SqlCommand command = new SqlCommand(query, connection);

                connection.Open();
                approvedBrands = Convert.ToInt32(command.ExecuteScalar());
            }
            return approvedBrands;
        }
        public int GetLast24HoursOrders()
        {
            int totalOrders = 0;
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"SELECT COUNT(*) 
                           FROM Orders 
                           WHERE OrderDate >= DATEADD(HOUR, -24, GETDATE())";
                SqlCommand command = new SqlCommand(query, connection);

                connection.Open();
                totalOrders = Convert.ToInt32(command.ExecuteScalar());
            }
            return totalOrders;
        }
        public int GetLast24HoursCustomers()
        {
            int totalCustomers = 0;
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"SELECT COUNT(DISTINCT Email) 
                           FROM Orders 
                           WHERE Email IS NOT NULL 
                           AND OrderDate >= DATEADD(HOUR, -24, GETDATE())";
                SqlCommand command = new SqlCommand(query, connection);

                connection.Open();
                totalCustomers = Convert.ToInt32(command.ExecuteScalar());
            }
            return totalCustomers;
        }
        public List<RecentOrder> GetAllOrdersForAdmin()
        {
            var orders = new List<RecentOrder>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"
            SELECT DISTINCT 
                o.OrderId, 
                o.OrderDate, 
                o.Total, 
                o.Status, 
                o.CustomerName
            FROM Orders o
            JOIN OrderProducts op ON o.OrderId = op.OrderId
            JOIN Product p ON op.productId = p.productId
            JOIN Brand b ON p.BrandId = b.BrandId
            ORDER BY o.OrderDate DESC";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            orders.Add(new RecentOrder
                            {
                                OrderId = reader.GetInt32(0),
                                OrderDate = reader.GetDateTime(1),
                                TotalAmount = reader.GetDecimal(2),
                                Status = reader.GetString(3),
                                CustomerName = reader.GetString(4)
                            });
                        }
                    }
                }
            }

            return orders;
        }
        public List<Order> GetAllDistinctCustomers()
        {
            List<Order> orders = new List<Order>();

            string query = @"
        SELECT DISTINCT 
            o.CustomerName, 
            o.Email, 
            o.Phone, 
            o.City, 
            o.Address, 
            o.PostCode
        FROM Orders o
        INNER JOIN OrderProducts op ON o.orderId = op.OrderId
        INNER JOIN Brand b ON op.brandId = b.brandId";

            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                con.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Order order = new Order
                        {
                            CustomerName = reader["CustomerName"].ToString(),
                            Email = reader["Email"]?.ToString(),
                            Phone = reader["Phone"].ToString(),
                            City = reader["City"].ToString(),
                            Address = reader["Address"].ToString(),
                            PostCode = reader["PostCode"]?.ToString()
                        };

                        orders.Add(order);
                    }
                }
            }
            return orders;
        }
    }
}
