using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
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
                             OrderNote, PaymentMethod, Subtotal, Shipping, Total, OrderDate, Status,UserId)
                            VALUES 
                            (@CustomerName, @Address, @City, @Country, @PostCode, @Phone, @Email,
                             @OrderNote, @PaymentMethod, @Subtotal, @Shipping, @Total, @OrderDate, @Status , @UserId);
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
                        cmd.Parameters.AddWithValue("@UserId", order.UserId);

                        orderId = (int)cmd.ExecuteScalar();
                    }

                    // 2. Insert Order Products and decrease quantities
                    foreach (var item in order.OrderItems)
                    {
                        // Decrease quantity for the specific size
                        DecreaseProductQuantity(item.productId, item.quantity, item.size);

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
                            cmd.Parameters.AddWithValue("@Size", item.size ?? (object)DBNull.Value);
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
        public void DecreaseProductQuantity(int productId, int quantity, string size)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // First get the current stock and sizes
                string getQuery = @"SELECT stock, AvailableSizes FROM Product WHERE productId = @ProductId";
                string stockJson = "";
                string sizesJson = "";

                using (SqlCommand getCmd = new SqlCommand(getQuery, conn))
                {
                    getCmd.Parameters.AddWithValue("@ProductId", productId);
                    using (SqlDataReader reader = getCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            stockJson = reader.GetString(0);
                            sizesJson = reader.GetString(1);
                        }
                    }
                }

                // Parse the JSON data
                var sizes = JsonConvert.DeserializeObject<List<string>>(sizesJson);
                var stock = JsonConvert.DeserializeObject<List<string>>(stockJson);

                // Find the index of the size and update the quantity
                if (!string.IsNullOrEmpty(size))
                {
                    int sizeIndex = sizes.IndexOf(size);
                    if (sizeIndex >= 0 && sizeIndex < stock.Count)
                    {
                        if (int.TryParse(stock[sizeIndex], out int currentQty))
                        {
                            int newQty = currentQty - quantity;
                            if (newQty < 0) newQty = 0; // Prevent negative quantities
                            stock[sizeIndex] = newQty.ToString();
                        }
                    }
                }
                else
                {
                    // For products without sizes, update the first quantity
                    if (stock.Count > 0 && int.TryParse(stock[0], out int currentQty))
                    {
                        int newQty = currentQty - quantity;
                        if (newQty < 0) newQty = 0;
                        stock[0] = newQty.ToString();
                    }
                }

                // Update the product with new stock values
                string updateQuery = @"UPDATE Product SET stock = @NewStock WHERE productId = @ProductId";
                string newStockJson = JsonConvert.SerializeObject(stock);

                using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                {
                    updateCmd.Parameters.AddWithValue("@ProductId", productId);
                    updateCmd.Parameters.AddWithValue("@NewStock", newStockJson);
                    updateCmd.ExecuteNonQuery();
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
            var recentOrders = new List<RecentOrder>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    SELECT TOP 3 
                        o.OrderId,
                        o.OrderDate,
                        o.Total AS TotalAmount,
                        o.Status,
                        o.CustomerName
                    FROM Orders o
                    INNER JOIN OrderProducts op ON o.OrderId = op.OrderId
                    INNER JOIN Product p ON op.ProductId = p.productId
                    INNER JOIN Brand b ON p.brandId = b.brandId
                    WHERE b.brandOwnerId = @ShopkeeperId
                    ORDER BY o.OrderDate DESC";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ShopkeeperId", shopkeeperId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            recentOrders.Add(new RecentOrder
                            {
                                OrderId = reader.GetInt32(reader.GetOrdinal("OrderId")),
                                OrderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate")),
                                TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                                Status = reader.GetString(reader.GetOrdinal("Status")),
                                CustomerName = reader.GetString(reader.GetOrdinal("CustomerName"))
                            });
                        }
                    }
                }
            }
            return recentOrders;
        }
        public DashboardStats GetDashboardStats(string shopkeeperId)
        {
            var stats = new DashboardStats();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Corrected Total Sales - Using DISTINCT OrderId to avoid double-counting
                string salesQuery = @"
            SELECT ISNULL(SUM(DistinctOrderTotals.Total), 0)
            FROM (
                SELECT DISTINCT o.OrderId, o.Total
                FROM Orders o
                JOIN OrderProducts op ON o.OrderId = op.OrderId
                JOIN Product p ON op.ProductId = p.ProductId
                JOIN Brand b ON p.BrandId = b.BrandId
                WHERE b.BrandOwnerId = @ShopkeeperId
            ) AS DistinctOrderTotals";

                using (SqlCommand cmd = new SqlCommand(salesQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@ShopkeeperId", shopkeeperId);
                    stats.TotalSales = Convert.ToDecimal(cmd.ExecuteScalar());
                }

                // Total Orders (unchanged, already correct)
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

                // Total Customers (unchanged, already correct)
                string customerCountQuery = @"
            SELECT COUNT(DISTINCT o.Email)
            FROM Orders o
            JOIN OrderProducts op ON o.OrderId = op.OrderId
            JOIN Product p ON op.ProductId = p.ProductId
            JOIN Brand b ON p.BrandId = b.BrandId
            WHERE b.BrandOwnerId = @ShopkeeperId
            AND o.Email IS NOT NULL";

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
                ProductThumbnailURL
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
                                ProductThumbnailURL = reader.IsDBNull(4) ? null : reader.GetString(4)
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
        public List<Order> GetDistinctCustomersByBrandOwnerId(string brandOwnerId)
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
            INNER JOIN Brand b ON op.brandId = b.brandId
            WHERE b.brandOwnerId = @brandOwnerId;";
            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@brandOwnerId", brandOwnerId);
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
        public List<Order> GetOrdersByUserId(string userId)
        {
            var orders = new List<Order>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"
            SELECT 
                OrderId,
                CustomerName,
                Total,
                OrderDate,
                Status
            FROM Orders
            WHERE UserId = @UserId
            ORDER BY OrderDate DESC";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            orders.Add(new Order
                            {
                                OrderId = reader.GetInt32(0),
                                CustomerName = reader.GetString(1),
                                Total = reader.GetDecimal(2),
                                OrderDate = reader.GetDateTime(3),
                                Status = reader.GetString(4)
                            });
                        }
                    }
                }
            }
            return orders;
        }
    }
}