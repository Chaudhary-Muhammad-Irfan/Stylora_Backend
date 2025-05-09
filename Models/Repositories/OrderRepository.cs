using Microsoft.Data.SqlClient;
using WebApplication1.Models.Interfaces;

namespace WebApplication1.Models.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Stylora;Integrated Security=True";
        public int PlaceOrder(Order order)
        {
            int orderId = 0;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    // 1. Insert Order
                    string orderQuery = @"INSERT INTO Orders 
                                    (CustomerName, Address, City, Country, PostCode, Phone, Email, 
                                     OrderNote, PaymentMethod, Subtotal, Shipping, Total, OrderDate, Status)
                                    VALUES 
                                    (@CustomerName, @Address, @City, @Country, @PostCode, @Phone, @Email,
                                     @OrderNote, @PaymentMethod, @Subtotal, @Shipping, @Total, @OrderDate, @Status);
                                    SELECT CAST(SCOPE_IDENTITY() as int)";

                    using (SqlCommand cmd = new SqlCommand(orderQuery, connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@CustomerName", order.CustomerName);
                        cmd.Parameters.AddWithValue("@Address", order.Address);
                        cmd.Parameters.AddWithValue("@City", order.City);
                        cmd.Parameters.AddWithValue("@Country", order.Country);
                        cmd.Parameters.AddWithValue("@PostCode", order.PostCode ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Phone", order.Phone);
                        cmd.Parameters.AddWithValue("@Email", order.Email ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@OrderNote", order.OrderNote ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PaymentMethod", order.PaymentMethod);
                        cmd.Parameters.AddWithValue("@Subtotal", order.Subtotal);
                        cmd.Parameters.AddWithValue("@Shipping", order.Shipping);
                        cmd.Parameters.AddWithValue("@Total", order.Total);
                        cmd.Parameters.AddWithValue("@OrderDate", order.OrderDate);
                        cmd.Parameters.AddWithValue("@Status", order.Status);

                        orderId = (int)cmd.ExecuteScalar();
                    }

                    // 2. Insert Order Products
                    foreach (var item in order.OrderItems)
                    {
                        string productQuery = @"INSERT INTO OrderProducts
                                          (OrderId, ProductId, BrandId, ProductName, Size, 
                                           Price, Quantity, Subtotal, ProductThumbnailURL)
                                          VALUES
                                          (@OrderId, @ProductId, @BrandId, @ProductName, @Size,
                                           @Price, @Quantity, @Subtotal, @ProductThumbnailURL)";

                        using (SqlCommand cmd = new SqlCommand(productQuery, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@OrderId", orderId);
                            cmd.Parameters.AddWithValue("@ProductId", item.productId);
                            cmd.Parameters.AddWithValue("@BrandId", item.brandId);
                            cmd.Parameters.AddWithValue("@ProductName", item.productName);
                            cmd.Parameters.AddWithValue("@Size", item.size);
                            cmd.Parameters.AddWithValue("@Price", item.price);
                            cmd.Parameters.AddWithValue("@Quantity", item.quantity);
                            cmd.Parameters.AddWithValue("@Subtotal", item.subTotal);
                            cmd.Parameters.AddWithValue("@ProductThumbnailURL", item.productThumbnailURL);

                            cmd.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                    return orderId;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
        public Order GetOrderById(int orderId)
        {
            Order order = null;
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // Fetch order details
                string orderQuery = @"SELECT * FROM Orders WHERE OrderId = @OrderId";
                using (SqlCommand cmd = new SqlCommand(orderQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@OrderId", orderId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            order = new Order
                            {
                                OrderId = (int)reader["OrderId"],
                                CustomerName = reader["CustomerName"].ToString(),
                                Address = reader["Address"].ToString(),
                                City = reader["City"].ToString(),
                                Country = reader["Country"].ToString(),
                                PostCode = reader["PostCode"].ToString(),
                                Phone = reader["Phone"].ToString(),
                                Email = reader["Email"].ToString(),
                                OrderNote = reader["OrderNote"].ToString(),
                                PaymentMethod = reader["PaymentMethod"].ToString(),
                                Subtotal = Convert.ToDecimal(reader["Subtotal"]),
                                Shipping = Convert.ToDecimal(reader["Shipping"]),
                                Total = Convert.ToDecimal(reader["Total"]),
                                OrderDate = Convert.ToDateTime(reader["OrderDate"]),
                                Status = reader["Status"].ToString(),
                                OrderItems = new List<Cart>()
                            };
                        }
                    }
                }

                if (order != null)
                {
                    // Fetch order products (Cart items)
                    string itemsQuery = @"SELECT * FROM OrderProducts WHERE OrderId = @OrderId";
                    using (SqlCommand cmd = new SqlCommand(itemsQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@OrderId", orderId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Cart item = new Cart
                                {
                                    productName = reader["ProductName"].ToString(),
                                    size = reader["Size"].ToString(),
                                    quantity = Convert.ToInt32(reader["Quantity"]),
                                    price = Convert.ToInt32(reader["Price"]),
                                    subTotal = Convert.ToInt32(reader["Subtotal"]),
                                    productThumbnailURL = reader["ProductThumbnailURL"].ToString()
                                };

                                order.OrderItems.Add(item);
                            }
                        }
                    }
                }
            }

            return order;
        }
        public List<RecentOrder> GetRecentOrders(string shopkeeperId)
        {
            var orders = new List<RecentOrder>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"
            SELECT TOP 2 o.OrderId, o.OrderDate, o.Total AS TotalAmount, 
                         o.Status, u.Name AS CustomerName
            FROM Orders o
            JOIN OrderProducts op ON o.OrderId = op.OrderId
            JOIN Product p ON op.ProductId = p.ProductId
            JOIN Brand b ON p.BrandId = b.BrandId
            JOIN AspNetUsers u ON o.Email = u.Email
            WHERE b.BrandOwnerId = @ShopkeeperId
              AND o.OrderDate >= DATEADD(day, -1, GETDATE())
            ORDER BY o.OrderDate DESC";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@ShopkeeperId", shopkeeperId);

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
        public DashboardStats GetDashboardStats(string shopkeeperId)
        {
            var stats = new DashboardStats();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Total Sales
                string salesQuery = @"
            SELECT SUM(o.Total)
            FROM Orders o
            JOIN OrderProducts op ON o.OrderId = op.OrderId
            JOIN Product p ON op.ProductId = p.ProductId
            JOIN Brand b ON p.BrandId = b.BrandId
            WHERE b.BrandOwnerId = @ShopkeeperId";

                using (SqlCommand cmd = new SqlCommand(salesQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@ShopkeeperId", shopkeeperId);
                    stats.TotalSales = cmd.ExecuteScalar() != DBNull.Value ? Convert.ToDecimal(cmd.ExecuteScalar()) : 0;
                }

                // Total Orders
                string orderCountQuery = @"
            SELECT COUNT(DISTINCT o.OrderId)
            FROM Orders o
            JOIN OrderProducts op ON o.OrderId = op.OrderId
            JOIN Product p ON op.ProductId = p.ProductId
            JOIN Brand b ON p.BrandId = b.BrandId
            WHERE b.BrandOwnerId = @ShopkeeperId";

                using (SqlCommand cmd = new SqlCommand(orderCountQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@ShopkeeperId", shopkeeperId);
                    stats.OrderCount = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // Total Customers
                string customerCountQuery = @"
            SELECT COUNT(DISTINCT o.Email)
            FROM Orders o
            JOIN OrderProducts op ON o.OrderId = op.OrderId
            JOIN Product p ON op.ProductId = p.ProductId
            JOIN Brand b ON p.BrandId = b.BrandId
            WHERE b.BrandOwnerId = @ShopkeeperId";

                using (SqlCommand cmd = new SqlCommand(customerCountQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@ShopkeeperId", shopkeeperId);
                    stats.CustomerCount = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }

            return stats;
        }
        public List<RecentOrder> GetAllOrdersOfShopkeeper(string shopkeeperId)
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
                u.Name AS CustomerName
            FROM Orders o
            JOIN OrderProducts op ON o.OrderId = op.OrderId
            JOIN Product p ON op.productId = p.productId
            JOIN Brand b ON p.BrandId = b.BrandId
            JOIN AspNetUsers u ON b.brandOwnerId = u.Id
            WHERE b.brandOwnerId = @ShopkeeperId
            ORDER BY o.OrderDate DESC";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@ShopkeeperId", shopkeeperId);

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
        public List<OrderedProduct> GetOrderedProductsByOrderId(int orderId)
        {
            var products = new List<OrderedProduct>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"
            SELECT 
                ProductName,
                Size,
                Price,
                Quantity,
                ProductThumbnailURL,
                OrderId
            FROM OrderProducts
            WHERE OrderId = @OrderId";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@OrderId", orderId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            products.Add(new OrderedProduct
                            {
                                ProductName = reader.GetString(0),
                                Size = reader.GetString(1),
                                Price = reader.GetDecimal(2),
                                Quantity = reader.GetInt32(3),
                                ProductThumbnailURL = reader.IsDBNull(4) ? null : reader.GetString(4),
                                OrderId=reader.GetInt32(5)
                            });
                        }
                    }
                }
            }
            return products;
        }
        public string GetShopkeeperName(string userId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT Name FROM AspNetUsers WHERE Id = @UserId";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    return cmd.ExecuteScalar()?.ToString();
                }
            }
        }
    }
}